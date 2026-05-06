using UnityEngine;
using TMPro;

public class PlayerItemUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text teamText;
    public TMP_Text readyText;

    public void SetData(PlayerData data)
    {
        nameText.text = "Player " + data.clientId;
        teamText.text = data.team.ToString();
        readyText.text = data.isReady ? "Ready" : "Not Ready";
    }
}