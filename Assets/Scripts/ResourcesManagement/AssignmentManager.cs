// AssignmentManager.cs
using UnityEngine;
using System.Collections.Generic;

public class AssignmentManager : MonoBehaviour
{
    public static AssignmentManager Instance;

    [Header("Referencias")]
    public List<DwellerNPC> allDwellers = new List<DwellerNPC>();
    public List<WorkStation> allWorkStations = new List<WorkStation>();

    private Dictionary<DwellerNPC, WorkStation> currentAssignments = new Dictionary<DwellerNPC, WorkStation>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindAllDwellersAndStations();
        // Asignación automática después de un breve delay
        Invoke("AutoAssignAll", 2f);

        // NUEVO: Suscribir a eventos de muerte de NPCs
        ResourceManager.OnNPCDied += HandleNPCDied;
    }

    void OnDestroy()
    {
        // NUEVO: Limpiar suscripción
        ResourceManager.OnNPCDied -= HandleNPCDied;
    }

    /// <summary>
    /// NUEVO: Maneja la muerte de un NPC - remueve sus asignaciones
    /// </summary>
    private void HandleNPCDied(DwellerNPC deadNPC)
    {
        if (deadNPC == null) return;

        // Remover asignaciones del NPC muerto
        if (currentAssignments.ContainsKey(deadNPC))
        {
            WorkStation assignedStation = currentAssignments[deadNPC];
            if (assignedStation != null)
            {
                assignedStation.RemoveWorker(deadNPC);
            }
            currentAssignments.Remove(deadNPC);
            Debug.Log($"🗑️ Removidas asignaciones de {deadNPC.dwellerName} (MUERTO)");
        }

        // Remover de la lista de NPCs
        if (allDwellers.Contains(deadNPC))
        {
            allDwellers.Remove(deadNPC);
        }

        // Reasignar trabajadores restantes si es necesario
        Invoke("ReassignAfterDeath", 1f);
    }

    /// <summary>
    /// NUEVO: Reasigna trabajadores después de una muerte
    /// </summary>
    private void ReassignAfterDeath()
    {
        Debug.Log($"🔄 Reasignando trabajadores después de muerte. NPCs vivos: {allDwellers.Count}");
        AutoAssignAll();
    }

    /// <summary>
    /// Encuentra todos los NPCs y estaciones de trabajo en la escena
    /// </summary>
    void FindAllDwellersAndStations()
    {
        allDwellers.Clear();
        allWorkStations.Clear();

        DwellerNPC[] allFoundDwellers = FindObjectsOfType<DwellerNPC>();

        // NUEVO: Filtrar NPCs muertos - solo incluir NPCs vivos
        foreach (var dweller in allFoundDwellers)
        {
            if (!dweller.IsDead)
            {
                allDwellers.Add(dweller);
            }
            else
            {
                Debug.Log($"🚫 Excluyendo {dweller.dwellerName} de asignaciones (MUERTO)");
            }
        }

        WorkStation[] allStations = FindObjectsOfType<WorkStation>();
        foreach (var station in allStations)
        {
            // Excluir estaciones de descanso de la lista de trabajo
            if (!(station is RestStation))
            {
                allWorkStations.Add(station);
            }
        }

        Debug.Log($"Encontrados {allDwellers.Count} NPCs vivos y {allWorkStations.Count} máquinas de trabajo (excluyendo camas)");
    }

    /// <summary>
    /// Asigna un NPC a una estación de trabajo específica
    /// </summary>
    public void AssignDwellerToStation(DwellerNPC dweller, WorkStation station)
    {
        // NUEVO: Verificar si el NPC está muerto
        if (dweller != null && dweller.IsDead)
        {
            Debug.LogWarning($"No se puede asignar {dweller.dwellerName} - ESTÁ MUERTO");
            return;
        }

        if (dweller == null || station == null) return;

        // Prevenir asignación a camas mediante este manager
        if (station is RestStation)
        {
            Debug.LogWarning($"No asignar {dweller.dwellerName} a cama mediante AssignmentManager");
            return;
        }

        // No reasignar si ya está asignado y trabajando efectivamente
        if (currentAssignments.ContainsKey(dweller) &&
            currentAssignments[dweller] == station &&
            dweller.CanWorkEffectively())
        {
            return;
        }

        // Remover asignación previa si existe
        if (currentAssignments.ContainsKey(dweller))
        {
            currentAssignments[dweller].RemoveWorker(dweller);
            currentAssignments.Remove(dweller);
        }

        // Realizar nueva asignación
        dweller.AssignToWorkStation(station);
        currentAssignments[dweller] = station;

        Debug.Log($"{dweller.dwellerName} asignado a {station.stationName}");
    }

    /// <summary>
    /// Remueve la asignación de trabajo de un NPC
    /// </summary>
    public void UnassignDweller(DwellerNPC dweller)
    {
        // NUEVO: Verificar si el NPC está muerto
        if (dweller != null && dweller.IsDead)
        {
            Debug.LogWarning($"No se puede desasignar {dweller.dwellerName} - ESTÁ MUERTO");
            return;
        }

        if (currentAssignments.ContainsKey(dweller))
        {
            currentAssignments[dweller].RemoveWorker(dweller);
            currentAssignments.Remove(dweller);
            dweller.AssignToWorkStation(null);
            Debug.Log($"{dweller.dwellerName} desasignado de trabajo");
        }
    }

    /// <summary>
    /// Asigna automáticamente todos los NPCs disponibles a estaciones de trabajo
    /// </summary>
    public void AutoAssignAll()
    {
        // NUEVO: Actualizar lista de NPCs vivos
        FindAllDwellersAndStations();

        var unassignedDwellers = new List<DwellerNPC>(allDwellers);
        // No remover NPCs que ya están asignados y trabajando bien
        unassignedDwellers.RemoveAll(d => currentAssignments.ContainsKey(d) && d.CanWorkEffectively());

        var availableStations = new List<WorkStation>(allWorkStations);

        Debug.Log($"Iniciando auto-asignación: {unassignedDwellers.Count} NPCs libres, {availableStations.Count} máquinas disponibles");

        // NUEVO: Filtrar NPCs muertos otra vez por seguridad
        unassignedDwellers.RemoveAll(d => d.IsDead);

        // Asignación por tipo de recurso necesario
        foreach (var dweller in unassignedDwellers)
        {
            if (availableStations.Count == 0) break;

            // NUEVO: Verificar que el NPC no haya muerto durante el proceso
            if (dweller.IsDead) continue;

            WorkStation bestStation = FindBestStationForDweller(dweller, availableStations);
            if (bestStation != null)
            {
                AssignDwellerToStation(dweller, bestStation);
                availableStations.Remove(bestStation);
            }
        }

        Debug.Log($"Auto-asignación completada. {Mathf.Min(unassignedDwellers.Count, allWorkStations.Count)} NPCs asignados a trabajo");
    }

    /// <summary>
    /// Encuentra la mejor estación de trabajo para un NPC basado en sus necesidades
    /// </summary>
    private WorkStation FindBestStationForDweller(DwellerNPC dweller, List<WorkStation> availableStations)
    {
        // NUEVO: Verificar que el NPC esté vivo
        if (dweller.IsDead) return null;

        // Si el NPC tiene una necesidad crítica, priorizar estaciones que produzcan ese recurso
        if (dweller.NeedsRecovery())
        {
            ResourceType criticalNeed = dweller.GetMostCriticalNeed();
            WorkStation needStation = availableStations.Find(s => s.producedResource == criticalNeed);
            if (needStation != null) return needStation;
        }

        // Buscar estaciones con menos trabajadores para balancear la carga
        WorkStation bestStation = null;
        int minWorkers = int.MaxValue;

        foreach (var station in availableStations)
        {
            int workerCount = station.GetAssignedWorkers().Count;
            if (workerCount < minWorkers)
            {
                minWorkers = workerCount;
                bestStation = station;
            }
        }

        return bestStation ?? (availableStations.Count > 0 ? availableStations[0] : null);
    }

    /// <summary>
    /// Encuentra una estación que produzca un tipo específico de recurso
    /// </summary>
    public WorkStation FindStationByResource(ResourceType resourceType)
    {
        return allWorkStations.Find(station => station.producedResource == resourceType);
    }

    /// <summary>
    /// Obtiene la estación asignada a un NPC específico
    /// </summary>
    public WorkStation GetAssignedStation(DwellerNPC dweller)
    {
        // NUEVO: Verificar si el NPC está muerto
        if (dweller != null && dweller.IsDead)
        {
            Debug.LogWarning($"No se puede obtener asignación de {dweller.dwellerName} - ESTÁ MUERTO");
            return null;
        }

        return currentAssignments.ContainsKey(dweller) ? currentAssignments[dweller] : null;
    }

    /// <summary>
    /// Verifica si un NPC está asignado a alguna estación
    /// </summary>
    public bool IsDwellerAssigned(DwellerNPC dweller)
    {
        // NUEVO: Verificar si el NPC está muerto
        if (dweller != null && dweller.IsDead)
        {
            return false; // NPCs muertos no están asignados
        }

        return currentAssignments.ContainsKey(dweller);
    }

    /// <summary>
    /// Obtiene todas las asignaciones actuales
    /// </summary>
    public Dictionary<DwellerNPC, WorkStation> GetAllAssignments()
    {
        // NUEVO: Devolver solo asignaciones de NPCs vivos
        var aliveAssignments = new Dictionary<DwellerNPC, WorkStation>();
        foreach (var assignment in currentAssignments)
        {
            if (!assignment.Key.IsDead)
            {
                aliveAssignments.Add(assignment.Key, assignment.Value);
            }
        }
        return aliveAssignments;
    }

    /// <summary>
    /// Reasigna todos los NPCs a estaciones de trabajo
    /// </summary>
    public void ReassignAllDwellers()
    {
        // NUEVO: Solo desasignar NPCs vivos
        var aliveDwellersToUnassign = new List<DwellerNPC>();
        foreach (var assignment in currentAssignments)
        {
            if (!assignment.Key.IsDead)
            {
                aliveDwellersToUnassign.Add(assignment.Key);
            }
        }

        foreach (var dweller in aliveDwellersToUnassign)
        {
            dweller.AssignToWorkStation(null);
        }
        currentAssignments.Clear();

        AutoAssignAll();
    }

    /// <summary>
    /// Reasigna solo los NPCs que tienen problemas para trabajar efectivamente
    /// </summary>
    [ContextMenu("Reasignar Solo NPCs Problemáticos")]
    public void ReassignProblematicDwellers()
    {
        var problematicDwellers = new List<DwellerNPC>();

        foreach (var dweller in allDwellers)
        {
            // NUEVO: Excluir NPCs muertos
            if (dweller.IsDead) continue;

            if (dweller.NeedsRecovery() && !dweller.IsRecovering())
            {
                problematicDwellers.Add(dweller);
            }
        }

        Debug.Log($"Reasignando {problematicDwellers.Count} NPCs problemáticos");

        foreach (var dweller in problematicDwellers)
        {
            UnassignDweller(dweller);
        }

        // Reasignar después de un breve delay
        Invoke("AutoAssignAll", 1f);
    }

    /// <summary>
    /// NUEVO: Obtiene la cantidad de NPCs asignados actualmente
    /// </summary>
    public int GetAssignedDwellerCount()
    {
        int count = 0;
        foreach (var assignment in currentAssignments)
        {
            if (!assignment.Key.IsDead)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// NUEVO: Obtiene la cantidad de NPCs vivos totales
    /// </summary>
    public int GetAliveDwellerCount()
    {
        return allDwellers.Count;
    }

    /// <summary>
    /// NUEVO: Reinicia todas las asignaciones (para reinicio de juego)
    /// </summary>
    public void ResetAllAssignments()
    {
        currentAssignments.Clear();
        FindAllDwellersAndStations();
        Debug.Log("🔄 Todas las asignaciones reiniciadas");
    }
}