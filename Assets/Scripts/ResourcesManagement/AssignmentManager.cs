// AssignmentManager.cs
using UnityEngine;
using System.Collections.Generic;

public class AssignmentManager : MonoBehaviour
{
    public static AssignmentManager Instance;

    [Header("References")]
    public List<DwellerNPC> allDwellers = new List<DwellerNPC>();
    public List<WorkStation> allWorkStations = new List<WorkStation>();

    private Dictionary<DwellerNPC, WorkStation> currentAssignments = new Dictionary<DwellerNPC, WorkStation>();

    /// <summary>
    /// Initializes the AssignmentManager as a singleton
    /// </summary>
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

    /// <summary>
    /// Sets up initial assignments and subscribes to NPC death events
    /// </summary>
    void Start()
    {
        FindAllDwellersAndStations();
        // Automatic assignment after a brief delay
        Invoke("AutoAssignAll", 2f);

        // Subscribe to NPC death events
        ResourceManager.OnNPCDied += HandleNPCDied;
    }

    /// <summary>
    /// Cleans up event subscriptions when destroyed
    /// </summary>
    void OnDestroy()
    {
        // Clean up subscription
        ResourceManager.OnNPCDied -= HandleNPCDied;
    }

    /// <summary>
    /// Handles NPC death - removes their assignments
    /// </summary>
    /// <param name="deadNPC">The NPC that died</param>
    private void HandleNPCDied(DwellerNPC deadNPC)
    {
        if (deadNPC == null) return;

        // Remove assignments from dead NPC
        if (currentAssignments.ContainsKey(deadNPC))
        {
            WorkStation assignedStation = currentAssignments[deadNPC];
            if (assignedStation != null)
            {
                assignedStation.RemoveWorker(deadNPC);
            }
            currentAssignments.Remove(deadNPC);
            Debug.Log($"Removed assignments from {deadNPC.dwellerName} (DEAD)");
        }

        // Remove from NPC list
        if (allDwellers.Contains(deadNPC))
        {
            allDwellers.Remove(deadNPC);
        }

        // Reassign remaining workers if necessary
        Invoke("ReassignAfterDeath", 1f);
    }

    /// <summary>
    /// Reassigns workers after an NPC death
    /// </summary>
    private void ReassignAfterDeath()
    {
        Debug.Log($"Reassigning workers after death. Alive NPCs: {allDwellers.Count}");
        AutoAssignAll();
    }

    /// <summary>
    /// Finds all NPCs and work stations in the scene
    /// </summary>
    void FindAllDwellersAndStations()
    {
        allDwellers.Clear();
        allWorkStations.Clear();

        DwellerNPC[] allFoundDwellers = FindObjectsOfType<DwellerNPC>();

        // Filter dead NPCs - only include alive NPCs
        foreach (var dweller in allFoundDwellers)
        {
            if (!dweller.IsDead)
            {
                allDwellers.Add(dweller);
            }
            else
            {
                Debug.Log($"Excluding {dweller.dwellerName} from assignments (DEAD)");
            }
        }

        WorkStation[] allStations = FindObjectsOfType<WorkStation>();
        foreach (var station in allStations)
        {
            // Exclude rest stations from work list
            if (!(station is RestStation))
            {
                allWorkStations.Add(station);
            }
        }

        Debug.Log($"Found {allDwellers.Count} alive NPCs and {allWorkStations.Count} work machines (excluding beds)");
    }

    /// <summary>
    /// Assigns a specific NPC to a specific work station
    /// </summary>
    /// <param name="dweller">The NPC to assign</param>
    /// <param name="station">The work station to assign to</param>
    public void AssignDwellerToStation(DwellerNPC dweller, WorkStation station)
    {
        // Verify NPC is not dead
        if (dweller != null && dweller.IsDead)
        {
            Debug.LogWarning($"Cannot assign {dweller.dwellerName} - IS DEAD");
            return;
        }

        if (dweller == null || station == null) return;

        // Prevent assignment to beds through this manager
        if (station is RestStation)
        {
            Debug.LogWarning($"Do not assign {dweller.dwellerName} to bed via AssignmentManager");
            return;
        }

        // Don't reassign if already assigned and working effectively
        if (currentAssignments.ContainsKey(dweller) &&
            currentAssignments[dweller] == station &&
            dweller.CanWorkEffectively())
        {
            return;
        }

        // Remove previous assignment if exists
        if (currentAssignments.ContainsKey(dweller))
        {
            currentAssignments[dweller].RemoveWorker(dweller);
            currentAssignments.Remove(dweller);
        }

        // Perform new assignment
        dweller.AssignToWorkStation(station);
        currentAssignments[dweller] = station;

        Debug.Log($"{dweller.dwellerName} assigned to {station.stationName}");
    }

    /// <summary>
    /// Removes work assignment from an NPC
    /// </summary>
    /// <param name="dweller">The NPC to unassign</param>
    public void UnassignDweller(DwellerNPC dweller)
    {
        // Verify NPC is not dead
        if (dweller != null && dweller.IsDead)
        {
            Debug.LogWarning($"Cannot unassign {dweller.dwellerName} - IS DEAD");
            return;
        }

        if (currentAssignments.ContainsKey(dweller))
        {
            currentAssignments[dweller].RemoveWorker(dweller);
            currentAssignments.Remove(dweller);
            dweller.AssignToWorkStation(null);
            Debug.Log($"{dweller.dwellerName} unassigned from work");
        }
    }

