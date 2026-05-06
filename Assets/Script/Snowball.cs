using Unity.Netcode;
using UnityEngine;

public class Snowball : NetworkBehaviour
{
    public TeamType team;

    public float size = 1f;
    public float maxSize = 5f;

    public Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            rb.isKinematic = false;
        }
        else
        {
            rb.isKinematic = true;
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