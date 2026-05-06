using System.Collections;
using System.Collections.Generic;
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
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(RebuildPlayerListRoutine());

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            SceneManager.sceneLoaded += OnSceneLoaded;

        }
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
        StartCoroutine(AssignTeamDelayed());
    }

    IEnumerator AssignTeamDelayed()
    {
        yield return new WaitForSeconds(0.2f);

        AssignTeams();
    }

    public void RollDice(ulong senderId)
    {
        if (playerIds.Count == 0) return;

        if (senderId != playerIds[currentPlayerIndex.Value])
            return;

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
        
        NextTurn();

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

        Debug.Log("TRIGGER TILE: " + tile.tileType);

        if (tile == null) return;

        switch (tile.tileType)
        {
            case TileType.Coin:
                player.coin.Value += tile.coinAmount;
                break;

            case TileType.Damage:
                player.hp.Value -= tile.damageAmount;
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

    [ServerRpc]
    void RequestEnterMinigameServerRpc()
    {
        EnterMinigame();
    }

    void EnterMinigame()
    {
        if (!IsServer || isInMinigame) return;

        isInMinigame = true;

        /*currentMinigame.Value = (Random.value > 0.5f)
            ? MinigameType.FFA
            : MinigameType.Snowball;*/

        int rnd = Random.Range(1,4);

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
        }

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

        playerIds.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            playerIds.Add(client.ClientId);
        }

        Debug.Log("Rebuild playerIds: " + playerIds.Count);

        currentPlayerIndex.Value = savedTurnIndex;
        
        if (!hasGameStartedServer)
        {
            CheckStartGame();
        }
    }

    void AssignTeams()
    {
        if (gameStarted.Value == false) return;

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
}