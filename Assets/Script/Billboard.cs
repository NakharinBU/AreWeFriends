using UnityEngine;
using Unity.Cinemachine;

public class Billboard : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2f, 0);

    Camera cam;

    void Start()
    {
        var brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null)
            cam = brain.OutputCamera;
    }

    void LateUpdate()
    {
        if (cam == null || target == null) return;

        Vector3 worldPos = target.position + offset;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            gameObject.SetActive(true);
        }

        transform.position = screenPos;
    }
}