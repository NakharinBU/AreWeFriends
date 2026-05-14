using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    private int turnCount = 0;

    public Transform spawnPoints;

    public int maxPlayers = 2;
    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);

    private void Awake()
    {
        Instance = this;
    }

    public NetworkVariable<int> currentPlayerIndex = new NetworkVariable<int>(0);
    public NetworkVariable<int> diceValue = new NetworkVariable<int>(0);

    private NetworkList<ulong> playerIds = new NetworkList<ulong>();

    private bool hasSpawnedPlayers = false;
    public bool shouldEnterMinigame = false;
    public bool isInMinigame = false;
    public bool pendingNextTurn = false;

    private bool hasGameStartedServer = false;
    public static int savedTurnIndex = 0;

    [SerializeField] private GameObject playerPrefab;

    public NetworkVariable<MinigameType> currentMinigame =
    new NetworkVariable<MinigameType>();

    [SerializeField] private string ffaScene = "Minigame_FFA";
    [SerializeField] private string snowballScene = "Minigame_Snow";

    public NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);
    public NetworkVariable<ulong> winnerClientId = new NetworkVariable<ulong>(0);

    public NetworkList<PlayerLeaderboardData> leaderboard = new NetworkList<PlayerLeaderboardData>();

    public bool canRoll = true;

    public bool CanPlay()
    {
        return !gameEnded.Value;
    }

    void SpawnAllPlayers()
    {
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
        {
            var client = NetworkManager.Singleton.ConnectedClientsList[i];
            ulong clientId = client.ClientId;

            if (client.PlayerObject != null)
            {
                Debug.Log("Player already exists: " + clientId);
                continue;
            }

            Transform spawn = spawnPoints.GetChild(i % spawnPoints.childCount);

            GameObject player = Instantiate(
                playerPrefab,
                spawn.position,
                spawn.rotation
            );

            player.GetComponent<NetworkObject>()
                .SpawnAsPlayerObject(clientId);

            Debug.Log("Spawn Player: " + clientId);
        }

        StartCoroutine(AssignTeamDelayed());
    }


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(RebuildPlayerListRoutine());

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        currentPlayerIndex.OnValueChanged += OnTurnChanged;

    }


    void OnTurnChanged(int oldIndex, int newIndex)
    {
        if (SceneManager.GetActiveScene().name != "BoardScene")
            return;

        ulong playerId = GetCurrentPlayerId();

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId))
            return;

        var playerObj = NetworkManager
            .Singleton
            .ConnectedClients[playerId]
            .PlayerObject;

        if (playerObj == null) return;

        Transform target = playerObj.transform;

        CameraManager.Instance.SetTarget(target);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log("Client Connected: " + clientId);

        if (!playerIds.Contains(clientId))
        {
            playerIds.Add(clientId);
        }

        CheckStartGame();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsServer) return;

        Debug.Log("Scene Loaded: " + scene.name);

        if (pendingNextTurn)
        {
            pendingNextTurn = false;
        }

        StartCoroutine(UpdateLeaderboardDelay());

        if (scene.name == "BoardScene")
        {
            StartCoroutine(ForceUpdateCamera());
        }
    }

    IEnumerator ForceUpdateCamera()
    {
        yield return new WaitUntil(() => CameraManager.Instance != null);

        yield return new WaitUntil(() => FindObjectOfType<CinemachineCamera>() != null);

        yield return new WaitUntil(() =>
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    return false;
            }
            return true;
        });

        yield return null;

        CameraManager.Instance.SetMode(CameraMode.Turn);
        ForceUpdateTurnCamera();

        Debug.Log("✅ Camera Force Updated (SAFE)");
    }

    public void ForceUpdateTurnCamera()
    {
        ulong playerId = GetCurrentPlayerId();

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId))
            return;

        var playerObj = NetworkManager
            .Singleton
            .ConnectedClients[playerId]
            .PlayerObject;

        if (playerObj == null) return;

        CameraManager.Instance.SetTarget(playerObj.transform);
    }

    void CheckStartGame()
    {
        if (hasGameStartedServer) return;

        if (gameStarted.Value) return;

        if (playerIds.Count >= maxPlayers)
        {
            Debug.Log("All players joined. Ready to start!");

            StartGame();
        }
    }

    void StartGame()
    {
        if (!IsServer) return;
        if (hasGameStartedServer) return;

        hasGameStartedServer = true;
        gameStarted.Value = true;

        SpawnAllPlayers();

        UpdateLeaderboard();
    }

    IEnumerator AssignTeamDelayed()
    {
        yield return new WaitUntil(() =>
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    return false;
            }
            return true;
        });

        yield return null;

        AssignTeams();
    }

    public void RollDice(ulong senderId)
    {
        if (gameEnded.Value) return;
        if (canRoll == false) return;

        if (playerIds.Count == 0) return;

        if (senderId != playerIds[currentPlayerIndex.Value])
            return;

        canRoll = false;

        int roll = Random.Range(1, 7);
        diceValue.Value = roll;

        StartCoroutine(TurnRoutine(senderId, roll));
    }

    IEnumerator TurnRoutine(ulong playerId, int steps)
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId))
            yield break;

        var playerObj = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject;
        if (playerObj == null) yield break;

        var player = playerObj.GetComponent<PlayerController>();
        if (player == null) yield break;

        yield return player.MoveRoutine(steps);

        TriggerTileEvent(player, player.currentTileIndex.Value);

        StartCoroutine(UpdateLeaderboardDelay());

        NextTurn();

        canRoll = true;

        yield return new WaitForSeconds(0.5f);

        if (shouldEnterMinigame)
        {
            Debug.Log("ENTER MINIGAME TRIGGERED!");

            shouldEnterMinigame = false;

            RequestEnterMinigameServerRpc();
            yield break;
        }
    }

    public void NextTurn()
    {
        if (!IsServer) return;

        if (playerIds == null || playerIds.Count == 0)
        {
            Debug.LogWarning("playerIds invalid");
            return;
        }

        currentPlayerIndex.Value =
            (currentPlayerIndex.Value + 1) % playerIds.Count;

        savedTurnIndex = currentPlayerIndex.Value;

        if (currentPlayerIndex.Value == 0)
        {
            turnCount++;

            Debug.Log("Round: " + turnCount);
        }

        Debug.Log("Turn → " + currentPlayerIndex.Value);
    }

    public ulong GetCurrentPlayerId()
    {
        if (playerIds.Count == 0) return 0;
        return playerIds[currentPlayerIndex.Value];
    }

    public int GetPlayerIndex(ulong clientId)
    {
        return playerIds.IndexOf(clientId);
    }

    public void TriggerTileEvent(PlayerController player, int tileIndex)
    {
        Tile tile = BoardManager.Instance.GetTile(tileIndex);

        if (tile == null) return;

        PlayTileSoundClientRpc(tileIndex);

        Debug.Log("TRIGGER TILE: " + tile.tileType);


        switch (tile.tileType)
        {
            case TileType.Coin:
                player.coin.Value += tile.coinAmount;
                CheckWinCondition(player);
                break;

            case TileType.Damage:
                player.hp.Value -= tile.damageAmount;

                if (player.hp.Value <= 0)
                {
                    HandlePlayerDeath(player);
                }

                break;

            case TileType.Minigame:
                shouldEnterMinigame = true;
                break;
        }

        if (tileIndex == 0)
        {
            shouldEnterMinigame = true;
        }
    }

    [ClientRpc]
    void PlayTileSoundClientRpc(int tileIndex)
    {
        Tile tile = BoardManager.Instance.GetTile(tileIndex);
        if (tile != null)
        {
            tile.PlaySound();
        }
    }

    [ServerRpc]
    void RequestEnterMinigameServerRpc()
    {
        StartCoroutine(EnterMinigameRoutine());
    }

    IEnumerator EnterMinigameRoutine()
    {
        Debug.Log("Waiting for team sync before entering minigame...");

        yield return new WaitForSeconds(0.2f);

        Debug.Log("All teams ready → Enter Minigame");

        isInMinigame = true;

       currentMinigame.Value = MinigameType.DoubleAgent;
        AssignDoubleAgentTeams();

        /*int rnd = Random.Range(1,4);

        switch (rnd)
        {
            case 1:
                currentMinigame.Value = MinigameType.FFA;
                break;
            case 2:
                currentMinigame.Value = MinigameType.Snowball;
                break;
            case 3:
                currentMinigame.Value = MinigameType.DoubleAgent;
                AssignDoubleAgentTeams();
                break;
        }*/

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<PlayerState>();
            if (player != null)
            {
                player.currentMinigameType.Value = currentMinigame.Value;
                player.SetMode(PlayerMode.Minigame);
            }
        }

        NetworkSceneLoader.Instance.LoadMinigame(currentMinigame.Value);
    }

    IEnumerator RebuildPlayerListRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (hasGameStartedServer)
        {
            Debug.Log("🚫 Skip Rebuild (Game Already Started)");
            yield break;
        }

        var list = new List<ulong>();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            list.Add(client.ClientId);
        }

        playerIds.Clear();
        foreach (var id in list)
        {
            playerIds.Add(id);
        }

        Debug.Log("Rebuild playerIds: " + playerIds.Count);

        currentPlayerIndex.Value = savedTurnIndex;

        CheckStartGame();
    }

    void AssignTeams()
    {
        List<ulong> shuffled = new List<ulong>();

        foreach (var id in playerIds)
        {
            shuffled.Add(id);
        }

        TeamType[] teams = new TeamType[]
    {
        TeamType.Red,
        TeamType.Blue,
        TeamType.Yellow,
        TeamType.Green
    };

        for (int i = 0; i < shuffled.Count; i++)
        {
            ulong clientId = shuffled[i];

            var playerObj = NetworkManager.Singleton
                .ConnectedClients[clientId].PlayerObject;

            var player = playerObj.GetComponent<PlayerController>();
            if (player == null) continue;

            TeamType team = teams[i % teams.Length];

            player.team.Value = team;

            Debug.Log($"Assign {clientId} -> {team}");
        }
    }

    TeamType GetMyTeam(ulong clientId)
    {
        foreach (var p in LobbyManager.Instance.players)
        {
            if (p.clientId == clientId)
                return p.team;
        }

        return TeamType.None;
    }

    void AssignDoubleAgentTeams()
    {
        Dictionary<TeamType, List<PlayerController>> teamMap = new();

        // create team map
        foreach (var id in playerIds)
        {
            var playerObj = NetworkManager.Singleton
                .ConnectedClients[id].PlayerObject;

            var pc = playerObj.GetComponent<PlayerController>();
            if (pc == null) continue;

            if (!teamMap.ContainsKey(pc.team.Value))
                teamMap[pc.team.Value] = new List<PlayerController>();

            teamMap[pc.team.Value].Add(pc);
        }

        List<PlayerController> agents = new();

        // random player from team
        foreach (var kv in teamMap)
        {
            var teamPlayers = kv.Value;

            if (teamPlayers.Count >= 2)
            {
                int rand = Random.Range(0, teamPlayers.Count);
                agents.Add(teamPlayers[rand]);
            }
        }

        // switch team
        foreach (var agent in agents)
        {
            TeamType original = agent.team.Value;

            // save old team
            agent.SaveCurrentTeam();

            // find new team
            List<TeamType> possibleTeams = new()
        {
            TeamType.Red,
            TeamType.Blue,
            TeamType.Yellow,
            TeamType.Green
        };

            possibleTeams.Remove(original);

            TeamType newTeam = possibleTeams[Random.Range(0, possibleTeams.Count)];

            agent.team.Value = newTeam;

            Debug.Log($"[DOUBLE AGENT] {agent.OwnerClientId} {original} -> {newTeam}");
        }
    }

    void CheckWinCondition(PlayerController player)
    {
        if (!IsServer) return;
        if (gameEnded.Value) return;

        if (player.coin.Value >= 500)
        {
            gameEnded.Value = true;
            winnerClientId.Value = player.OwnerClientId;

            Debug.Log($"Player {player.OwnerClientId} WIN!");

            StartCoroutine(EndGameRoutine());
        }
    }

    IEnumerator EndGameRoutine()
    {
        Debug.Log("Game Ending...");

        shouldEnterMinigame = false;
        isInMinigame = false;

        yield return new WaitForSeconds(1f);

        UpdateLeaderboard();
    }

    IEnumerator UpdateLeaderboardDelay()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateLeaderboard();
    }

    public void UpdateLeaderboard()
    {
        if (!IsServer) return;

        leaderboard.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            var player = client.PlayerObject.GetComponent<PlayerController>();
            if (player == null) continue;

            PlayerLeaderboardData data = new PlayerLeaderboardData
            {
                clientId = client.ClientId,
                coin = player.coin.Value,
                hp = player.hp.Value,
                tileIndex = player.currentTileIndex.Value
            };

            leaderboard.Add(data);
        }
    }

    void HandlePlayerDeath(PlayerController player)
    {
        if (!IsServer) return;

        Debug.Log($"Player {player.OwnerClientId} DIED");

        player.coin.Value = Mathf.Max(0, player.coin.Value - 300);

        player.hp.Value = 100;

    }

    [ContextMenu("DEBUG / Force Win Player 0")]
    public void ForceWinPlayer0()
    {
        if (!IsServer)
        {
            Debug.LogWarning("ForceWin can only run on Server");
            return;
        }

        if (playerIds.Count == 0)
        {
            Debug.LogWarning("No players in game");
            return;
        }

        ulong fakeWinnerId = playerIds[0];

        gameEnded.Value = true;
        winnerClientId.Value = fakeWinnerId;

        Debug.Log($"[FORCE WIN] Player {fakeWinnerId} is winner");

        StartCoroutine(EndGameRoutine());
    }
}