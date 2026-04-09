using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardOnlyRebindEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _actionNameText;
    [SerializeField] private TMP_Text _bindingValueText;
    [SerializeField] private Button _rebindButton;
    [SerializeField] private Button _clearButton;

    public void Setup(string actionName, string bindingValue, UnityEngine.Events.UnityAction onRebindPressed, UnityEngine.Events.UnityAction onClearPressed)
    {
        if (_actionNameText != null)
            _actionNameText.text = actionName;

        if (_bindingValueText != null)
            _bindingValueText.text = bindingValue;

        if (_rebindButton != null)
        {
            _rebindButton.onClick.RemoveAllListeners();
            _rebindButton.onClick.AddListener(onRebindPressed);
        }

        if (_clearButton != null)
        {
            _clearButton.onClick.RemoveAllListeners();
            _clearButton.onClick.AddListener(onClearPressed);
        }
    }
}
