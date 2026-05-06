using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public NetworkList<PlayerData> players = new NetworkList<PlayerData>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                AddPlayer(client.ClientId);
            }

            NetworkManager.Singleton.OnClientConnectedCallback += AddPlayer;
        }
    }

    void AddPlayer(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log("Add Player: " + clientId);

        PlayerData data = new PlayerData
        {
            clientId = clientId,
            team = TeamType.None,
            isReady = false
        };

        players.Add(data);
    }

    [Rpc(SendTo.Server)]
    public void ToggleReadyRpc(ulong clientId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == clientId)
            {
                var p = players[i];
                p.isReady = !p.isReady;
                players[i] = p;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void ChangeTeamRpc(ulong clientId, TeamType newTeam)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == clientId)
            {
                var p = players[i];
                p.team = newTeam;
                players[i] = p;
            }
        }
    }

    public bool CanStartGame()
    {
        if (players.Count < 2) return false;

        foreach (var p in players)
        {
            if (!p.isReady) return false;
        }

        return true;
    }


    [Rpc(SendTo.Server)]
    public void StartGameRpc()
    {
        if (!IsServer) return;

        if (!CanStartGame())
        {
            Debug.Log("Not all players ready!");
            return;
        }

        Debug.Log("Starting Game...");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "BoardScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}