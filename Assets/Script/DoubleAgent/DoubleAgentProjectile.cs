using Unity.Netcode;
using UnityEngine;

public class DoubleAgentProjectile : NetworkBehaviour
{
    public float lifeTime = 3f;
    public ulong ownerId;
    public int damage = 10;

    void Start()
    {
        if (IsServer)
        {
            Invoke(nameof(DestroySelf), lifeTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        var target = collision.collider.GetComponent<PlayerDoubleAgent>();

        if (target != null)
        {
            if (target.OwnerClientId == ownerId)
                return;

            target.TakeDamage(damage, ownerId);
        }

        DestroySelf();
    }

    void DestroySelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}