using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDoubleAgent : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody rb;

    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false);

    public NetworkVariable<int> hp = new NetworkVariable<int>(100);

    private Vector3 moveDir;
    private Vector3 lastMoveDir = Vector3.forward;

    [Header("Shoot")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public float shootForce = 15f;
    public float shootCooldown = 0.5f;

    private float lastShootTime;

    public bool isDead = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hp.Value = 100;
        }
    }

    void Update()
    {
        if (!IsOwner || !canMove.Value || isDead) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        moveDir = new Vector3(h, 0, v);

        if (moveDir.sqrMagnitude > 0.01f)
            lastMoveDir = moveDir;

        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryShoot();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner || !canMove.Value) return;

        MoveServerRpc(moveDir);
    }

    [ServerRpc]
    void MoveServerRpc(Vector3 dir)
    {
        rb.velocity = dir * moveSpeed;

        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void TryShoot()
    {
        if (Time.time - lastShootTime < shootCooldown) return;

        if (isDead) return;

        lastShootTime = Time.time;

        ShootServerRpc(lastMoveDir.normalized);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 dir)
    {
        if (isDead) return;

        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;

        Quaternion rot = Quaternion.LookRotation(dir);

        var bullet = Instantiate(bulletPrefab, shootPoint.position, rot);
        bullet.GetComponent<NetworkObject>().Spawn();

        var rb = bullet.GetComponent<Rigidbody>();
        rb.velocity = dir * shootForce;

        var proj = bullet.GetComponent<DoubleAgentProjectile>();
        proj.ownerId = OwnerClientId;
    }

    public void TakeDamage(int dmg, ulong attackerId)
    {
        if (!IsServer) return;
        if (isDead) return;

        hp.Value -= dmg;

        if (hp.Value <= 0)
        {
            isDead = true;

            HandleDeath(attackerId);
        }
    }

    void HandleDeath(ulong attackerId)
    {
        var attackerObj = NetworkManager.Singleton
            .ConnectedClients[attackerId].PlayerObject;

        var attackerPC = attackerObj.GetComponent<PlayerController>();

        if (attackerPC != null)
        {
            DoubleAgentMinigameManager.Instance
                .AddScore(attackerPC.team.Value, 20);
        }

        DoubleAgentMinigameManager.Instance.PlayerDied(this);

        isDead = true;
        canMove.Value = false;
        SetVisible(false);
    }

    public void SetVisible(bool state)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = state;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = state;
    }
}