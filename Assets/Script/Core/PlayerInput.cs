using Unity.Netcode;
using UnityEngine;

public class PlayerInput : NetworkBehaviour
{
    private PlayerController player;

    private void Start()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (player == null) return;
        if (!player.IsMyTurn()) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.RollDiceRpc();
        }
    }
}