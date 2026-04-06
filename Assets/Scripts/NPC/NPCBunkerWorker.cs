using System.Collections;
using UnityEngine;

public class NPCBunkerWorker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NPCRailWalker _railWalker;
    [SerializeField] private Animator _animator;

    [Header("State")]
    [SerializeField] private RoomManager _currentRoom;
    [SerializeField] private MachineManager _currentMachine;
    [SerializeField] private MachineManager _targetMachine;
    [SerializeField] private int _currentWorkSlot = -1;

    [Header("Needs")]
    [SerializeField, Range(0f, 100f)] private float _hunger;
    [SerializeField, Range(0f, 100f)] private float _thirst;
    [SerializeField, Range(0f, 100f)] private float _fatigue;
    [SerializeField] private float _hungerPerSecond = 0.75f;
    [SerializeField] private float _thirstPerSecond = 0.9f;
    [SerializeField] private float _fatiguePerSecond = 0.65f;

    [Header("Off Rail Movement")]
    [SerializeField] private float _offRailMoveSpeed = 2f;
    [SerializeField] private float _offRailArrivalDistance = 0.05f;

    [Header("Animation")]
    [SerializeField] private string _walkStateName = "Walk";
    [SerializeField] private string _idleStateName = "Idle";
    [SerializeField] private string _workStateName = "Work";

    [Header("Debug")]
    [SerializeField] private bool _moveToDebugMachineOnStart;
    [SerializeField] private bool _isBusy;
    [SerializeField] private bool _isWorking;
    [SerializeField] private MachineManager _pendingMachine;
    [SerializeField] private int _pendingWorkSlot = -1;

    private Coroutine _currentRoutine;
    private string _currentAnimationState;

    private void Awake()
    {
        if (_railWalker == null)
            _railWalker = GetComponent<NPCRailWalker>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (_currentRoom == null && _railWalker != null)
            _currentRoom = _railWalker.GetStartRoom();

        if (_moveToDebugMachineOnStart && _targetMachine != null)
            AssignMachine(_targetMachine);
    }

    private void Update()
    {
        TickNeeds(Time.deltaTime);
    }

    public void AssignMachine(MachineManager machine)
    {
        if (machine == null)
            return;

        if (_currentMachine == machine && _isWorking)
            return;

        if (_targetMachine == machine && (_isBusy || _isWorking))
            return;

        ReleasePendingReservation();

        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }

        _targetMachine = machine;
        _currentRoutine = StartCoroutine(MoveToMachineRoutine(machine));
    }

    public MachineManager GetAssignedOrTargetMachine()
    {
        if (_targetMachine != null)
            return _targetMachine;

        return _currentMachine;
    }

    public bool IsBusy()
    {
        return _isBusy;
    }

    public bool IsWorking()
    {
        return _isWorking;
    }

    public float GetHunger()
    {
        return _hunger;
    }

    public float GetThirst()
    {
        return _thirst;
    }

    public float GetFatigue()
    {
        return _fatigue;
    }

    public float GetNeedValue(NPCNeedType needType)
    {
        switch (needType)
        {
            case NPCNeedType.Hunger:
                return _hunger;
            case NPCNeedType.Thirst:
                return _thirst;
            case NPCNeedType.Fatigue:
                return _fatigue;
            default:
                return 0f;
        }
    }

    public NPCNeedType GetMostUrgentNeedType()
    {
        float highestValue = _hunger;
        NPCNeedType highestNeed = NPCNeedType.Hunger;

        if (_thirst > highestValue)
        {
            highestValue = _thirst;
            highestNeed = NPCNeedType.Thirst;
        }

        if (_fatigue > highestValue)
            highestNeed = NPCNeedType.Fatigue;

        return highestNeed;
    }

    public float GetMostUrgentNeedValue()
    {
        return Mathf.Max(_hunger, _thirst, _fatigue);
    }

    public void RecoverNeed(NPCNeedType needType, float amount)
    {
        if (amount <= 0f)
            return;

        switch (needType)
        {
            case NPCNeedType.Hunger:
                _hunger = Mathf.Max(0f, _hunger - amount);
                break;
            case NPCNeedType.Thirst:
                _thirst = Mathf.Max(0f, _thirst - amount);
                break;
            case NPCNeedType.Fatigue:
                _fatigue = Mathf.Max(0f, _fatigue - amount);
                break;
        }
    }

    private void TickNeeds(float deltaTime)
    {
        _hunger = Mathf.Clamp(_hunger + _hungerPerSecond * deltaTime, 0f, 100f);
        _thirst = Mathf.Clamp(_thirst + _thirstPerSecond * deltaTime, 0f, 100f);
        _fatigue = Mathf.Clamp(_fatigue + _fatiguePerSecond * deltaTime, 0f, 100f);
    }

    private IEnumerator MoveToMachineRoutine(MachineManager targetMachine)
    {
        _isBusy = true;
        _isWorking = false;

        RoomManager targetRoom = targetMachine.GetOwnerRoom();

        if (_railWalker == null || _currentRoom == null || targetRoom == null)
        {
            ClearBusyState();
            yield break;
        }

        if (!targetMachine.TryReserveWorkSlot(this, out int reservedWorkSlot, out Vector3 reservedWorkPoint))
        {
            ClearBusyState();
            yield break;
        }

        _pendingMachine = targetMachine;
        _pendingWorkSlot = reservedWorkSlot;

        if (_currentMachine != null)
        {
            yield return MoveOffRailTo(_currentRoom.GetRailCenterWorldPosition(), true);
            ReleaseCurrentMachineSlot();
            _railWalker.SnapToRoom(_currentRoom);
        }
        else
        {
            _railWalker.SnapToRoom(_currentRoom);
        }

        IntersectionSplineExpander currentIntersection = _currentRoom.GetRailOwner();
        IntersectionSplineExpander targetIntersection = targetRoom.GetRailOwner();

        if (currentIntersection == null || targetIntersection == null)
        {
            ReleasePendingReservation();
            ClearBusyState();
            yield break;
        }

        if (currentIntersection == targetIntersection)
        {
            yield return TravelInsideSingleIntersection(_currentRoom, targetRoom, currentIntersection);
        }
        else
        {
            yield return TravelAcrossFloors(_currentRoom, targetRoom, currentIntersection, targetIntersection);
        }

        reservedWorkPoint = targetMachine.GetWorkPointWorldPosition(reservedWorkSlot);
        yield return MoveOffRailTo(reservedWorkPoint, true);

        if (!targetMachine.ConfirmWorkerArrived(this, reservedWorkSlot))
        {
            ReleasePendingReservation();
            ClearBusyState();
            yield break;
        }

        FaceTowards(targetMachine.transform.position);
        PlayAnimation(_workStateName);

        _currentRoom = targetRoom;
        _currentMachine = targetMachine;
        _targetMachine = targetMachine;
        _currentWorkSlot = reservedWorkSlot;
        _pendingMachine = null;
        _pendingWorkSlot = -1;
        _isWorking = true;
        _isBusy = false;
        _currentRoutine = null;
    }

    private IEnumerator TravelAcrossFloors(RoomManager fromRoom, RoomManager targetRoom, IntersectionSplineExpander currentIntersection, IntersectionSplineExpander targetIntersection)
    {
        IntersectionElevator sourceElevator = currentIntersection.gameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<IntersectionElevator>();
        IntersectionElevator targetElevator = targetIntersection.gameObject.transform.GetChild(0).transform.GetChild(0).GetComponent<IntersectionElevator>();
        IntersectionRailPort targetPort = targetIntersection.GetRoomPort(targetRoom);

        yield return GoToElevatorFromCurrentRoom(fromRoom, currentIntersection, sourceElevator);

        if (sourceElevator != null)
        {
            yield return sourceElevator.WaitForOpenFinished();
            yield return sourceElevator.PlayCloseAndWait();
        }

        _railWalker.SnapToIntersectionPort(targetIntersection, IntersectionRailPort.Elevator);
        FaceTowardsUpcomingElevatorExit(targetIntersection, targetPort);

        if (targetElevator != null)
            yield return targetElevator.PlayOpenAndWait();

        FaceTowardsUpcomingElevatorExit(targetIntersection, targetPort);
        yield return LeaveElevatorToTargetRoom(targetRoom, targetIntersection, targetElevator);
    }

    private IEnumerator TravelInsideSingleIntersection(RoomManager fromRoom, RoomManager toRoom, IntersectionSplineExpander intersection)
    {
        IntersectionRailPort fromPort = intersection.GetRoomPort(fromRoom);
        IntersectionRailPort toPort = intersection.GetRoomPort(toRoom);

        if (fromPort != toPort)
        {
            if (_railWalker.MoveToBranchStart(fromRoom))
            {
                _railWalker.QueueMovePortToPort(intersection, fromPort, toPort);
                _railWalker.QueueMoveToRoom(toRoom);
                yield return WaitForRailWalker();
                yield break;
            }
        }

        _railWalker.MoveToRoom(toRoom);
        yield return WaitForRailWalker();
    }

    private IEnumerator GoToElevatorFromCurrentRoom(RoomManager room, IntersectionSplineExpander intersection, IntersectionElevator elevator)
    {
        IntersectionRailPort roomPort = intersection.GetRoomPort(room);

        if (_railWalker.MoveToBranchStart(room))
            yield return WaitForRailWalker();

        if (elevator != null)
            elevator.OpenDoors();

        if (_railWalker.MovePortToPort(intersection, roomPort, IntersectionRailPort.Elevator))
            yield return WaitForRailWalker();
    }

    private IEnumerator LeaveElevatorToTargetRoom(RoomManager targetRoom, IntersectionSplineExpander intersection, IntersectionElevator elevator)
    {
        IntersectionRailPort targetPort = intersection.GetRoomPort(targetRoom);

        if (_railWalker.MovePortToPort(intersection, IntersectionRailPort.Elevator, targetPort))
            yield return WaitForRailWalker();

        if (elevator != null)
            elevator.CloseDoors();

        _railWalker.MoveToRoom(targetRoom);
        yield return WaitForRailWalker();
    }

    private void FaceTowardsUpcomingElevatorExit(IntersectionSplineExpander intersection, IntersectionRailPort targetPort)
    {
        if (intersection == null)
            return;

        if (!intersection.TryGetTransition(IntersectionRailPort.Elevator, targetPort, out int splineIndex, out _))
            return;

        Vector3 exitDirection = -intersection.EvaluateDirectionWorldFromLine(splineIndex, 1f);
        FaceDirection(exitDirection);
    }

    private IEnumerator MoveOffRailTo(Vector3 targetPosition, bool playWalk)
    {
        if (playWalk)
            PlayAnimation(_walkStateName);

        while ((targetPosition - transform.position).sqrMagnitude > _offRailArrivalDistance * _offRailArrivalDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _offRailMoveSpeed * Time.deltaTime);
            FaceTowards(targetPosition);
            yield return null;
        }

        transform.position = targetPosition;
        FaceTowards(targetPosition);
        PlayAnimation(_idleStateName);
    }

    private IEnumerator WaitForRailWalker()
    {
        if (_railWalker == null)
            yield break;

        while (_railWalker.IsMovingOnRail)
            yield return null;
    }

    private void FaceTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        FaceDirection(direction);
    }

    private void FaceDirection(Vector3 direction)
    {
        if (_railWalker != null)
        {
            _railWalker.FaceDirection(direction);
            return;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void ReleaseCurrentMachineSlot()
    {
        if (_currentMachine == null)
            return;

        _currentMachine.ReleaseWorkSlot(this);
        _currentMachine = null;
        _currentWorkSlot = -1;
    }

    private void ReleasePendingReservation()
    {
        if (_pendingMachine == null)
            return;

        _pendingMachine.ReleaseWorkSlot(this);
        _pendingMachine = null;
        _pendingWorkSlot = -1;
    }

    private void ClearBusyState()
    {
        _isBusy = false;
        _isWorking = false;
        _currentRoutine = null;
        _targetMachine = null;
    }

    private void PlayAnimation(string stateName)
    {
        if (_animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        if (_currentAnimationState == stateName)
            return;

        _animator.Play(stateName, 0, 0f);
        _currentAnimationState = stateName;
    }
}
