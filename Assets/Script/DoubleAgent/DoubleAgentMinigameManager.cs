using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DoubleAgentMinigameManager : NetworkBehaviour
{
    public static DoubleAgentMinigameManager Instance;

    public float gameDuration = 60f;

    private float timer;
    private bool gameStarted = false;

    private List<ulong> alivePlayers = new List<ulong>();

    private Dictionary<TeamType, int> teamScore = new();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        RegisterAllPlayers();

        StartCoroutine(StartGameRoutine());
    }

    void RegisterAllPlayers()
    {
        alivePlayers.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            alivePlayers.Add(client.ClientId);
        }

        Debug.Log("Alive Players: " + alivePlayers.Count);
    }

    IEnumerator StartGameRoutine()
    {
        yield return new WaitForSeconds(2f);

        gameStarted = true;
        timer = gameDuration;

        Debug.Log("Double Agent Minigame START");
    }

    void Update()
    {
        if (!IsServer || !gameStarted) return;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            EndGame();
        }
    }

    public void PlayerDied(PlayerDoubleAgent player)
    {
        if (!IsServer) return;

        ulong id = player.OwnerClientId;

        if (alivePlayers.Contains(id))
        {
            alivePlayers.Remove(id);
            Debug.Log($"Player Dead: {id}");
        }

        CheckEndByAlive();
    }

    void CheckEndByAlive()
    {
        if (alivePlayers.Count <= 1)
        {
            Debug.Log("End by Last Man Standing");
            EndGame();
        }
    }

    public void AddScore(TeamType team, int amount)
    {
        if (!teamScore.ContainsKey(team))
            teamScore[team] = 0;

        teamScore[team] += amount;
    }

    void EndGame()
    {
        if (!gameStarted) return;

        gameStarted = false;

        TeamType winner = TeamType.None;
        int maxScore = -1;

        foreach (var kv in teamScore)
        {
            if (kv.Value > maxScore)
            {
                maxScore = kv.Value;
                winner = kv.Key;
            }
        }

        Debug.Log($"WINNER TEAM: {winner}");

        StartCoroutine(EndRoutine(winner));
    }

    IEnumerator EndRoutine(TeamType winner)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<PlayerController>();

            if (player.team.Value == winner)
            {
                player.coin.Value += 100;
            }
        }

        yield return new WaitForSeconds(3f);

        MinigameManager.Instance.ReturnToBoard();
    }
}