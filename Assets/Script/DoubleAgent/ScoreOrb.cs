using Unity.Netcode;
using UnityEngine;

public class ScoreOrb : NetworkBehaviour
{
    public int score = 10;

    public override void OnNetworkDespawn()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        DoubleAgentMinigameManager.Instance
            .AddScore(player.team.Value, score);

        player.coin.Value += 5;

        NetworkObject.Despawn();
    }
}