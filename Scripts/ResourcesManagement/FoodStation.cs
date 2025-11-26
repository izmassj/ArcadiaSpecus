// FoodStation.cs
using UnityEngine;
using System.Collections.Generic;

public class FoodStation : WorkStation
{
    [Header("Configuración Dispensador Comida")]
    public int maxEatingNPCs = 3;
    public int foodConsumptionAmount = 5; // COMIDA CONSUMIDA por NPC por segundo
    public float eatingDuration = 5f;

    private List<DwellerNPC> eatingNPCs = new List<DwellerNPC>();
    private float consumptionTimer = 0f;

    void Start()
    {
        if (string.IsNullOrEmpty(stationId))
            stationId = System.Guid.NewGuid().ToString();

        stationName = "Dispensador de Comida";
        isConsumptionStation = true; // Marcar como estación de consumo
    }

    void Update()
    {
        if (eatingNPCs.Count > 0)
        {
            consumptionTimer += Time.deltaTime;

            // Procesar consumo cada segundo
            if (consumptionTimer >= 1f)
            {
                ProcessFoodConsumption();
                consumptionTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Procesa el consumo de comida por parte de los NPCs
    /// </summary>
    void ProcessFoodConsumption()
    {
        foreach (var npc in eatingNPCs)
        {
            if (npc != null && npc.needs != null)
            {
                // Verificar si hay comida disponible
                if (ResourceManager.Instance.ConsumeResource(ResourceType.Food, foodConsumptionAmount))
                {
                    // Aplicar recuperación de hambre
                    npc.needs.Eat(Time.deltaTime);

                    // Debug opcional
                    if (Time.frameCount % 200 == 0)
                    {
                        Debug.Log($"{npc.dwellerName} comiendo (H:{(int)npc.needs.hunger}) - Consumió {foodConsumptionAmount} comida");
                    }
                }
                else
                {
                    Debug.LogWarning($"{npc.dwellerName} no puede comer - Sin comida disponible");
                    // El NPC puede permanecer pero no recuperarse sin comida
                }
            }
        }
    }

    /// <summary>
    /// Verifica si la estación puede aceptar más NPCs
    /// </summary>
    public bool CanAcceptNPC()
    {
        return eatingNPCs.Count < maxEatingNPCs;
    }

    /// <summary>
    /// Asigna un NPC para comer en esta estación
    /// </summary>
    public void AssignEatingNPC(DwellerNPC npc)
    {
        if (!eatingNPCs.Contains(npc) && CanAcceptNPC())
        {
            eatingNPCs.Add(npc);
            npc.transform.position = GetEatingPosition(eatingNPCs.Count - 1);
            Debug.Log($"{npc.dwellerName} comiendo en {stationName} (H:{(int)npc.needs.hunger})");
        }
    }

    /// <summary>
    /// Remueve un NPC de la estación de comida
    /// </summary>
    public void RemoveEatingNPC(DwellerNPC npc)
    {
        if (eatingNPCs.Contains(npc))
        {
            eatingNPCs.Remove(npc);
            Debug.Log($"{npc.dwellerName} terminó de comer en {stationName} (H:{(int)npc.needs.hunger})");
        }
    }

    /// <summary>
    /// Obtiene la posición para comer basada en el índice
    /// </summary>
    Vector3 GetEatingPosition(int index)
    {
        Vector3[] positions = {
            transform.position + transform.right * -1f,
            transform.position,
            transform.position + transform.right * 1f
        };
        return index < positions.Length ? positions[index] : transform.position;
    }

    /// <summary>
    /// Obtiene la posición principal para comer
    /// </summary>
    public Vector3 GetFoodPosition()
    {
        return GetEatingPosition(0);
    }

    /// <summary>
    /// Obtiene el número de NPCs actualmente comiendo
    /// </summary>
    public int GetEatingNPCCount()
    {
        return eatingNPCs.Count;
    }
}