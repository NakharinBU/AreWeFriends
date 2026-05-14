using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerWorldUI : NetworkBehaviour
{
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI nameText;

    private PlayerController player;

    public override void OnNetworkSpawn()
    {
        player = GetComponentInParent<PlayerController>();

        if (player == null) return;

        player.hp.OnValueChanged += OnHPChanged;
        player.coin.OnValueChanged += OnCoinChanged;

        UpdateUI();
    }

    void OnDestroy()
    {
        if (player == null) return;

        player.hp.OnValueChanged -= OnHPChanged;
        player.coin.OnValueChanged -= OnCoinChanged;
    }

    void OnHPChanged(int oldValue, int newValue)
    {
        UpdateUI();
    }

    void OnCoinChanged(int oldValue, int newValue)
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (hpText != null)
            hpText.text = "HP: " + player.hp.Value;

        if (coinText != null)
            coinText.text = "Coin: " + player.coin.Value;

        if (nameText != null)
        {
            if (player.OwnerClientId == NetworkManager.LocalClientId)
            {
                nameText.text = "You";
            }
            else
            {
                nameText.text = "Player " + (player.OwnerClientId + 1);
            }
        }
    }
}