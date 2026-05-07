using UnityEngine;
using TMPro;

public class RelayUI : MonoBehaviour
{
    public TMP_InputField joinInput;

    public async void OnClickCreate()
    {
        string code = await RelayManager.Instance.CreateRelay();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("Failed to create relay");
        }
    }

    public async void OnClickJoin()
    {
        string code = joinInput.text;

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("Join code empty");
            return;
        }

        await RelayManager.Instance.JoinRelay(code);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}