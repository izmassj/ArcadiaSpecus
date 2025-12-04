// RestStation.cs
using UnityEngine;
using System.Collections.Generic;

public class RestStation : WorkStation
{
    [Header("Configuración Descanso")]
    public int maxRestingNPCs = 2;
    public float restEfficiency = 2.0f;
    public float fatigueRecoveryRate = 40f;

    private List<DwellerNPC> restingNPCs = new List<DwellerNPC>();

    void Start()
    {
        if (string.IsNullOrEmpty(stationId))
            stationId = System.Guid.NewGuid().ToString();

        stationName = "Cama Descanso";
        isConsumptionStation = true; // Marcar como estación de consumo
    }

    void Update()
    {
        // Aplicar recuperación de fatiga a todos los NPCs descansando
        foreach (var npc in restingNPCs)
        {
            if (npc != null && npc.needs != null)
            {
                npc.needs.Rest(Time.deltaTime * restEfficiency);

                // Debug opcional cada 5 segundos
                if (Time.frameCount % 300 == 0)
                {
                    Debug.Log($"{npc.dwellerName} descansando (F:{(int)npc.needs.fatigue})");
                }
            }
        }
    }

    /// <summary>
    /// Verifica si la estación puede aceptar más NPCs
    /// </summary>
    public bool CanAcceptNPC()
    {
        return restingNPCs.Count < maxRestingNPCs;
    }

    /// <summary>
    /// Asigna un NPC para descansar en esta estación
    /// </summary>
    public void AssignRestingNPC(DwellerNPC npc)
    {
        if (!restingNPCs.Contains(npc) && CanAcceptNPC())
        {
            restingNPCs.Add(npc);
            npc.transform.position = GetRestingPosition(restingNPCs.Count - 1);
            Debug.Log($"{npc.dwellerName} se acostó en {stationName} (F:{(int)npc.needs.fatigue})");
        }
    }

    /// <summary>
    /// Remueve un NPC de la estación de descanso
    /// </summary>
    public void RemoveRestingNPC(DwellerNPC npc)
    {
        if (restingNPCs.Contains(npc))
        {
            restingNPCs.Remove(npc);
            Debug.Log($"{npc.dwellerName} se levantó de {stationName} (F:{(int)npc.needs.fatigue})");
        }
    }

    /// <summary>
    /// Obtiene la posición de descanso basada en el índice
    /// </summary>
    Vector3 GetRestingPosition(int index)
    {
        Vector3[] positions = {
            transform.position + transform.forward * 1f,
            transform.position + transform.forward * -1f
        };
        return index < positions.Length ? positions[index] : transform.position;
    }

    /// <summary>
    /// Obtiene la posición principal para descansar
    /// </summary>
    public Vector3 GetRestPosition()
    {
        return GetRestingPosition(0);
    }

    /// <summary>
    /// Verifica si un NPC debería dejar la estación de descanso
    /// </summary>
    public bool ShouldNPCLeave(DwellerNPC npc)
    {
        return npc.needs.fatigue <= 5f;
    }

    /// <summary>
    /// Obtiene el número de NPCs actualmente descansando
    /// </summary>
    public int GetRestingNPCCount()
    {
        return restingNPCs.Count;
    }
}