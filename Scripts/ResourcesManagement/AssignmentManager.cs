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
    }

    /// <summary>
    /// Encuentra todos los NPCs y estaciones de trabajo en la escena
    /// </summary>
    void FindAllDwellersAndStations()
    {
        allDwellers.Clear();
        allWorkStations.Clear();

        allDwellers.AddRange(FindObjectsOfType<DwellerNPC>());

        WorkStation[] allStations = FindObjectsOfType<WorkStation>();
        foreach (var station in allStations)
        {
            // Excluir estaciones de descanso de la lista de trabajo
            if (!(station is RestStation))
            {
                allWorkStations.Add(station);
            }
        }

        Debug.Log($"Encontrados {allDwellers.Count} NPCs y {allWorkStations.Count} máquinas de trabajo (excluyendo camas)");
    }

    /// <summary>
    /// Asigna un NPC a una estación de trabajo específica
    /// </summary>
    public void AssignDwellerToStation(DwellerNPC dweller, WorkStation station)
    {
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
        if (currentAssignments.ContainsKey(dweller))
        {
            currentAssignments[dweller].RemoveWorker(dweller);
            currentAssignments.Remove(dweller);
            dweller.AssignToWorkStation(null);
        }
    }

    /// <summary>
    /// Asigna automáticamente todos los NPCs disponibles a estaciones de trabajo
    /// </summary>
    public void AutoAssignAll()
    {
        FindAllDwellersAndStations();

        var unassignedDwellers = new List<DwellerNPC>(allDwellers);
        // No remover NPCs que ya están asignados y trabajando bien
        unassignedDwellers.RemoveAll(d => currentAssignments.ContainsKey(d) && d.CanWorkEffectively());

        var availableStations = new List<WorkStation>(allWorkStations);

        Debug.Log($"Iniciando auto-asignación: {unassignedDwellers.Count} NPCs libres, {availableStations.Count} máquinas disponibles");

        // Asignación por tipo de recurso necesario
        foreach (var dweller in unassignedDwellers)
        {
            if (availableStations.Count == 0) break;

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
        return currentAssignments.ContainsKey(dweller) ? currentAssignments[dweller] : null;
    }

    /// <summary>
    /// Verifica si un NPC está asignado a alguna estación
    /// </summary>
    public bool IsDwellerAssigned(DwellerNPC dweller)
    {
        return currentAssignments.ContainsKey(dweller);
    }

    /// <summary>
    /// Obtiene todas las asignaciones actuales
    /// </summary>
    public Dictionary<DwellerNPC, WorkStation> GetAllAssignments()
    {
        return new Dictionary<DwellerNPC, WorkStation>(currentAssignments);
    }

    /// <summary>
    /// Reasigna todos los NPCs a estaciones de trabajo
    /// </summary>
    public void ReassignAllDwellers()
    {
        foreach (var assignment in currentAssignments)
        {
            assignment.Key.AssignToWorkStation(null);
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
}