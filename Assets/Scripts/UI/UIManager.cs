using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject resourcesPanel;
    public GameObject npcStatusPanel;

    void Start()
    {
        // UI Manager inicializado
    }

    public void RefreshAllDisplays()
    {
        NPCStatusPanel npcPanel = FindObjectOfType<NPCStatusPanel>();
        if (npcPanel != null)
            npcPanel.UpdateUI();
    }

}