using UnityEngine;
using Unity.Netcode;

public class StartGameUI : MonoBehaviour
{
    public GameObject startButton;

    void Start()
    {
        startButton.SetActive(NetworkManager.Singleton.IsHost);
    }

    public void OnClickStart()
    {
        LobbyManager.Instance.StartGameRpc();
    }
}