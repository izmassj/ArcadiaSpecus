using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("Panel de este menú")]
    [SerializeField] private GameObject thisPanel;

    [Header("Primer botón por defecto")]
    [SerializeField] private Button firstButton;

    private Button lastSelectedButton;

    public void OpenMenu()
    {
        Debug.Log($"OpenMenu llamado en: {name}");

        if (thisPanel != null)
            thisPanel.SetActive(true);

        StartCoroutine(SelectButtonRoutine());
    }

    public void CloseMenu()
    {
        if (thisPanel != null)
            thisPanel.SetActive(false);
    }

    public void ResetToFirstButton()
    {
        lastSelectedButton = firstButton;
    }

    private IEnumerator SelectButtonRoutine()
    {
        yield return null;
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);

        yield return null;

        Button target = lastSelectedButton;

        if (target == null || !target.transform.IsChildOf(thisPanel.transform))
            target = firstButton;

        if (target == null)
        {
            Debug.LogError($"[{name}] No hay botón objetivo. Asigna firstButton.");
            yield break;
        }

        if (!target.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[{name}] El botón objetivo no está activo en jerarquía: {target.name}");
            yield break;
        }

        if (!target.interactable)
        {
            Debug.LogError($"[{name}] El botón objetivo no es interactable: {target.name}");
            yield break;
        }

        EventSystem.current.SetSelectedGameObject(target.gameObject);

        Debug.Log($"[{name}] Seleccionado botón: {target.name}");
    }

    private void Update()
    {
        if (thisPanel == null || !thisPanel.activeInHierarchy)
            return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current == null)
            return;

        if (!current.transform.IsChildOf(thisPanel.transform))
            return;

        Button currentButton = current.GetComponent<Button>();
        if (currentButton != null)
            lastSelectedButton = currentButton;
    }
}