    /// <summary>
    /// Automatically assigns all available NPCs to work stations
    /// </summary>
    public void AutoAssignAll()
    {
        // Update list of alive NPCs
        FindAllDwellersAndStations();

        var unassignedDwellers = new List<DwellerNPC>(allDwellers);
        // Don't remove NPCs that are already assigned and working well
        unassignedDwellers.RemoveAll(d => currentAssignments.ContainsKey(d) && d.CanWorkEffectively());

        var availableStations = new List<WorkStation>(allWorkStations);

        Debug.Log($"Starting auto-assignment: {unassignedDwellers.Count} free NPCs, {availableStations.Count} machines available");

        // Filter dead NPCs again for safety
        unassignedDwellers.RemoveAll(d => d.IsDead);

        // Assignment by needed resource type
        foreach (var dweller in unassignedDwellers)
        {
            if (availableStations.Count == 0) break;

            // Verify NPC didn't die during the process
            if (dweller.IsDead) continue;

            WorkStation bestStation = FindBestStationForDweller(dweller, availableStations);
            if (bestStation != null)
            {
                AssignDwellerToStation(dweller, bestStation);
                availableStations.Remove(bestStation);
            }
        }

        Debug.Log($"Auto-assignment completed. {Mathf.Min(unassignedDwellers.Count, allWorkStations.Count)} NPCs assigned to work");
    }

    /// <summary>
    /// Finds the best work station for an NPC based on their needs
    /// </summary>
    /// <param name="dweller">The NPC to find a station for</param>
    /// <param name="availableStations">List of available stations</param>
    /// <returns>The best matching work station</returns>
    private WorkStation FindBestStationForDweller(DwellerNPC dweller, List<WorkStation> availableStations)
    {
        // Verify NPC is alive
        if (dweller.IsDead) return null;

        // If NPC has critical need, prioritize stations that produce that resource
        if (dweller.NeedsRecovery())
        {
            ResourceType criticalNeed = dweller.GetMostCriticalNeed();
            WorkStation needStation = availableStations.Find(s => s.producedResource == criticalNeed);
            if (needStation != null) return needStation;
        }

        // Find stations with fewer workers to balance load
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
    /// Finds a station that produces a specific resource type
    /// </summary>
    /// <param name="resourceType">The resource type to find</param>
    /// <returns>The work station producing that resource</returns>
    public WorkStation FindStationByResource(ResourceType resourceType)
    {
        return allWorkStations.Find(station => station.producedResource == resourceType);
    }

    /// <summary>
    /// Gets the station assigned to a specific NPC
    /// </summary>
    /// <param name="dweller">The NPC to check</param>
    /// <returns>The assigned work station</returns>
    public WorkStation GetAssignedStation(DwellerNPC dweller)
    {
        // Verify NPC is not dead
        if (dweller != null && dweller.IsDead)
        {
            Debug.LogWarning($"Cannot get assignment from {dweller.dwellerName} - IS DEAD");
            return null;
        }

        return currentAssignments.ContainsKey(dweller) ? currentAssignments[dweller] : null;
    }

    /// <summary>
    /// Checks if an NPC is assigned to any station
    /// </summary>
    /// <param name="dweller">The NPC to check</param>
    /// <returns>True if assigned, false otherwise</returns>
    public bool IsDwellerAssigned(DwellerNPC dweller)
    {
        // Verify NPC is not dead
        if (dweller != null && dweller.IsDead)
        {
            return false; // Dead NPCs are not assigned
        }

        return currentAssignments.ContainsKey(dweller);
    }

    /// <summary>
    /// Gets all current assignments
    /// </summary>
    /// <returns>Dictionary of NPC to work station assignments</returns>
    public Dictionary<DwellerNPC, WorkStation> GetAllAssignments()
    {
        // Return only assignments of alive NPCs
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
    /// Reassigns all NPCs to work stations
    /// </summary>
    public void ReassignAllDwellers()
    {
        // Only unassign alive NPCs
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
    /// Reassigns only NPCs that have problems working effectively
    /// </summary>
    [ContextMenu("Reassign Problematic NPCs")]
    public void ReassignProblematicDwellers()
    {
        var problematicDwellers = new List<DwellerNPC>();

        foreach (var dweller in allDwellers)
        {
            // Exclude dead NPCs
            if (dweller.IsDead) continue;

            if (dweller.NeedsRecovery() && !dweller.IsRecovering())
            {
                problematicDwellers.Add(dweller);
            }
        }

        Debug.Log($"Reassigning {problematicDwellers.Count} problematic NPCs");

        foreach (var dweller in problematicDwellers)
        {
            UnassignDweller(dweller);
        }

        // Reassign after a brief delay
        Invoke("AutoAssignAll", 1f);
    }

    /// <summary>
    /// Gets the count of currently assigned NPCs
    /// </summary>
    /// <returns>Number of assigned NPCs</returns>
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
    /// Gets the count of alive NPCs
    /// </summary>
    /// <returns>Number of alive NPCs</returns>
    public int GetAliveDwellerCount()
    {
        return allDwellers.Count;
    }

    /// <summary>
    /// Resets all assignments (for game restart)
    /// </summary>
    public void ResetAllAssignments()
    {
        currentAssignments.Clear();
        FindAllDwellersAndStations();
        Debug.Log("All assignments reset");
    }
}