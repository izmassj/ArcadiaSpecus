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

    public void AssignMachine(MachineManager machine)
    {
        if (machine == null)
            return;

        if (_currentMachine == machine && _isWorking)
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
            yield return GoToElevatorFromCurrentRoom(_currentRoom, currentIntersection);
            _railWalker.SnapToIntersectionPort(targetIntersection, IntersectionRailPort.Elevator);
            yield return LeaveElevatorToTargetRoom(targetRoom, targetIntersection);
        }

        reservedWorkPoint = targetMachine.GetWorkPointWorldPosition(reservedWorkSlot);
        yield return MoveOffRailTo(reservedWorkPoint, true);
        FaceTowards(targetMachine.transform.position);
        PlayAnimation(_workStateName);

        _currentRoom = targetRoom;
        _currentMachine = targetMachine;
        _currentWorkSlot = reservedWorkSlot;
        _pendingMachine = null;
        _pendingWorkSlot = -1;
        _isWorking = true;
        _isBusy = false;
        _currentRoutine = null;
    }

    private IEnumerator TravelInsideSingleIntersection(RoomManager fromRoom, RoomManager toRoom, IntersectionSplineExpander intersection)
    {
        IntersectionRailPort fromPort = intersection.GetRoomPort(fromRoom);
        IntersectionRailPort toPort = intersection.GetRoomPort(toRoom);

        if (fromPort != toPort)
        {
            _railWalker.MoveToBranchStart(fromRoom);
            yield return WaitForRailWalker();

            _railWalker.MovePortToPort(intersection, fromPort, toPort);
            yield return WaitForRailWalker();
        }

        _railWalker.MoveToRoom(toRoom);
        yield return WaitForRailWalker();
    }

    private IEnumerator GoToElevatorFromCurrentRoom(RoomManager room, IntersectionSplineExpander intersection)
    {
        IntersectionRailPort roomPort = intersection.GetRoomPort(room);

        _railWalker.MoveToBranchStart(room);
        yield return WaitForRailWalker();

        _railWalker.MovePortToPort(intersection, roomPort, IntersectionRailPort.Elevator);
        yield return WaitForRailWalker();
    }

    private IEnumerator LeaveElevatorToTargetRoom(RoomManager targetRoom, IntersectionSplineExpander intersection)
    {
        IntersectionRailPort targetPort = intersection.GetRoomPort(targetRoom);

        _railWalker.MovePortToPort(intersection, IntersectionRailPort.Elevator, targetPort);
        yield return WaitForRailWalker();

        _railWalker.MoveToRoom(targetRoom);
        yield return WaitForRailWalker();
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
        if (_railWalker != null)
        {
            _railWalker.FaceTowards(targetPosition);
            return;
        }

        Vector3 direction = targetPosition - transform.position;
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
