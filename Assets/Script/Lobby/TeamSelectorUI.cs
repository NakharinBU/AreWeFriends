using UnityEngine;
using Unity.Netcode;

public class TeamSelectorUI : MonoBehaviour
{
    public void SelectRed()
    {
        LobbyManager.Instance.ChangeTeamRpc(
            NetworkManager.Singleton.LocalClientId,
            TeamType.Red
        );
    }

    public void SelectBlue()
    {
        LobbyManager.Instance.ChangeTeamRpc(
            NetworkManager.Singleton.LocalClientId,
            TeamType.Blue
        );
    }
}