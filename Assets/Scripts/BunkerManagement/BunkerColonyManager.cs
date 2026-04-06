using System.Collections.Generic;
using UnityEngine;

public class BunkerColonyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BunkerResourceManager _resourceManager;

    [Header("Refresh")]
    [SerializeField] private float _refreshInterval = 2f;
    [SerializeField] private bool _autoFindOnStart = true;

    [Header("Stable Mode Thresholds")]
    [SerializeField] private float _stableNeedThreshold = 65f;
    [SerializeField] private float _criticalNeedThreshold = 85f;

    [Header("Production Mode Thresholds")]
    [SerializeField] private float _productionNeedThreshold = 90f;

    [Header("Recovery Mode Thresholds")]
    [SerializeField] private float _recoveryNeedThreshold = 45f;

    [Header("Debug")]
    [SerializeField] private BunkerManagementMode _currentMode = BunkerManagementMode.Stable;
    [SerializeField] private List<NPCBunkerWorker> _workers = new List<NPCBunkerWorker>();
    [SerializeField] private List<MachineManager> _machines = new List<MachineManager>();

    private float _refreshTimer;

    private void Start()
    {
        if (_autoFindOnStart)
            RefreshWorldLists();
    }

    private void Update()
    {
        _refreshTimer += Time.deltaTime;

        if (_refreshTimer < _refreshInterval)
            return;

        _refreshTimer = 0f;
        RefreshWorldLists();
        EvaluateAssignments();
    }

    public void SetStableMode()
    {
        _currentMode = BunkerManagementMode.Stable;
        EvaluateAssignments();
    }

    public void SetProductionFocusMode()
    {
        _currentMode = BunkerManagementMode.ProductionFocus;
        EvaluateAssignments();
    }

    public void SetRecoveryFocusMode()
    {
        _currentMode = BunkerManagementMode.RecoveryFocus;
        EvaluateAssignments();
    }

    [ContextMenu("Refresh World Lists")]
    public void RefreshWorldLists()
    {
        _workers.Clear();
        _machines.Clear();

        NPCBunkerWorker[] workers = FindObjectsByType<NPCBunkerWorker>(FindObjectsSortMode.None);
        MachineManager[] machines = FindObjectsByType<MachineManager>(FindObjectsSortMode.None);

        for (int i = 0; i < workers.Length; i++)
        {
            if (workers[i] != null)
                _workers.Add(workers[i]);
        }

        for (int i = 0; i < machines.Length; i++)
        {
            if (machines[i] != null)
                _machines.Add(machines[i]);
        }
    }

    [ContextMenu("Evaluate Assignments")]
    public void EvaluateAssignments()
    {
        for (int i = 0; i < _workers.Count; i++)
        {
            NPCBunkerWorker worker = _workers[i];
            if (worker == null)
                continue;

            MachineManager desiredMachine = GetBestMachineForWorker(worker);
            if (desiredMachine == null)
                continue;

            if (worker.GetAssignedOrTargetMachine() == desiredMachine)
                continue;

            worker.AssignMachine(desiredMachine);
        }
    }

    private MachineManager GetBestMachineForWorker(NPCBunkerWorker worker)
    {
        if (worker == null)
            return null;

        bool shouldRecover = ShouldSendWorkerToStation(worker, out NPCNeedType desiredNeed);

        if (shouldRecover)
        {
            MachineManager stationMachine = FindBestStation(worker, desiredNeed);
            if (stationMachine != null)
                return stationMachine;
        }

        return FindBestProductionMachine(worker);
    }

    private bool ShouldSendWorkerToStation(NPCBunkerWorker worker, out NPCNeedType desiredNeed)
    {
        desiredNeed = worker.GetMostUrgentNeedType();
        float needValue = worker.GetNeedValue(desiredNeed);

        switch (_currentMode)
        {
            case BunkerManagementMode.ProductionFocus:
                return needValue >= _productionNeedThreshold;

            case BunkerManagementMode.RecoveryFocus:
                return needValue >= _recoveryNeedThreshold;

            default:
                return needValue >= _stableNeedThreshold;
        }
    }

    private MachineManager FindBestStation(NPCBunkerWorker worker, NPCNeedType desiredNeed)
    {
        MachineManager bestMachine = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < _machines.Count; i++)
        {
            MachineManager machine = _machines[i];
            if (machine == null)
                continue;

            if (machine.GetUsageType() != MachineUsageType.Station)
                continue;

            if (machine.GetRecoveredNeedType() != desiredNeed)
                continue;

            if (!machine.HasFreeWorkSlotFor(worker))
                continue;

            float distanceScore = -Vector3.Distance(worker.transform.position, machine.transform.position);
            float freeSlotBonus = (2 - machine.GetAssignedWorkerCount()) * 10f;
            float score = distanceScore + freeSlotBonus;

            if (score > bestScore)
            {
                bestScore = score;
                bestMachine = machine;
            }
        }

        return bestMachine;
    }

    private MachineManager FindBestProductionMachine(NPCBunkerWorker worker)
    {
        MachineManager bestMachine = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < _machines.Count; i++)
        {
            MachineManager machine = _machines[i];
            if (machine == null)
                continue;

            if (machine.GetUsageType() != MachineUsageType.Production)
                continue;

            if (machine.CanCollectProducedResources())
                continue;

            if (!machine.HasFreeWorkSlotFor(worker))
                continue;

            int currentAmount = _resourceManager != null ? _resourceManager.GetAmount(machine.GetProducedResourceType()) : 0;
            float scarcityScore = -currentAmount;
            float distanceScore = -Vector3.Distance(worker.transform.position, machine.transform.position) * 0.25f;
            float freeSlotBonus = (2 - machine.GetAssignedWorkerCount()) * 15f;
            float criticalBias = IsCriticalNeed(worker) ? -1000f : 0f;
            float score = scarcityScore + distanceScore + freeSlotBonus + criticalBias;

            if (_currentMode == BunkerManagementMode.ProductionFocus)
                score += 25f;

            if (_currentMode == BunkerManagementMode.RecoveryFocus)
                score -= 15f;

            if (score > bestScore)
            {
                bestScore = score;
                bestMachine = machine;
            }
        }

        return bestMachine;
    }

    private bool IsCriticalNeed(NPCBunkerWorker worker)
    {
        if (worker == null)
            return false;

        return worker.GetMostUrgentNeedValue() >= _criticalNeedThreshold;
    }
}
