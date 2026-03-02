using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tag runtime para una fila de remapping (para refrescar labels).
/// </summary>
public class RemapRowTag : MonoBehaviour
{
    public string ActionName { get; private set; }
    public TMP_Text KbmLabel { get; private set; }
    public TMP_Text PadLabel { get; private set; }
    public InputAction Action { get; private set; }

    public void Init(string actionName, TMP_Text kbm, TMP_Text pad, InputAction action)
    {
        ActionName = actionName;
        KbmLabel = kbm;
        PadLabel = pad;
        Action = action;
    }
}
