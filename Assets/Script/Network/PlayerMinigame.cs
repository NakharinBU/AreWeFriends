using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMinigame : NetworkBehaviour
{
    [Header("Shockwave")]
    public float shockwaveForce = 125f;
    public float shockwaveRadius = 3f;
    public float shockwaveCooldown = 2f;
    [Header("Directional Attack")]
    public float attackRange = 4f;
    public float attackAngle = 60f;

    private Vector3 lastMoveDirection = Vector3.forward;

    private float lastShockwaveTime = -999f;

    public float moveSpeed = 5f;
    public Rigidbody rb;
    public PlayerState state;
    public bool isAlive = true;
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false);

    [Header("Input")]
    public InputReader inputReader;

    private Vector3 moveDirection;

    public Transform visualRoot;

    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.MoveEvent += OnMoveInput;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.MoveEvent -= OnMoveInput;
        }
    }

    public override void OnNetworkSpawn()
    {
        canMove.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"[{OwnerClientId}] canMove: {newVal}");
        };
    }
    void Update()
    {
        if (!IsOwner || !isAlive || !canMove.Value) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryShockwave();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner || !canMove.Value || !isAlive) return;

        float speed = rb.velocity.magnitude;

        animator.SetFloat("Speed", speed);

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



    void TryShockwave()
    {
        if (Time.time - lastShockwaveTime < shockwaveCooldown)
            return;

        lastShockwaveTime = Time.time;

        ShockwaveServerRpc(lastMoveDirection);
    }

    [ServerRpc]
    void ShockwaveServerRpc(Vector3 direction)
    {
        if (!isAlive) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);

        foreach (var hit in hits)
        {
            var other = hit.GetComponent<PlayerMinigame>();

            if (other != null && other != this && other.isAlive)
            {
                Vector3 toTarget = (other.transform.position - transform.position).normalized;

                float angle = Vector3.Angle(direction, toTarget);

                if (angle <= attackAngle / 2f)
                {
                    Rigidbody otherRb = other.rb;

                    if (otherRb != null)
                    {
                        otherRb.AddForce(direction * shockwaveForce, ForceMode.Impulse);
                    }
                }
            }
        }
    }

    private void OnMoveInput(Vector3 input)
    {
        if (!IsOwner) return;
        moveDirection = input;
        
        if (input != Vector3.zero)
        {
            lastMoveDirection = input.normalized;
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("DeadZone"))
            Die();
    }

    void Die()
    {
        if (!IsServer) return;

        isAlive = false;

        SetCanMove(false);

        HidePlayer();

        MinigameManager.Instance.PlayerDied(this);
    }

    void HidePlayer()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = false;

        var colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
            c.enabled = false;
    }

    public void SetCanMove(bool value)
    {
        if (!IsServer) return;
        canMove.Value = value;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + lastMoveDirection * attackRange);
    }
}