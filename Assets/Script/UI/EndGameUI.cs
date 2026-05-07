using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI winnerText;
    public Transform leaderboardRoot;
    public GameObject leaderboardItemPrefab;
    bool shown = false;

    private void Start()
    {
        panel.SetActive(false);

        GameManager.Instance.gameEnded.OnValueChanged += OnGameEnded;

        if (GameManager.Instance.gameEnded.Value)
        {
            ShowUI();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.gameEnded.OnValueChanged -= OnGameEnded;
        }
    }

    void OnGameEnded(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ShowUI();
        }
    }

    void ShowUI()
    {
        if (shown) return;
        shown = true;

        panel.SetActive(true);

        ulong winnerId = GameManager.Instance.winnerClientId.Value;

        if (NetworkManager.Singleton.LocalClientId == winnerId)
            winnerText.text = "YOU WIN";
        else
            winnerText.text = $"Player {winnerId} Wins";

        ShowLeaderboard();
    }

    void ShowLeaderboard()
    {
        foreach (Transform child in leaderboardRoot)
        {
            Destroy(child.gameObject);
        }

        var list = new List<PlayerLeaderboardData>();

        foreach (var data in GameManager.Instance.leaderboard)
        {
            list.Add(data);
        }

        list.Sort((a, b) => b.coin.CompareTo(a.coin));

        foreach (var data in list)
        {
            GameObject item =
                Instantiate(leaderboardItemPrefab, leaderboardRoot);

            item.GetComponentInChildren<TextMeshProUGUI>().text =
                $"Player {data.clientId} - {data.coin} coins";
        }
    }

    public void OnClickBackToMain()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
    "Mainmenu",
    LoadSceneMode.Single);
        }
    }
}