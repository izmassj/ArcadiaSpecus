using UnityEngine;

public class MachineManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] public MachineKind typeOfMachine;

    [Header("Simulation")]
    [SerializeField] private MachineUsageType _usageType = MachineUsageType.Production;

    [Header("Production")]
    [SerializeField] private BunkerResourceType _producedResourceType = BunkerResourceType.Scrap;
    [SerializeField] private int _resourceAmountPerCycle = 5;
    [SerializeField] private float _productionTimeWithOneWorker = 10f;
    [SerializeField] private float _secondWorkerTimeMultiplier = 0.65f;

    [Header("Station")]
    [SerializeField] private NPCNeedType _recoveredNeedType = NPCNeedType.Fatigue;
    [SerializeField] private float _stationCycleTime = 6f;
    [SerializeField] private float _stationRecoveryPerCycle = 20f;

    [Header("NPC")]
    [SerializeField] private RoomManager _ownerRoom;
    [SerializeField] private Transform _workPointA;
    [SerializeField] private Transform _workPointB;
    [SerializeField] private Vector3 _workPointOffset = Vector3.zero;
    [SerializeField] private float _fallbackSlotSeparation = 0.6f;

    [Header("Debug")]
    [SerializeField] private float _currentCycleTimer;
    [SerializeField] private bool _canCollect;
    [SerializeField] private int _pendingResourceAmount;

    private NPCBunkerWorker _workerInSlotA;
    private NPCBunkerWorker _workerInSlotB;
    private bool _workerInSlotAReady;
    private bool _workerInSlotBReady;

    private void Awake()
    {
        ResolveOwnerRoom();
        ApplyDefaultSetupFromMachineKind();
    }

    private void Update()
    {
        SimulateMachine(Time.deltaTime);
    }

    public void AssignOwnerRoom(RoomManager room)
    {
        _ownerRoom = room;
    }

    public RoomManager GetOwnerRoom()
    {
        ResolveOwnerRoom();
        return _ownerRoom;
    }

    public MachineUsageType GetUsageType()
    {
        return _usageType;
    }

    public BunkerResourceType GetProducedResourceType()
    {
        return _producedResourceType;
    }

    public NPCNeedType GetRecoveredNeedType()
    {
        return _recoveredNeedType;
    }

    public int GetAssignedWorkerCount()
    {
        int count = 0;

        if (_workerInSlotA != null)
            count++;

        if (_workerInSlotB != null)
            count++;

        return count;
    }

    public int GetActiveWorkerCount()
    {
        int count = 0;

        if (_workerInSlotA != null && _workerInSlotAReady)
            count++;

        if (_workerInSlotB != null && _workerInSlotBReady)
            count++;

        return count;
    }

    public bool HasFreeWorkSlotFor(NPCBunkerWorker worker)
    {
        if (worker == null)
            return false;

        if (_workerInSlotA == worker || _workerInSlotB == worker)
            return true;

        return _workerInSlotA == null || _workerInSlotB == null;
    }

    public bool CanCollectProducedResources()
    {
        return _usageType == MachineUsageType.Production && _canCollect && _pendingResourceAmount > 0;
    }

    public float CurrentTimeCollectProducingResources()
    {
        return _currentCycleTimer;
    }

    public float MaxTimeCollectProducingResources()
    {
        return _stationCycleTime;
    }

    public void CollectProducedResources(BunkerResourceManager resourceManager)
    {
        if (!CanCollectProducedResources() || resourceManager == null)
            return;

        resourceManager.Add(_producedResourceType, _pendingResourceAmount);
        _pendingResourceAmount = 0;
        _canCollect = false;
        _currentCycleTimer = 0f;
    }

    public bool TryReserveWorkSlot(NPCBunkerWorker worker, out int slotIndex, out Vector3 workPointWorldPosition)
    {
        slotIndex = -1;
        workPointWorldPosition = transform.position;

        if (worker == null)
            return false;

        if (_workerInSlotA == worker)
        {
            slotIndex = 0;
            workPointWorldPosition = GetWorkPointWorldPosition(0);
            return true;
        }

        if (_workerInSlotB == worker)
        {
            slotIndex = 1;
            workPointWorldPosition = GetWorkPointWorldPosition(1);
            return true;
        }

        if (_workerInSlotA == null)
        {
            _workerInSlotA = worker;
            slotIndex = 0;
            workPointWorldPosition = GetWorkPointWorldPosition(0);
            return true;
        }

        if (_workerInSlotB == null)
        {
            _workerInSlotB = worker;
            slotIndex = 1;
            workPointWorldPosition = GetWorkPointWorldPosition(1);
            return true;
        }

        return false;
    }

    public bool ConfirmWorkerArrived(NPCBunkerWorker worker, int slotIndex)
    {
        if (worker == null)
            return false;

        if (slotIndex == 0 && _workerInSlotA == worker)
        {
            _workerInSlotAReady = true;
            return true;
        }

        if (slotIndex == 1 && _workerInSlotB == worker)
        {
            _workerInSlotBReady = true;
            return true;
        }

        return false;
    }

    public void ReleaseWorkSlot(NPCBunkerWorker worker)
    {
        if (worker == null)
            return;

        if (_workerInSlotA == worker)
        {
            _workerInSlotA = null;
            _workerInSlotAReady = false;
        }

        if (_workerInSlotB == worker)
        {
            _workerInSlotB = null;
            _workerInSlotBReady = false;
        }
    }

    public Vector3 GetWorkPointWorldPosition(int slotIndex)
    {
        if (slotIndex == 0 && _workPointA != null)
            return _workPointA.position;

        if (slotIndex == 1 && _workPointB != null)
            return _workPointB.position;

        ResolveOwnerRoom();

        Vector3 basePosition = transform.position + _workPointOffset;

        if (_ownerRoom == null)
        {
            float offset = slotIndex == 0 ? _fallbackSlotSeparation * 0.5f : -_fallbackSlotSeparation * 0.5f;
            return basePosition + transform.right * offset;
        }

        Vector3 railCenter = _ownerRoom.GetRailCenterWorldPosition();
        Vector3 toRail = (railCenter - basePosition).normalized;

        if (toRail.sqrMagnitude <= 0.0001f)
            toRail = -transform.forward;

        Vector3 side = Vector3.Cross(Vector3.up, toRail).normalized;
        Vector3 centerPoint = (railCenter + basePosition) * 0.5f;
        float sideOffset = slotIndex == 0 ? _fallbackSlotSeparation * 0.5f : -_fallbackSlotSeparation * 0.5f;
        return centerPoint + side * sideOffset;
    }

    private void SimulateMachine(float deltaTime)
    {
        int workerCount = GetActiveWorkerCount();
        if (workerCount <= 0)
        {
            _currentCycleTimer = 0f;
            return;
        }

        if (_usageType == MachineUsageType.Production)
        {
            SimulateProduction(deltaTime, workerCount);
            return;
        }

        SimulateStation(deltaTime);
    }

    private void SimulateProduction(float deltaTime, int workerCount)
    {
        if (_canCollect)
            return;

        float cycleDuration = GetCurrentProductionCycleDuration(workerCount);
        if (cycleDuration <= 0f)
            cycleDuration = 0.1f;

        _currentCycleTimer += deltaTime;

        if (_currentCycleTimer < cycleDuration)
            return;

        _currentCycleTimer = 0f;
        _pendingResourceAmount += _resourceAmountPerCycle;
        _canCollect = true;
    }

    private void SimulateStation(float deltaTime)
    {
        if (_recoveredNeedType == NPCNeedType.None)
            return;

        _currentCycleTimer += deltaTime;

        if (_currentCycleTimer < _stationCycleTime)
            return;

        _currentCycleTimer = 0f;
        if (_workerInSlotAReady)
            ApplyStationRecovery(_workerInSlotA);

        if (_workerInSlotBReady)
            ApplyStationRecovery(_workerInSlotB);
    }

    private void ApplyStationRecovery(NPCBunkerWorker worker)
    {
        if (worker == null)
            return;

        worker.RecoverNeed(_recoveredNeedType, _stationRecoveryPerCycle);
    }

    private float GetCurrentProductionCycleDuration(int workerCount)
    {
        if (workerCount <= 1)
            return _productionTimeWithOneWorker;

        return _productionTimeWithOneWorker * _secondWorkerTimeMultiplier;
    }

    private void ResolveOwnerRoom()
    {
        if (_ownerRoom != null)
            return;

        _ownerRoom = GetComponentInParent<RoomManager>();
    }

    private void ApplyDefaultSetupFromMachineKind()
    {
        switch (typeOfMachine)
        {
            case MachineKind.ElectricityMachine:
                _usageType = MachineUsageType.Production;
                _producedResourceType = BunkerResourceType.Electricity;
                break;
            case MachineKind.FoodMachine:
                _usageType = MachineUsageType.Production;
                _producedResourceType = BunkerResourceType.Food;
                break;
            case MachineKind.ScrapMachine:
                _usageType = MachineUsageType.Production;
                _producedResourceType = BunkerResourceType.Scrap;
                break;
            case MachineKind.WaterMachine:
                _usageType = MachineUsageType.Production;
                _producedResourceType = BunkerResourceType.Water;
                break;
            case MachineKind.Bed:
                _usageType = MachineUsageType.Station;
                _recoveredNeedType = NPCNeedType.Fatigue;
                break;
            case MachineKind.VendingMachine:
                _usageType = MachineUsageType.Station;
                _recoveredNeedType = NPCNeedType.Hunger;
                break;
            case MachineKind.WaterCooler:
                _usageType = MachineUsageType.Station;
                _recoveredNeedType = NPCNeedType.Thirst;
                break;
        }
    }
}
