using Unity.Netcode;
using UnityEngine;

public class GoalZone : NetworkBehaviour
{
    public TeamType team;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var ball = other.GetComponent<Snowball>();

        if (ball == null) return;

        if (ball.scored) return;

        Debug.Log($"Ball Team = {ball.team} | Goal Team = {team}");

        if (ball.team == team)
        {
            ball.scored = true;

            Debug.Log("TEAM SCORE!");

            MinigameManager.Instance.TeamWin(ball.OwnerClientId);

            ball.GetComponent<NetworkObject>().Despawn();
        }
    }
}