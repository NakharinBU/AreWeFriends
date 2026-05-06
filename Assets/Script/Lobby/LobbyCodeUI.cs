using UnityEngine;
using TMPro;
using Unity.Collections;

public class LobbyCodeUI : MonoBehaviour
{
    public TextMeshProUGUI codeText;

    void Start()
    {
        /*if (RelayManager.Instance != null)
        {
            codeText.text = RelayManager.Instance.joinCodeNet.Value.ToString();
        }

        RelayManager.Instance.joinCodeNet.OnValueChanged += OnCodeChanged;*/

        codeText.text = RelayManager.Instance.CurrentJoinCode;
    }

    void OnCodeChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        codeText.text = newValue.ToString();
    }

    public void CopyCode()
    {
        GUIUtility.systemCopyBuffer = RelayManager.Instance.CurrentJoinCode;
    }
}