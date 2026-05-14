using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class LeaderboardUI : MonoBehaviour
{
    public Transform contentRoot;
    public GameObject rowPrefab;

    private List<LeaderboardRowUI> spawnedRows = new();

    void Start()
    {
        GameManager.Instance.leaderboard.OnListChanged += UpdateUI;
        Refresh();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.leaderboard.OnListChanged -= UpdateUI;
    }

    void UpdateUI(NetworkListEvent<PlayerLeaderboardData> changeEvent)
    {
        Refresh();
    }

    void Refresh()
    {
        var list = GameManager.Instance.leaderboard;

        foreach (var row in spawnedRows)
        {
            Destroy(row.gameObject);
        }
        spawnedRows.Clear();

        var sorted = new List<PlayerLeaderboardData>();
        foreach (var data in list)
        {
            sorted.Add(data);
        }

        sorted.Sort((a, b) => b.coin.CompareTo(a.coin));

        foreach (var data in sorted)
        {
            GameObject go = Instantiate(rowPrefab, contentRoot);
            var row = go.GetComponent<LeaderboardRowUI>();

            string playerName =
                data.clientId == NetworkManager.Singleton.LocalClientId
                ? "You"
                : "Player " + data.clientId;

            bool isMe = data.clientId == NetworkManager.Singleton.LocalClientId;

            row.Setup(playerName, data.coin, data.hp, isMe);

            spawnedRows.Add(row);
        }
    }
}