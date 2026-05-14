using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;

public enum CameraMode
{
    Owner,
    Turn
}

public class CameraManager : NetworkBehaviour
{
    public static CameraManager Instance;

    private CinemachineCamera currentCam;
    private Transform currentTarget;

    private bool isInitialized = false;

    private CameraMode currentMode = CameraMode.Owner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public CameraMode GetMode()
    {
        return currentMode;
    }

    public void SetMode(CameraMode mode)
    {
        currentMode = mode;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isInitialized = false;
        currentCam = null;

        StartCoroutine(InitCameraRoutine());
    }

    IEnumerator InitCameraRoutine()
    {
        yield return new WaitUntil(() => FindObjectOfType<CinemachineCamera>() != null);

        currentCam = FindObjectOfType<CinemachineCamera>();
        isInitialized = true;

        if (currentTarget != null)
        {
            ApplyTarget();
        }

        Debug.Log("✅ Camera Initialized");
    }

    public void RegisterPlayer(Transform player)
    {
        if (currentMode != CameraMode.Owner)
            return;

        currentTarget = player;

        if (!isInitialized)
            return;

        ApplyTarget();
    }

    void ApplyTarget()
    {
        if (currentCam == null || currentTarget == null)
        {
            Debug.LogWarning("Camera or Target missing");
            return;
        }

        currentCam.Follow = currentTarget;
        currentCam.LookAt = currentTarget;
    }

    public void SetTarget(Transform target)
    {
        if (currentMode != CameraMode.Turn)
            return;

        currentTarget = target;

        if (!isInitialized)
            return;

        ApplyTarget();
    }
}