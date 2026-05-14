using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum MinigameType
{
    FFA,
    Snowball,
    DoubleAgent
}

public class MinigameManager : NetworkBehaviour
{
    public static MinigameManager Instance;
    private List<ulong> alivePlayerIds = new List<ulong>();
    private bool gameStarted = false;
    private bool gameEnded = false;
    private Dictionary<TeamType, int> teamAlive = new();

    private void Awake()
    {
        Instance = this;
        Debug.Log("MinigameManager Awake");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        MinigameSpawnManager.Instance.SpawnPlayers();

        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        yield return new WaitUntil(() =>
            alivePlayerIds.Count == NetworkManager.Singleton.ConnectedClientsList.Count
        );

        Debug.Log("All players ready");

        LockAllPlayers(true);

        yield return new WaitForSeconds(2f);

        LockAllPlayers(false);

        gameStarted = true;
    }

    public void RegisterPlayer(NetworkBehaviour player)
    {
        if (!IsServer) return;

        if (!alivePlayerIds.Contains(player.OwnerClientId))
        {
            alivePlayerIds.Add(player.OwnerClientId);
        }
    }

    public void PlayerDied(NetworkBehaviour player)
    {
        if (!IsServer) return;

        alivePlayerIds.Remove(player.OwnerClientId);

        CheckWinner();
    }

    void CheckWinner()
    {
        if (!IsServer || gameEnded) return;

        if (alivePlayerIds.Count <= 1)
        {
            gameEnded = true;

            ulong? winner = alivePlayerIds.Count == 1
                ? alivePlayerIds[0]
                : (ulong?)null;

            StartCoroutine(EndGameRoutine(winner));
        }
    }

    public void TeamWin(TeamType winningTeam)
    {
        if (!IsServer || gameEnded) return;

        gameEnded = true;

        StartCoroutine(EndGameTeamRoutine(winningTeam));
    }

    IEnumerator EndGameTeamRoutine(TeamType winningTeam)
    {
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            var player = obj.GetComponent<PlayerController>();

            if (player == null) continue;

            if (player.team.Value == winningTeam)
            {
                player.coin.Value += 100;
                GameManager.Instance.CheckWinCondition(player);
            }
        }

        yield return new WaitForSeconds(3f);

        ReturnToBoard();
    }


    IEnumerator EndGameRoutine(ulong? winnerId)
    {

        if (winnerId.HasValue)
        {
            var player = NetworkManager.Singleton
                .ConnectedClients[winnerId.Value]
                .PlayerObject
                .GetComponent<PlayerController>();

            if (player != null)
            {
                player.coin.Value += 100;
                GameManager.Instance.CheckWinCondition(player);
            }
        }

        yield return new WaitForSeconds(3f);

        ReturnToBoard();
    }

    public void ReturnToBoard()
    {
        if (!IsServer) return;

        StartCoroutine(ReturnBoardRoutine());
    }

    IEnumerator ReturnBoardRoutine()
    {
        GameManager.Instance.pendingNextTurn = true;
        GameManager.Instance.isInMinigame = false;

        gameStarted = false;
        gameEnded = false;
        alivePlayerIds.Clear();

        var objects = new List<NetworkObject>(
            NetworkManager.Singleton.SpawnManager.SpawnedObjectsList
        );

        foreach (var obj in objects)
        {
            if (obj.GetComponent<Snowball>() != null ||
                obj.GetComponent<SnowProjectile>() != null)
            {
                obj.Despawn(true);
            }
            if (obj.GetComponent<ScoreOrb>() != null)
            {
                obj.Despawn(true);
            }
        }

        yield return null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            var state = client.PlayerObject.GetComponent<PlayerState>();
            if (state != null)
                state.SetMode(PlayerMode.Board);
        }

        StartCoroutine(LoadBoardRoutine());
    }

    IEnumerator LoadBoardRoutine()
    {
        yield return new WaitForSeconds(0.3f);

        NetworkSceneLoader.Instance.LoadBoard();
    }

    void LockAllPlayers(bool locked)
    {
        Debug.Log("Unlock");
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var obj = client.PlayerObject;
            if (obj == null) continue;

            var ffa = obj.GetComponent<PlayerMinigame>();
            if (ffa != null)
            {
                ffa.isAlive = true;
                ffa.SetCanMove(!locked);
            }

            var snow = obj.GetComponent<PlayerSnowballMinigame>();
            if (snow != null)
            {
                snow.isAlive = true;
                snow.canMove.Value = !locked;
            }

            var da = obj.GetComponent<PlayerDoubleAgent>();
            if (da != null)
            {
                da.canMove.Value = !locked;
            }
        }
    }
}