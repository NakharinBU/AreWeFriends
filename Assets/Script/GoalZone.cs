using Unity.Netcode;
using UnityEngine;

public class GoalZone : NetworkBehaviour
{
    public TeamType team;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var ball = other.GetComponent<Snowball>();

        if (ball != null && ball.team == team)
        {
            MinigameManager.Instance.TeamWin(team);
        }
    }
}