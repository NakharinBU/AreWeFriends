using Unity.Netcode;
using UnityEngine;

public class SnowProjectile : NetworkBehaviour
{
    public ulong ownerId;
    public float force = 10f;
    public float lifeTime = 5f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Invoke(nameof(DestroySelf), lifeTime);

        rb.isKinematic = !IsServer;
    }

    void DestroySelf()
    {
        if (IsServer && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        var player = collision.collider.GetComponent<PlayerSnowballMinigame>();

        if (player != null && player.OwnerClientId != ownerId)
        {
            Rigidbody rb = player.rb;

            if (rb != null)
            {
                Vector3 dir = (player.transform.position - transform.position).normalized;
                rb.AddForce(dir * force, ForceMode.Impulse);
            }
        }

        DestroySelf();
    }
}