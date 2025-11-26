// WaterStation.cs
using UnityEngine;
using System.Collections.Generic;

public class WaterStation : WorkStation
{
    [Header("Configuración Dispensador Agua")]
    public int maxDrinkingNPCs = 3;
    public int waterConsumptionAmount = 3; // AGUA CONSUMIDA por NPC por segundo
    public float drinkingDuration = 3f;

    private List<DwellerNPC> drinkingNPCs = new List<DwellerNPC>();
    private float consumptionTimer = 0f;

    void Start()
    {
        if (string.IsNullOrEmpty(stationId))
            stationId = System.Guid.NewGuid().ToString();

        stationName = "Dispensador de Agua";
        isConsumptionStation = true; // Marcar como estación de consumo
    }

    void Update()
    {
        if (drinkingNPCs.Count > 0)
        {
            consumptionTimer += Time.deltaTime;

            // Procesar consumo cada segundo
            if (consumptionTimer >= 1f)
            {
                ProcessWaterConsumption();
                consumptionTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Procesa el consumo de agua por parte de los NPCs
    /// </summary>
    void ProcessWaterConsumption()
    {
        foreach (var npc in drinkingNPCs)
        {
            if (npc != null && npc.needs != null)
            {
                // Verificar si hay agua disponible
                if (ResourceManager.Instance.ConsumeResource(ResourceType.Water, waterConsumptionAmount))
                {
                    // Aplicar recuperación de sed
                    npc.needs.Drink(Time.deltaTime);

                    // Debug opcional
                    if (Time.frameCount % 200 == 0)
                    {
                        Debug.Log($"{npc.dwellerName} bebiendo (S:{(int)npc.needs.thirst}) - Consumió {waterConsumptionAmount} agua");
                    }
                }
                else
                {
                    Debug.LogWarning($"{npc.dwellerName} no puede beber - Sin agua disponible");
                    // El NPC puede permanecer pero no recuperarse sin agua
                }
            }
        }
    }

    /// <summary>
    /// Verifica si la estación puede aceptar más NPCs
    /// </summary>
    public bool CanAcceptNPC()
    {
        return drinkingNPCs.Count < maxDrinkingNPCs;
    }

    /// <summary>
    /// Asigna un NPC para beber en esta estación
    /// </summary>
    public void AssignDrinkingNPC(DwellerNPC npc)
    {
        if (!drinkingNPCs.Contains(npc) && CanAcceptNPC())
        {
            drinkingNPCs.Add(npc);
            npc.transform.position = GetDrinkingPosition(drinkingNPCs.Count - 1);
            Debug.Log($"{npc.dwellerName} bebiendo en {stationName} (S:{(int)npc.needs.thirst})");
        }
    }

    /// <summary>
    /// Remueve un NPC de la estación de agua
    /// </summary>
    public void RemoveDrinkingNPC(DwellerNPC npc)
    {
        if (drinkingNPCs.Contains(npc))
        {
            drinkingNPCs.Remove(npc);
            Debug.Log($"{npc.dwellerName} terminó de beber en {stationName} (S:{(int)npc.needs.thirst})");
        }
    }

    /// <summary>
    /// Obtiene la posición para beber basada en el índice
    /// </summary>
    Vector3 GetDrinkingPosition(int index)
    {
        Vector3[] positions = {
            transform.position + transform.forward * -1f,
            transform.position,
            transform.position + transform.forward * 1f
        };
        return index < positions.Length ? positions[index] : transform.position;
    }

    /// <summary>
    /// Obtiene la posición principal para beber
    /// </summary>
    public Vector3 GetWaterPosition()
    {
        return GetDrinkingPosition(0);
    }

    /// <summary>
    /// Obtiene el número de NPCs actualmente bebiendo
    /// </summary>
    public int GetDrinkingNPCCount()
    {
        return drinkingNPCs.Count;
    }
}