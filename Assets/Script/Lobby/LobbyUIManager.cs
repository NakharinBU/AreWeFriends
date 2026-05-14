using UnityEngine;
using Unity.Netcode;
using TMPro;

public class LobbyUIManager : MonoBehaviour
{
    public Transform playerListParent;
    public GameObject playerItemPrefab;
    public TextMeshProUGUI playerCount;


    void Start()
    {
        LobbyManager.Instance.players.OnListChanged += OnLobbyChanged;
        RefreshUI();
    }

    void OnLobbyChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        RefreshUI();
    }

    void OnDestroy()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.players.OnListChanged -= OnLobbyChanged;
    }

    void RefreshUI()
    {
        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        foreach (var p in LobbyManager.Instance.players)
        {
            var item = Instantiate(playerItemPrefab, playerListParent);
            item.GetComponent<PlayerItemUI>().SetData(p);
        }


        playerCount.text = $"Players: {LobbyManager.Instance.players.Count} / 8";
    }
}