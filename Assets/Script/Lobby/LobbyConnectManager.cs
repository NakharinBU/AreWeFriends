using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyConnectManager : MonoBehaviour
{
    public TMP_InputField ipInput;

    public void CreateLobby()
    {
        NetworkManager.Singleton.StartHost();

        Debug.Log("Lobby Created!");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Lobby",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    public void JoinLobby()
    {
        string ip = ipInput.text;

        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        transport.ConnectionData.Address = ip;

        NetworkManager.Singleton.StartClient();

        Debug.Log("Joining Lobby at: " + ip);
    }
}