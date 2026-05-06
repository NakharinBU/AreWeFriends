using UnityEngine;
using Unity.Netcode;

public class ReadyButtonUI : MonoBehaviour
{
    public void OnClickReady()
    {
        LobbyManager.Instance.ToggleReadyRpc(
            NetworkManager.Singleton.LocalClientId
        );
    }
}