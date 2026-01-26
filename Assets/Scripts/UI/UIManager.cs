using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject _resourcesPanel;
    [SerializeField] private GameObject _npcStatusPanel;

    private NPCStatusPanel _cachedNPCStatusPanel;

    private void Start()
    {
        CacheUIComponents();
    }

    private void CacheUIComponents()
    {
        _cachedNPCStatusPanel = FindObjectOfType<NPCStatusPanel>();
    }

    public void RefreshAllDisplays()
    {
        RefreshNPCStatusPanel();
    }

    private void RefreshNPCStatusPanel()
    {
        if (_cachedNPCStatusPanel != null)
        {
            _cachedNPCStatusPanel.UpdateUI();
        }
        else
        {
            _cachedNPCStatusPanel = FindObjectOfType<NPCStatusPanel>();
            if (_cachedNPCStatusPanel != null)
                _cachedNPCStatusPanel.UpdateUI();
        }
    }

    public void SetResourcesPanelVisible(bool visible)
    {
        if (_resourcesPanel != null)
            _resourcesPanel.SetActive(visible);
    }

    public void SetNPCStatusPanelVisible(bool visible)
    {
        if (_npcStatusPanel != null)
            _npcStatusPanel.SetActive(visible);
    }
}