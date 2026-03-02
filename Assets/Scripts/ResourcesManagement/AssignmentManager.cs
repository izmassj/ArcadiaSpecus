using System.Collections.Generic;
using UnityEngine;

public class AssignmentManager : MonoBehaviour
{
    public static AssignmentManager Instance;

    [Header("Auto asignación")]
    [SerializeField] private bool _autoAssignOnStart = true;
    [SerializeField] private bool _reassignWhenNpcRevives = true;

    [Header("Reparto dinámico de producción")]
    [SerializeField] private bool _enableDynamicProductionRebalance = true;
    [SerializeField] private float _rebalanceIntervalSeconds = 2f;
    [SerializeField] private bool _rebalanceAtStartAfterAutoAssign = true;
    [SerializeField] private bool _verboseRebalanceLogs = false;

    private readonly List<DwellerNPC> _cachedDwellers = new List<DwellerNPC>();
    private readonly List<WorkStation> _cachedStations = new List<WorkStation>();

    private float _rebalanceTimer;

    private class StationBalanceInfo
    {
        public WorkStation station;
        public List<DwellerNPC> assignedAliveWorkers = new List<DwellerNPC>();
        public List<DwellerNPC> assignedAvailableWorkers = new List<DwellerNPC>();
        public int targetAvailableWorkers;
        public int CurrentAvailableCount => assignedAvailableWorkers.Count;
        public int CurrentAliveAssignedCount => assignedAliveWorkers.Count;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        ResourceManager.OnNPCDied += HandleNPCDied;
    }

    private void Start()
    {
        RefreshCaches();

        if (_autoAssignOnStart)
        {
            AutoAssignAll();
        }

        if (_enableDynamicProductionRebalance && _rebalanceAtStartAfterAutoAssign)
        {
            RebalanceProductionAssignments();
        }
    }

    private void Update()
    {
        if (!_enableDynamicProductionRebalance)
        {
            return;
        }

        _rebalanceTimer += Time.deltaTime;
        if (_rebalanceTimer < Mathf.Max(0.25f, _rebalanceIntervalSeconds))
        {
            return;
        }

        _rebalanceTimer = 0f;
        RebalanceProductionAssignments();
    }

