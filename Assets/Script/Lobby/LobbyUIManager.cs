using UnityEngine;
using Unity.Netcode;

public class LobbyUIManager : MonoBehaviour
{
    public Transform playerListParent;
    public GameObject playerItemPrefab;

    void Start()
    {
        LobbyManager.Instance.players.OnListChanged += OnLobbyChanged;
        RefreshUI();
    }

    void OnLobbyChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        RefreshUI();
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
    }
}