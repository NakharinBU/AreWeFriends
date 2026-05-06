using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class TurnUI : NetworkBehaviour
{
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI diceText;
    public DiceAnimator diceAnimator;

    private void Start()
    {
        StartCoroutine(StartUI());
    }

    public override void OnNetworkSpawn()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.currentPlayerIndex.OnValueChanged += OnTurnChanged;
        GameManager.Instance.diceValue.OnValueChanged += OnDiceChanged;

        UpdateTurnUI(GameManager.Instance.currentPlayerIndex.Value);
        UpdateDiceUI(GameManager.Instance.diceValue.Value);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.currentPlayerIndex.OnValueChanged -= OnTurnChanged;
        GameManager.Instance.diceValue.OnValueChanged -= OnDiceChanged;
    }
    IEnumerator StartUI()
    {
        yield return new WaitForSeconds(0.5f);

        if (GameManager.Instance != null)
        {
            UpdateTurnUI(GameManager.Instance.currentPlayerIndex.Value);
        }
    }
    void OnTurnChanged(int oldValue, int newValue)
    {
        UpdateTurnUI(newValue);
    }

    void OnDiceChanged(int oldValue, int newValue)
    {
        if (diceAnimator != null)
            diceAnimator.PlayDiceAnimation(newValue);
    }

    void UpdateTurnUI(int index)
    {
        if (turnText == null || GameManager.Instance == null) return;

        ulong currentPlayerId = GameManager.Instance.GetCurrentPlayerId();
        ulong myId = NetworkManager.Singleton.LocalClientId;

        if (currentPlayerId == myId)
        {
            turnText.text = "Turn: You";
        }
        else
        {
            int playerIndex = GameManager.Instance.GetPlayerIndex(currentPlayerId);

            turnText.text = "Turn: Player " + (playerIndex + 1);
        }
    }

    void UpdateDiceUI(int value)
    {
        if (diceText != null)
            diceText.text = "Dice: " + value;
    }
}