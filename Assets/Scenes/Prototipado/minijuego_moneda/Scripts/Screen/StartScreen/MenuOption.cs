using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuOption : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TMP_Text text;

    private string baseText;

    private void Awake()
    {
        if (text != null)
            baseText = CleanPrefix(text.text);
    }

    private void OnEnable()
    {
        if (text != null)
        {
            baseText = CleanPrefix(text.text);
            text.text = baseText;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (text == null) return;

        baseText = CleanPrefix(baseText);
        text.text = "> " + baseText;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (text == null) return;

        baseText = CleanPrefix(baseText);
        text.text = baseText;
    }

    private string CleanPrefix(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.StartsWith("> "))
            return value.Substring(2);

        return value;
    }
}
