using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInputHandler : NetworkBehaviour
{
    public Vector2 _moveInput2D; // ใช้ Vector2 แทน Vector3

    // Only enable input actions for the local player instance
    public override void OnNetworkSpawn()
    {
        // Nothing to do here if using Send Messages
    }

    // Method linked via PlayerInput (Send Messages)
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return; // Owner check for Netcode

        _moveInput2D = value.Get<Vector2>(); // <-- ต้องเป็น Vector2
    }

    // Optionally, you can convert Vector2 -> Vector3 when applying movement
    public Vector3 GetMoveInput3D()
    {
        return new Vector3(_moveInput2D.x, 0f, _moveInput2D.y);
    }
}