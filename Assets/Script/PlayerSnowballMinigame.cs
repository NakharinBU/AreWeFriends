using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSnowballMinigame : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody rb;

    public bool isAlive = true;
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false);

    [Header("Shoot")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float shootForce = 12f;
    public float shootCooldown = 0.5f;
    private float lastShootTime;

    [Header("Push Snowball")]
    public float pushForce = 20f;

    private Vector3 moveDirection;
    private Vector3 lastMoveDirection = Vector3.forward;

    public InputReader inputReader;
    public Transform visualRoot;

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.MoveEvent += OnMove;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.MoveEvent -= OnMove;
    }

    void Update()
    {
        if (!IsOwner || !canMove.Value || !isAlive) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TryShoot();
    }

    void FixedUpdate()
    {
        if (!IsOwner || !canMove.Value || !isAlive) return;

        SendInputServerRpc(moveDirection);
    }

    [ServerRpc]
    void SendInputServerRpc(Vector3 dir)
    {
        if (!isAlive) return;

        if (dir.sqrMagnitude < 0.001f)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        dir = dir.normalized;

        rb.velocity = new Vector3(dir.x * moveSpeed, rb.velocity.y, dir.z * moveSpeed);

        Quaternion rot = Quaternion.LookRotation(dir);

        transform.rotation = rot;

        float offset = 0f;
        var state = GetComponent<PlayerState>();
        if (state != null)
            offset = state.modelZRotationOffset;

        visualRoot.localRotation = Quaternion.Euler(0, offset, 0);

        lastMoveDirection = dir;

    }

    void TryShoot()
    {
        if (Time.time - lastShootTime < shootCooldown) return;

        lastShootTime = Time.time;
        ShootServerRpc(lastMoveDirection);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 dir)
    {
        Quaternion rot = Quaternion.LookRotation(dir);

        var obj = Instantiate(projectilePrefab, shootPoint.position, rot);
        obj.GetComponent<NetworkObject>().Spawn();

        var rb = obj.GetComponent<Rigidbody>();
        rb.velocity = dir * shootForce;

        var proj = obj.GetComponent<SnowProjectile>();
        proj.ownerId = OwnerClientId;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsOwner) return;

        var ball = collision.collider.GetComponent<Snowball>();

        if (ball != null)
        {
            PushBallServerRpc(ball.NetworkObjectId, lastMoveDirection);
        }
    }

    [ServerRpc]
    void PushBallServerRpc(ulong ballId, Vector3 dir)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(ballId))
            return;

        var ball = NetworkManager.Singleton.SpawnManager
            .SpawnedObjects[ballId]
            .GetComponent<Snowball>();

        if (ball != null)
        {
            ball.AddForce(dir, pushForce);
            ball.Grow(0.02f);
        }
    }

    void OnMove(Vector3 input)
    {
        if (!IsOwner) return;
        moveDirection = input;
    }
}