    private void OnDisable()
    {
        ResourceManager.OnNPCDied -= HandleNPCDied;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleNPCDied(DwellerNPC _npc)
    {
        if (_npc == null)
        {
            return;
        }

        // Rebalance ligero para cubrir la producción con los NPCs vivos disponibles.
        if (_enableDynamicProductionRebalance)
        {
            RebalanceProductionAssignments();
        }

        // Comentario audio: aquí podría dispararse una alerta de supervisor.
    }

    public void RefreshCaches()
    {
        _cachedDwellers.Clear();
        _cachedStations.Clear();

        DwellerNPC[] _dwellers = FindObjectsOfType<DwellerNPC>(true);
        WorkStation[] _stations = FindObjectsOfType<WorkStation>(true);

        for (int i = 0; i < _dwellers.Length; i++)
        {
            if (_dwellers[i] != null)
            {
                _cachedDwellers.Add(_dwellers[i]);
            }
        }

        for (int i = 0; i < _stations.Length; i++)
        {
            if (_stations[i] != null)
            {
                _cachedStations.Add(_stations[i]);
            }
        }
    }

    public void AutoAssignAll()
    {
        RefreshCaches();

        // Limpieza de referencias inconsistentes antes de reasignar.
        for (int i = 0; i < _cachedDwellers.Count; i++)
        {
            DwellerNPC _npc = _cachedDwellers[i];
            if (_npc == null || _npc.IsDead)
            {
                continue;
            }

            if (_npc.assignedWorkStation != null)
            {
                // Si ya está asignado, respetar para no romper comportamiento existente.
                continue;
            }

            WorkStation _best = FindBestStationForAutoAssign();
            if (_best != null)
            {
                _npc.AssignToWorkStation(_best);
            }
        }

        if (_enableDynamicProductionRebalance)
        {
            RebalanceProductionAssignments();
        }
    }

    private WorkStation FindBestStationForAutoAssign()
    {
        WorkStation _bestStation = null;
        int _bestWorkersCount = int.MaxValue;

        for (int i = 0; i < _cachedStations.Count; i++)
        {
            WorkStation _station = _cachedStations[i];
            if (_station == null || !_station.gameObject.activeInHierarchy)
            {
                continue;
            }

            // Evitar asignar NPCs a estaciones de consumo (comida/agua/cama) como trabajo fijo.
            if (_station.isConsumptionStation)
            {
                continue;
            }

            List<DwellerNPC> _workers = _station.GetAssignedWorkers();
            int _workersCount = 0;

            for (int j = 0; j < _workers.Count; j++)
            {
                if (_workers[j] != null && !_workers[j].IsDead)
                {
                    _workersCount++;
                }
            }

            if (_workersCount < _bestWorkersCount)
            {
                _bestWorkersCount = _workersCount;
                _bestStation = _station;
            }
        }

        return _bestStation;
    }

    public void ResetAllAssignments()
    {
        RefreshCaches();

        for (int i = 0; i < _cachedDwellers.Count; i++)
        {
            DwellerNPC _npc = _cachedDwellers[i];
            if (_npc == null)
            {
                continue;
            }

            _npc.AssignToWorkStation(null);
        }
    }

    public void AssignNPCToStation(DwellerNPC _npc, WorkStation _station)
    {
        if (_npc == null)
        {
            return;
        }

        if (_npc.IsDead)
        {
            return;
        }

        _npc.AssignToWorkStation(_station);

        if (_enableDynamicProductionRebalance)
        {
            RebalanceProductionAssignments();
        }
    }

    public void AssignByName(string _npcName, string _stationId)
    {
        if (string.IsNullOrWhiteSpace(_npcName))
        {
            return;
        }

        RefreshCaches();

        DwellerNPC _targetNpc = null;
        for (int i = 0; i < _cachedDwellers.Count; i++)
        {
            if (_cachedDwellers[i] != null && _cachedDwellers[i].dwellerName == _npcName)
            {
                _targetNpc = _cachedDwellers[i];
                break;
            }
        }

        WorkStation _targetStation = null;
        for (int i = 0; i < _cachedStations.Count; i++)
        {
            if (_cachedStations[i] != null && _cachedStations[i].stationId == _stationId)
            {
                _targetStation = _cachedStations[i];
                break;
            }
        }

        if (_targetNpc != null)
        {
            _targetNpc.AssignToWorkStation(_targetStation);
        }

        if (_enableDynamicProductionRebalance)
        {
            RebalanceProductionAssignments();
        }
    }

    public void ReassignUnassignedNPCs()
    {
        RefreshCaches();

        for (int i = 0; i < _cachedDwellers.Count; i++)
        {
            DwellerNPC _npc = _cachedDwellers[i];
            if (_npc == null || _npc.IsDead)
            {
                continue;
            }

            if (_npc.assignedWorkStation == null)
            {
                WorkStation _best = FindBestStationForAutoAssign();
                if (_best != null)
                {
                    _npc.AssignToWorkStation(_best);
                }
            }
        }

        if (_enableDynamicProductionRebalance)
        {
            RebalanceProductionAssignments();
        }
    }

    [ContextMenu("AutoAssign All")]
    public void ContextAutoAssignAll()
    {
        AutoAssignAll();
    }

    [ContextMenu("Reset All Assignments")]
    public void ContextResetAllAssignments()
    {
        ResetAllAssignments();
    }

    [ContextMenu("Reassign Unassigned NPCs")]
    public void ContextReassignUnassignedNPCs()
    {
        ReassignUnassignedNPCs();
    }

    [ContextMenu("Rebalance Production Assignments")]
    public void ContextRebalanceProductionAssignments()
    {
        RebalanceProductionAssignments();
    }

    public void NotifyNPCRevived(DwellerNPC _npc)
    {
        if (!_reassignWhenNpcRevives || _npc == null || _npc.IsDead)
        {
            return;
        }

        if (_npc.assignedWorkStation != null)
        {
            if (_enableDynamicProductionRebalance)
            {
                RebalanceProductionAssignments();
            }
            return;
        }

        RefreshCaches();
        WorkStation _best = FindBestStationForAutoAssign();
        if (_best != null)
        {
            _npc.AssignToWorkStation(_best);
        }

        if (_enableDynamicProductionRebalance)
        {
            RebalanceProductionAssignments();
        }
    }

    public void RebalanceProductionAssignments()
    {
        RefreshCaches();

        // Hacemos varias pasadas pequeñas para cubrir déficits de estaciones mientras haya cambios.
        const int _maxPasses = 12;
        bool _changedAny = false;

        for (int _pass = 0; _pass < _maxPasses; _pass++)
        {
            bool _changedThisPass = TryRebalanceSinglePass();
            if (!_changedThisPass)
            {
                break;
            }

            _changedAny = true;
        }

        if (_verboseRebalanceLogs && _changedAny)
        {
            Debug.Log("[AssignmentManager] Rebalance de producción aplicado.");
        }
    }

    private bool TryRebalanceSinglePass()
    {
        List<StationBalanceInfo> _productionInfos;
        List<DwellerNPC> _allAvailableWorkers;
        List<DwellerNPC> _unassignedAvailableWorkers;

        BuildProductionBalanceSnapshot(out _productionInfos, out _allAvailableWorkers, out _unassignedAvailableWorkers);

        if (_productionInfos.Count == 0)
        {
            return false;
        }

        // Si no hay NPCs disponibles para trabajar ahora mismo, no se puede repartir nada.
        if (_allAvailableWorkers.Count == 0)
        {
            return false;
        }

        AssignTargetsForCurrentAvailability(_productionInfos, _allAvailableWorkers.Count);

        StationBalanceInfo _deficitStation = FindDeficitStation(_productionInfos);
        if (_deficitStation == null)
        {
            return false;
        }


        for (int i = 0; i < _unassignedAvailableWorkers.Count; i++)
        {
            DwellerNPC _npc = _unassignedAvailableWorkers[i];
            if (_npc == null || !_npc.CanBeReassignedToProductionNow())
            {
                continue;
            }

            if (_npc.assignedWorkStation == _deficitStation.station)
            {
                continue;
            }

            if (_verboseRebalanceLogs)
            {
                Debug.Log($"[AssignmentManager] Rebalance -> {_npc.dwellerName} asignado a {_deficitStation.station.stationName}");
            }

            _npc.AssignToWorkStation(_deficitStation.station);
            return true;
        }

        // 2) Si no hay libres, mover desde una estación con superávit de trabajadores DISPONIBLES.
        StationBalanceInfo _donorStation = FindBestDonorStation(_productionInfos, _deficitStation);
        if (_donorStation == null)
        {
            return false;
        }

        DwellerNPC _donorNpc = FindBestMovableWorkerFromDonor(_donorStation);
        if (_donorNpc == null)
        {
            return false;
        }

        if (_verboseRebalanceLogs)
        {
            string _from = _donorStation.station != null ? _donorStation.station.stationName : "N/A";
            string _to = _deficitStation.station != null ? _deficitStation.station.stationName : "N/A";
            Debug.Log($"[AssignmentManager] Rebalance -> {_donorNpc.dwellerName} movido de {_from} a {_to}");
        }

        _donorNpc.AssignToWorkStation(_deficitStation.station);
        return true;
    }

    private void BuildProductionBalanceSnapshot(
        out List<StationBalanceInfo> _productionInfos,
        out List<DwellerNPC> _allAvailableWorkers,
        out List<DwellerNPC> _unassignedAvailableWorkers)
    {
        _productionInfos = new List<StationBalanceInfo>();
        _allAvailableWorkers = new List<DwellerNPC>();
        _unassignedAvailableWorkers = new List<DwellerNPC>();

        // 1) Estaciones de producción válidas
        for (int i = 0; i < _cachedStations.Count; i++)
        {
            WorkStation _station = _cachedStations[i];
            if (_station == null || !_station.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (_station.isConsumptionStation)
            {
                continue;
            }

            StationBalanceInfo _info = new StationBalanceInfo();
            _info.station = _station;

            List<DwellerNPC> _workers = _station.GetAssignedWorkers();
            for (int j = 0; j < _workers.Count; j++)
            {
                DwellerNPC _npc = _workers[j];
                if (_npc == null || _npc.IsDead)
                {
                    continue;
                }

                if (!_info.assignedAliveWorkers.Contains(_npc))
                {
                    _info.assignedAliveWorkers.Add(_npc);
                }

                if (_npc.CanBeReassignedToProductionNow() && !_info.assignedAvailableWorkers.Contains(_npc))
                {
                    _info.assignedAvailableWorkers.Add(_npc);
                }
            }

            _productionInfos.Add(_info);
        }

        // 2) NPCs disponibles ahora mismo para producción
        for (int i = 0; i < _cachedDwellers.Count; i++)
        {
            DwellerNPC _npc = _cachedDwellers[i];
            if (_npc == null || _npc.IsDead)
            {
                continue;
            }

            if (!_npc.CanBeReassignedToProductionNow())
            {
                continue;
            }

            // Si por error estuviera asignado a estación de consumo, lo tratamos como no asignado a producción.
            if (_npc.assignedWorkStation != null && _npc.assignedWorkStation.isConsumptionStation)
            {
                _unassignedAvailableWorkers.Add(_npc);
                _allAvailableWorkers.Add(_npc);
                continue;
            }

            _allAvailableWorkers.Add(_npc);

            if (_npc.assignedWorkStation == null)
            {
                _unassignedAvailableWorkers.Add(_npc);
            }
        }
    }

    private void AssignTargetsForCurrentAvailability(List<StationBalanceInfo> _infos, int _availableWorkersCount)
    {
        if (_infos == null || _infos.Count == 0)
        {
            return;
        }

        // Reiniciar objetivos
        for (int i = 0; i < _infos.Count; i++)
        {
            _infos[i].targetAvailableWorkers = 0;
        }

        if (_availableWorkersCount <= 0)
        {
            return;
        }

        int _stationCount = _infos.Count;
        int _basePerStation = _availableWorkersCount / _stationCount;
        int _remainder = _availableWorkersCount % _stationCount;

        // Ordenar por estaciones con menor disponibilidad actual para priorizar cubrir huecos.
        List<StationBalanceInfo> _ordered = new List<StationBalanceInfo>(_infos);
        _ordered.Sort(CompareStationsForTargetDistribution);

        for (int i = 0; i < _ordered.Count; i++)
        {
            _ordered[i].targetAvailableWorkers = _basePerStation + (i < _remainder ? 1 : 0);
        }
    }

    private int CompareStationsForTargetDistribution(StationBalanceInfo _a, StationBalanceInfo _b)
    {
        if (_a == null && _b == null) return 0;
        if (_a == null) return 1;
        if (_b == null) return -1;

        int _byAvailable = _a.CurrentAvailableCount.CompareTo(_b.CurrentAvailableCount);
        if (_byAvailable != 0)
        {
            return _byAvailable;
        }

        int _byAliveAssigned = _a.CurrentAliveAssignedCount.CompareTo(_b.CurrentAliveAssignedCount);
        if (_byAliveAssigned != 0)
        {
            return _byAliveAssigned;
        }

        string _aId = _a.station != null ? _a.station.stationId : string.Empty;
        string _bId = _b.station != null ? _b.station.stationId : string.Empty;
        return string.CompareOrdinal(_aId, _bId);
    }

    private StationBalanceInfo FindDeficitStation(List<StationBalanceInfo> _infos)
    {
        StationBalanceInfo _best = null;
        int _bestDeficit = 0;

        for (int i = 0; i < _infos.Count; i++)
        {
            StationBalanceInfo _info = _infos[i];
            if (_info == null || _info.station == null)
            {
                continue;
            }

            int _deficit = _info.targetAvailableWorkers - _info.CurrentAvailableCount;
            if (_deficit <= 0)
            {
                continue;
            }

            if (_best == null || _deficit > _bestDeficit)
            {
                _best = _info;
                _bestDeficit = _deficit;
            }
        }

        return _best;
    }

    private StationBalanceInfo FindBestDonorStation(List<StationBalanceInfo> _infos, StationBalanceInfo _receiver)
    {
        StationBalanceInfo _best = null;
        int _bestSurplus = 0;

        for (int i = 0; i < _infos.Count; i++)
        {
            StationBalanceInfo _info = _infos[i];
            if (_info == null || _info.station == null)
            {
                continue;
            }

            if (_receiver != null && _info.station == _receiver.station)
            {
                continue;
            }

            int _surplus = _info.CurrentAvailableCount - _info.targetAvailableWorkers;
            if (_surplus <= 0)
            {
                continue;
            }

            if (FindBestMovableWorkerFromDonor(_info) == null)
            {
                continue;
            }

            if (_best == null || _surplus > _bestSurplus)
            {
                _best = _info;
                _bestSurplus = _surplus;
            }
        }

        return _best;
    }

    private DwellerNPC FindBestMovableWorkerFromDonor(StationBalanceInfo _donor)
    {
        if (_donor == null)
        {
            return null;
        }

        // Prioridad 1: NPCs en Idle (disponibles inmediatos sin cortar producción "ya activa")
        for (int i = 0; i < _donor.assignedAvailableWorkers.Count; i++)
        {
            DwellerNPC _npc = _donor.assignedAvailableWorkers[i];
            if (_npc == null || !_npc.CanBeReassignedToProductionNow())
            {
                continue;
            }

            if (_npc.currentState == DwellerState.Idle)
            {
                return _npc;
            }
        }

        // Prioridad 2: cualquier NPC disponible (puede estar trabajando o yendo al trabajo).
        for (int i = 0; i < _donor.assignedAvailableWorkers.Count; i++)
        {
            DwellerNPC _npc = _donor.assignedAvailableWorkers[i];
            if (_npc == null || !_npc.CanBeReassignedToProductionNow())
            {
                continue;
            }

            return _npc;
        }

        return null;
    }
}
