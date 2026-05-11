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

        Debug.Log($"Ball Team = {ball.teamNet.Value} | Goal Team = {team}");

        if (ball.teamNet.Value == team)
        {
            ball.scored = true;

            Debug.Log("TEAM SCORE!");

            TeamType scoringTeam = ball.teamNet.Value;

            MinigameManager.Instance.TeamWin(scoringTeam);

            ball.GetComponent<NetworkObject>().Despawn();
        }
    }
}