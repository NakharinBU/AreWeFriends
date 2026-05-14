using Unity.Netcode;
using UnityEngine;

public class Snowball : NetworkBehaviour
{
    public TeamType team;

    public NetworkVariable<TeamType> teamNet = new NetworkVariable<TeamType>();

    public float size = 1f;
    public float maxSize = 3f;

    public bool scored = false;

    [Header("Decay")]
    public float shrinkDelay = 1.5f;
    public float shrinkSpeed = 0.5f;

    private float lastPushTime;

    public Rigidbody rb;

    [Header("Visual")]
    public Renderer meshRenderer;

    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material greenMat;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    [Header("Respawn")]
    public float fallY = -10f;

    public void SetSpawnPoint(Vector3 pos, Quaternion rot)
    {
        spawnPosition = pos;
        spawnRotation = rot;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            rb.isKinematic = false;
            teamNet.Value = team;
        }
        else
        {
            rb.isKinematic = true;
        }

        ApplyMaterial(teamNet.Value);

        teamNet.OnValueChanged += OnTeamChanged;
    }

    void Update()
    {
        if (!IsServer) return;

        if (Time.time - lastPushTime > shrinkDelay)
        {
            Shrink();
        }

        if (transform.position.y < fallY)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        size = 1f;
        transform.localScale = Vector3.one * size;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsServer) return;

        var player = collision.collider.GetComponent<PlayerSnowballMinigame>();

        if (player != null && player.isAlive)
        {
            Vector3 dir = (player.transform.position - transform.position).normalized;

            AddForce(dir, player.pushForce);
            Grow(0.02f);

            lastPushTime = Time.time;
        }
    }

    void Shrink()
    {
        if (size <= 1f) return;

        size -= shrinkSpeed * Time.deltaTime;
        size = Mathf.Clamp(size, 1f, maxSize);

        transform.localScale = Vector3.one * size;
    }

    void OnTeamChanged(TeamType oldTeam, TeamType newTeam)
    {
        ApplyMaterial(newTeam);
    }

    void ApplyMaterial(TeamType t)
    {
        switch (t)
        {
            case TeamType.Red:
                meshRenderer.material = redMat;
                break;
            case TeamType.Blue:
                meshRenderer.material = blueMat;
                break;
            case TeamType.Yellow:
                meshRenderer.material = yellowMat;
                break;
            case TeamType.Green:
                meshRenderer.material = greenMat;
                break;
        }
    }

    public void AddForce(Vector3 dir, float power)
    {
        if (!IsServer) return;
        rb.AddForce(dir * power, ForceMode.Force);
    }

    public void Grow(float amount)
    {
        if (!IsServer) return;

        size = Mathf.Clamp(size + amount, 1f, maxSize);
        transform.localScale = Vector3.one * size;
    }
}