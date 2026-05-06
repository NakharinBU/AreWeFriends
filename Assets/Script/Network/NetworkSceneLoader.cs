using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneLoader : NetworkBehaviour
{
    public static NetworkSceneLoader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (!IsServer) return;
        
        if (clientId != NetworkManager.ServerClientId) return;

        Debug.Log("Scene Loaded → " + sceneName);

        if (sceneName.Contains("Minigame"))
        {
            StartCoroutine(ApplyMinigameModeDelayed());
        }

        if (sceneName == "BoardScene")
        {
            StartCoroutine(WarpPlayersToBoard());
        }
    }

    IEnumerator WarpPlayersToBoard()
    {
        yield return null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            var playerObj = client.PlayerObject;

            var rb = playerObj.GetComponent<Rigidbody>();
            var netRb = playerObj.GetComponent<NetworkRigidbody>();

            if (netRb != null)
                netRb.enabled = false;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var player = playerObj.GetComponent<PlayerController>();
            player.WarpToTile(player.currentTileIndex.Value);
        }
    }

    IEnumerator ApplyMinigameModeDelayed()
    {
        yield return new WaitForSeconds(0.2f);

        MinigameSpawnManager.Instance.SpawnPlayers();

        yield return new WaitForSeconds(0.1f);

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var state = client.PlayerObject.GetComponent<PlayerState>();

            if (state != null && state.IsSpawned)
            {
                state.SetMode(PlayerMode.Minigame);
            }
        }
    }

    public void LoadMinigame(MinigameType type)
    {
        if (!IsServer) return;

        string sceneName = "";

        switch (type)
        {
            case MinigameType.FFA:
                sceneName = "Minigame_FFA";
                break;

            case MinigameType.Snowball:
                sceneName = "Minigame_Snow";
                break;
            case MinigameType.DoubleAgent:
                sceneName = "Minigame_DoubleAgent";
                break;
        }

        Debug.Log("LOADING MINIGAME: " + sceneName);

        NetworkManager.Singleton.SceneManager.LoadScene(
            sceneName,
            LoadSceneMode.Single
        );
    }

    public void LoadBoard()
    {
        Debug.Log("LoadBoard CALLED");

        if (!IsServer)
        {
            Debug.LogError("NOT SERVER");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NO NETWORK MANAGER");
            return;
        }

        Debug.Log("LOADING SCENE...");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "BoardScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}