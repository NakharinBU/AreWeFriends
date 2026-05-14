using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI hpText;

    public void Setup(string playerName, int coin, int hp, bool isMe)
    {
        nameText.text = playerName;
        coinText.text = "Coin: " + coin;
        hpText.text = "HP: " + hp;

        if (isMe)
        {
            nameText.color = Color.yellow;
        }
        else
        {
            nameText.color = Color.white;
        }
    }
}