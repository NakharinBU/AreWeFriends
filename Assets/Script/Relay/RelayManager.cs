using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Collections;

public class RelayManager : NetworkBehaviour
{
    public static RelayManager Instance;
    bool isInitialized = false;

    public string CurrentJoinCode { get; private set; }

    //public NetworkVariable<FixedString32Bytes> joinCodeNet = new NetworkVariable<FixedString32Bytes>();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await InitUnityServices();
        isInitialized = true;
    }

    async Task InitUnityServices()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Unity Services Initialized");
    }

    public async Task<string> CreateRelay()
    {
        if (!isInitialized)
        {
            Debug.LogError("Services not ready yet!");
            return null;
        }

        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        CurrentJoinCode = joinCode;
        //joinCodeNet.Value = joinCode;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = alloc.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();

        SetJoinCodeClientRpc(joinCode);

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Lobby",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );

        return joinCode;
    }

    public async Task JoinRelay(string joinCode)
    {
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        CurrentJoinCode = joinCode;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = joinAlloc.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();

        Debug.Log("Joined with code: " + joinCode);
    }

    [ClientRpc]
    void SetJoinCodeClientRpc(string code)
    {
        CurrentJoinCode = code;
    }
}