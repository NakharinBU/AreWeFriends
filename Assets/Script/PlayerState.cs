using System.Collections;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public enum PlayerMode
{
    Board,
    Minigame
}

public class PlayerState : NetworkBehaviour
{
    public NetworkVariable<PlayerMode> currentMode = new NetworkVariable<PlayerMode>(PlayerMode.Board);
    public NetworkVariable<MinigameType> currentMinigameType =
    new NetworkVariable<MinigameType>();

    private PlayerController boardController;
    private PlayerMinigame minigameController;
    private Rigidbody rb;
    private PlayerInput input;
    private NetworkTransform netTransform;
    private NetworkRigidbody netRb;

    private PlayerSnowballMinigame snowController;

    private PlayerDoubleAgent doubleAgentController;

    public float modelZRotationOffset = 0;

    public BoxCollider boxCollider;
    private bool cameraRegistered = false;

    private bool applied = false;

    private void Awake()
    {
        boardController = GetComponent<PlayerController>();
        minigameController = GetComponent<PlayerMinigame>();
        snowController = GetComponent<PlayerSnowballMinigame>();
        doubleAgentController = GetComponent<PlayerDoubleAgent>();

        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
        netTransform = GetComponent<NetworkTransform>();
        netRb = GetComponent<NetworkRigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        currentMode.OnValueChanged += OnModeChanged;
        currentMinigameType.OnValueChanged += OnMinigameChanged;

        StartCoroutine(ForceInit());
    }

    void OnMinigameChanged(MinigameType oldType, MinigameType newType)
    {
        if (currentMode.Value == PlayerMode.Minigame)
        {
            ApplyMode(PlayerMode.Minigame);
        }
    }

    IEnumerator ForceInit()
    {
        yield return new WaitUntil(() => IsSpawned);

        yield return new WaitForSeconds(0.2f);

        ApplyMode(currentMode.Value);
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null && IsSpawned)
        {
            currentMode.OnValueChanged -= OnModeChanged;
        }
    }

    void OnModeChanged(PlayerMode oldMode, PlayerMode newMode)
    {
        ApplyMode(newMode);
    }

    void ApplyMode(PlayerMode mode)
    {
        if (rb == null || boardController == null)
        {
            Debug.LogWarning("Player not ready yet");
            return;
        }

        Debug.Log($"APPLY MODE: {mode} | Owner: {IsOwner}");

        if (IsOwner)
        {
            StartCoroutine(RegisterCamera());
        }

        if (mode == PlayerMode.Board)
        {

            var da = GetComponent<PlayerDoubleAgent>();

            if (IsServer)
            {
                var controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.RestorePreviousTeam();
                }
                if (da != null)
                {
                    da.ResetStateServer();
                    da.gunActive.Value = false;
                }

            }

            if (da != null)
            {
                da.SetVisible(true);
            }

            ShowPlayer();

            if (boardController != null)
                boardController.enabled = true;

            if (minigameController != null)
                minigameController.enabled = false;

            if (snowController != null)
                snowController.enabled = false;

            if (doubleAgentController != null)
            {
                doubleAgentController.enabled = false;
            }

            if (input != null)
                input.enabled = true;

            StartCoroutine(ApplyBoardPhysics());
        }

        if (mode == PlayerMode.Minigame)
        {
            if (boardController != null)
                boardController.enabled = false;

            if (input != null)
                input.enabled = true;

            StartCoroutine(ApplyMinigamePhysics());

            var type = currentMinigameType.Value;

            if (type == MinigameType.FFA)
            {
                if (IsServer)
                {
                    var controller = GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.SaveCurrentTeam();
                        controller.team.Value = TeamType.None;
                    }
                }

                DisableAllModes();
                minigameController.enabled = true;
            }
            else if (type == MinigameType.Snowball)
            {
                DisableAllModes();
                snowController.enabled = true;
            }
            else if (type == MinigameType.DoubleAgent)
            {
                Debug.Log($"DoubleAgent Enabled: {doubleAgentController.enabled} | Owner: {IsOwner}");
                DisableAllModes();
                doubleAgentController.enabled = true;
            }
        }
    }

    IEnumerator RegisterCamera()
    {
        yield return new WaitUntil(() => CameraManager.Instance != null);

        CameraManager.Instance.RegisterPlayer(transform);
    }

    public void SetMode(PlayerMode mode)
    {
        if (!IsServer) return;
        currentMode.Value = mode;
        ApplyMode(mode);
    }

    void ShowPlayer()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = true;

        var colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
            c.enabled = true;
    }

    IEnumerator ApplyMinigamePhysics()
    {
        yield return null;

        if (IsServer)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (netRb != null)
            netRb.enabled = true;
    }

    IEnumerator ApplyBoardPhysics()
    {
        yield return null;

        if (IsServer)
        {
            rb.isKinematic = true;
            rb.useGravity = false;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (netRb != null)
            netRb.enabled = false;
    }



    void DisableAllModes()
    {
        if (minigameController) minigameController.enabled = false;
        if (snowController) snowController.enabled = false;
        if (doubleAgentController) doubleAgentController.enabled = false;
    }

}