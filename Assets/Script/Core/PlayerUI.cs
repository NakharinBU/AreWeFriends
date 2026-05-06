using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerUI : NetworkBehaviour
{
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI hpText;

    private PlayerController player;

    public override void OnNetworkSpawn()
    {
        player = GetComponent<PlayerController>();

        if (player == null) return;

        player.coin.OnValueChanged += OnCoinChanged;
        player.hp.OnValueChanged += OnHPChanged;

        UpdateUI();
    }

    void OnDestroy()
    {
        if (player == null) return;

        player.coin.OnValueChanged -= OnCoinChanged;
        player.hp.OnValueChanged -= OnHPChanged;
    }

    void OnCoinChanged(int oldValue, int newValue)
    {
        UpdateUI();
    }

    void OnHPChanged(int oldValue, int newValue)
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinText != null)
            coinText.text = "Coin: " + player.coin.Value;

        if (hpText != null)
            hpText.text = "HP: " + player.hp.Value;
    }
}