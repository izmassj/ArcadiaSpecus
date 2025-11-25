// NPCStatusPanel.cs
using UnityEngine;
using TMPro;

public class NPCStatusPanel : MonoBehaviour
{
    [System.Serializable]
    public class NPCRow
    {
        public TextMeshProUGUI nameText;
        public NPCStatIndicator hungerIndicator;
        public NPCStatIndicator thirstIndicator;
        public NPCStatIndicator fatigueIndicator;
    }

    [Header("UI General")]
    public TextMeshProUGUI totalDwellersText;

    [Header("Filas de NPC")]
    public NPCRow[] npcRows;

    private DwellerNPC[] npcs;

    void Start()
    {
        npcs = FindObjectsOfType<DwellerNPC>();

        UpdateUI();
        // Actualizar periódicamente cada 0.5 segundos
        InvokeRepeating(nameof(UpdateUI), 1f, 0.5f);
    }

    /// <summary>
    /// Actualiza toda la interfaz de usuario del panel de estado
    /// </summary>
    public void UpdateUI()
    {
        npcs = FindObjectsOfType<DwellerNPC>();

        // Mostrar total habitantes correctamente
        if (totalDwellersText != null)
        {
            totalDwellersText.text = "HABITANTES: " + npcs.Length;
        }

        // Actualizar cada fila con el NPC correspondiente
        for (int i = 0; i < npcRows.Length; i++)
        {
            NPCRow row = npcRows[i];

            // Si hay menos NPCs que filas, vaciar la fila
            if (i >= npcs.Length)
            {
                if (row.nameText != null)
                    row.nameText.text = "-";

                if (row.hungerIndicator != null)
                    row.hungerIndicator.SetValue(0);

                if (row.thirstIndicator != null)
                    row.thirstIndicator.SetValue(0);

                if (row.fatigueIndicator != null)
                    row.fatigueIndicator.SetValue(0);

                continue;
            }

            DwellerNPC npc = npcs[i];

            // Nombre del NPC
            if (row.nameText != null)
                row.nameText.text = npc.dwellerName;

            // Indicadores H / S / F
            if (row.hungerIndicator != null)
                row.hungerIndicator.SetValue(npc.needs.hunger);

            if (row.thirstIndicator != null)
                row.thirstIndicator.SetValue(npc.needs.thirst);

            if (row.fatigueIndicator != null)
                row.fatigueIndicator.SetValue(npc.needs.fatigue);
        }
    }
}