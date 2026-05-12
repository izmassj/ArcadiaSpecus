using UnityEngine;
using UnityEngine.UI;

public class UIButtonScaleFeedbackInstaller : MonoBehaviour
{
    [SerializeField] private Transform _root;
    [SerializeField] private bool _includeInactive = true;

    private void Awake()
    {
        Install();
    }

    [ContextMenu("Install Button Feedback")]
    public void Install()
    {
        Transform targetRoot = _root != null ? _root : transform;
        Button[] buttons = targetRoot.GetComponentsInChildren<Button>(_includeInactive);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            if (button.GetComponent<UIButtonScaleFeedback>() != null)
                continue;

            button.gameObject.AddComponent<UIButtonScaleFeedback>();
        }
    }
}