using System.Collections.Generic;
using UnityEngine;

public class NPCRailWalker : MonoBehaviour
{
    [Header("Start")]
    [SerializeField] private RoomManager _startRoom;
    [SerializeField] private bool _snapToStartRoomOnStart = true;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 50f;
    [SerializeField] private float _arrivalDistance = 0.05f;

    [Header("Visual")]
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private bool _flipVisualByDirection = true;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _walkStateName = "Walk";
    [SerializeField] private string _idleStateName = "Idle";

    [Header("Debug")]
    [SerializeField] private IntersectionSplineExpander _railOwner;
    [SerializeField] private int _splineIndex = -1;
    [SerializeField] private float _currentDistance;
    [SerializeField] private float _targetDistance;
    [SerializeField] private bool _isMovingOnRail;
    [SerializeField] private float _visualYawOffset = 0f;

    private Quaternion _visualBaseLocalRotation;
    private string _currentAnimationState;
    private float _lastMovementSign = 1f;
    private Vector3 _lastFacingDirection = Vector3.forward;
    private readonly Queue<RailMoveRequest> _queuedMoves = new();
    private bool _keepWalkAnimationOnNextRailStop;

    private struct RailMoveRequest
    {
        public IntersectionSplineExpander Owner;
        public int SplineIndex;
        public float StartNormalized;
        public float EndNormalized;
    }

    public bool IsMovingOnRail => _isMovingOnRail;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_visualRoot == null)
            _visualRoot = _animator != null ? _animator.transform : transform;

        _visualBaseLocalRotation = _visualRoot.localRotation;

        if (_animator != null)
            _animator.applyRootMotion = false;
    }

    private void Start()
    {
        if (_snapToStartRoomOnStart && _startRoom != null)
            InitializeFromRoom(_startRoom);
    }

    private void Update()
    {
        if (!_isMovingOnRail)
            return;

        if (_railOwner == null || !_railOwner.HasUsableSpline(_splineIndex))
        {
            StopRailMovement();
            return;
        }

        float previousDistance = _currentDistance;
        float maxStep = _moveSpeed * Time.deltaTime;
        _currentDistance = Mathf.MoveTowards(_currentDistance, _targetDistance, maxStep);

        float delta = _currentDistance - previousDistance;
        if (Mathf.Abs(delta) > 0.0001f)
            _lastMovementSign = Mathf.Sign(delta);

        SnapToRail();

        if (Mathf.Abs(_targetDistance - _currentDistance) <= _arrivalDistance)
        {
            _currentDistance = _targetDistance;
            SnapToRail();

            if (TryStartNextQueuedMove())
                return;

            StopRailMovement();
        }
    }

    public RoomManager GetStartRoom() => _startRoom;

    public void SetStartRoomReferenceOnly(RoomManager room)
    {
        _startRoom = room;
    }

    public void InitializeFromRoom(RoomManager room)
    {
        _startRoom = room;
        SnapToRoom(room);
    }

    public void SnapToRoom(RoomManager room)
    {
        if (room == null)
            return;

        IntersectionSplineExpander owner = room.GetRailOwner();
        int splineIndex = room.GetBranchIndex();
        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return;

        float targetNormalized = owner.GetRoomNormalizedT(room);
        _queuedMoves.Clear();
        SnapToNormalized(owner, splineIndex, targetNormalized);
    }

    public void SnapToIntersectionPort(IntersectionSplineExpander intersection, IntersectionRailPort port)
    {
        SnapToIntersectionPort(intersection, port, IntersectionRailPort.None);
    }

    public void SnapToIntersectionPort(IntersectionSplineExpander intersection, IntersectionRailPort port, IntersectionRailPort elevatorSideHint)
    {
        if (intersection == null)
            return;

        _queuedMoves.Clear();

        switch (port)
        {
            case IntersectionRailPort.LeftRooms:
                SnapToNormalized(intersection, intersection.GetLeftBranchIndex(), 0f);
                break;

            case IntersectionRailPort.RightRooms:
                SnapToNormalized(intersection, intersection.GetRightBranchIndex(), 0f);
                break;

            case IntersectionRailPort.Elevator:
                SnapToNormalized(intersection, intersection.GetElevatorSplineIndexForPort(elevatorSideHint), 1f);
                break;
        }
    }

    public bool MoveToRoom(RoomManager room)
    {
        _queuedMoves.Clear();
        return QueueMoveToRoom(room, true);
    }

    public void KeepWalkAnimationOnNextRailStop()
    {
        _keepWalkAnimationOnNextRailStop = true;
    }

    public bool MoveBetweenRoomsOnSameBranch(RoomManager fromRoom, RoomManager toRoom)
    {
        _queuedMoves.Clear();
        return QueueMoveBetweenRoomsOnSameBranch(fromRoom, toRoom, true);
    }

    public bool MoveToBranchStart(RoomManager room)
    {
        _queuedMoves.Clear();
        return QueueMoveToBranchStart(room, true);
    }

    public bool MoveToRoomFromBranchStart(RoomManager room)
    {
        _queuedMoves.Clear();
        return QueueMoveToRoomFromBranchStart(room, true);
    }

    public bool MovePortToPort(IntersectionSplineExpander intersection, IntersectionRailPort from, IntersectionRailPort to)
    {
        _queuedMoves.Clear();
        return QueueMovePortToPort(intersection, from, to, true);
    }

    public bool QueueMoveToRoom(RoomManager room) => QueueMoveToRoom(room, false);
    public bool QueueMoveBetweenRoomsOnSameBranch(RoomManager fromRoom, RoomManager toRoom) => QueueMoveBetweenRoomsOnSameBranch(fromRoom, toRoom, false);
    public bool QueueMoveToBranchStart(RoomManager room) => QueueMoveToBranchStart(room, false);
    public bool QueueMoveToRoomFromBranchStart(RoomManager room)
    {
        return QueueMoveToRoomFromBranchStart(room, false);
    }
    public bool QueueMovePortToPort(IntersectionSplineExpander intersection, IntersectionRailPort from, IntersectionRailPort to) => QueueMovePortToPort(intersection, from, to, false);

    public void FaceTowards(Vector3 targetWorldPosition)
    {
        Vector3 direction = targetWorldPosition - transform.position;
        FaceDirection(direction);
    }

    public void FaceDirection(Vector3 worldDirection)
    {
        UpdateVisualDirection(worldDirection);
    }

    public void SetWalkAnimation(bool value)
    {
        PlayAnimation(value ? _walkStateName : _idleStateName);
    }

    private bool QueueMoveToRoom(RoomManager room, bool startImmediately)
    {
        if (room == null)
            return false;

        IntersectionSplineExpander owner = room.GetRailOwner();
        int splineIndex = room.GetBranchIndex();
        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return false;

        float roomT = owner.GetRoomNormalizedT(room);
        float startT = roomT;

        if (_railOwner == owner && _splineIndex == splineIndex)
        {
            float splineLength = owner.GetSplineLength(splineIndex);
            if (splineLength > 0.001f)
                startT = Mathf.Clamp01(_currentDistance / splineLength);
        }
        else
        {
            startT = owner.GetClosestNormalizedT(splineIndex, transform.position);
        }

        return QueueOrStartMove(owner, splineIndex, startT, roomT, startImmediately);
    }

    private bool QueueMoveBetweenRoomsOnSameBranch(RoomManager fromRoom, RoomManager toRoom, bool startImmediately)
    {
        if (fromRoom == null || toRoom == null)
            return false;

        IntersectionSplineExpander owner = fromRoom.GetRailOwner();
        int splineIndex = fromRoom.GetBranchIndex();

        if (owner == null || owner != toRoom.GetRailOwner() || splineIndex != toRoom.GetBranchIndex())
            return false;

        if (!owner.HasUsableSpline(splineIndex))
            return false;

        float fromT = owner.GetRoomNormalizedT(fromRoom);
        float toT = owner.GetRoomNormalizedT(toRoom);
        return QueueOrStartMove(owner, splineIndex, fromT, toT, startImmediately);
    }

    private bool QueueMoveToBranchStart(RoomManager room, bool startImmediately)
    {
        if (room == null)
            return false;

        IntersectionSplineExpander owner = room.GetRailOwner();
        int splineIndex = room.GetBranchIndex();
        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return false;

        float roomT = owner.GetRoomNormalizedT(room);
        return QueueOrStartMove(owner, splineIndex, roomT, 0f, startImmediately);
    }

    private bool QueueMoveToRoomFromBranchStart(RoomManager room, bool startImmediately)
    {
        if (room == null)
            return false;

        IntersectionSplineExpander owner = room.GetRailOwner();
        int splineIndex = room.GetBranchIndex();

        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return false;

        float roomT = owner.GetRoomNormalizedT(room);
        return QueueOrStartMove(owner, splineIndex, 0f, roomT, startImmediately);
    }

    private bool QueueMovePortToPort(IntersectionSplineExpander intersection, IntersectionRailPort from, IntersectionRailPort to, bool startImmediately)
    {
        if (intersection == null)
            return false;

        if (!intersection.TryGetTransitionSegment(from, to, out int splineIndex, out float startT, out float endT))
            return false;

        return QueueOrStartMove(intersection, splineIndex, startT, endT, startImmediately);
    }

    private bool QueueOrStartMove(IntersectionSplineExpander owner, int splineIndex, float startNormalized, float endNormalized, bool startImmediately)
    {
        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return false;

        RailMoveRequest request = new()
        {
            Owner = owner,
            SplineIndex = splineIndex,
            StartNormalized = Mathf.Clamp01(startNormalized),
            EndNormalized = Mathf.Clamp01(endNormalized)
        };

        if (!_isMovingOnRail || startImmediately)
        {
            if (_isMovingOnRail && startImmediately)
                _queuedMoves.Clear();

            return BeginMove(request);
        }

        _queuedMoves.Enqueue(request);
        return true;
    }

    private bool BeginMove(RailMoveRequest request)
    {
        if (request.Owner == null || !request.Owner.HasUsableSpline(request.SplineIndex))
            return false;

        _railOwner = request.Owner;
        _splineIndex = request.SplineIndex;

        float splineLength = _railOwner.GetSplineLength(_splineIndex);
        if (splineLength <= 0.001f)
            return false;

        _currentDistance = Mathf.Clamp01(request.StartNormalized) * splineLength;
        _targetDistance = Mathf.Clamp01(request.EndNormalized) * splineLength;

        float distanceDelta = _targetDistance - _currentDistance;
        if (Mathf.Abs(distanceDelta) > 0.0001f)
            _lastMovementSign = Mathf.Sign(distanceDelta);

        _isMovingOnRail = true;
        SetWalkAnimation(true);
        SnapToRail();
        return true;
    }

    private bool TryStartNextQueuedMove()
    {
        while (_queuedMoves.Count > 0)
        {
            RailMoveRequest nextRequest = _queuedMoves.Dequeue();
            if (BeginMove(nextRequest))
                return true;
        }

        return false;
    }

    private void SnapToNormalized(IntersectionSplineExpander owner, int splineIndex, float normalized)
    {
        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return;

        _keepWalkAnimationOnNextRailStop = false;
        _railOwner = owner;
        _splineIndex = splineIndex;
        _currentDistance = owner.GetSplineLength(splineIndex) * Mathf.Clamp01(normalized);
        _targetDistance = _currentDistance;
        _lastMovementSign = 1f;
        _isMovingOnRail = false;
        SnapToRail();
        SetWalkAnimation(false);
    }

    private void StopRailMovement()
    {
        _isMovingOnRail = false;

        if (_keepWalkAnimationOnNextRailStop)
        {
            _keepWalkAnimationOnNextRailStop = false;
            return;
        }

        SetWalkAnimation(false);
    }

    private void SnapToRail()
    {
        if (_railOwner == null || !_railOwner.HasUsableSpline(_splineIndex))
            return;

        float splineLength = _railOwner.GetSplineLength(_splineIndex);
        if (splineLength <= 0.001f)
            return;

        float normalizedT = Mathf.Clamp01(_currentDistance / splineLength);
        Vector3 previousPosition = transform.position;
        Vector3 worldPosition = _railOwner.EvaluatePositionWorld(_splineIndex, normalizedT);
        Vector3 tangent = _railOwner.EvaluateDirectionWorldFromLine(_splineIndex, normalizedT);

        if (_lastMovementSign < 0f)
            tangent = -tangent;

        transform.position = worldPosition;

        Vector3 movementDirection = worldPosition - previousPosition;
        movementDirection = Vector3.ProjectOnPlane(movementDirection, Vector3.up);

        if (movementDirection.sqrMagnitude > 0.000001f)
            UpdateVisualDirection(movementDirection);
        else
            UpdateVisualDirection(tangent);
    }

    private void UpdateVisualDirection(Vector3 direction)
    {
        if (!_flipVisualByDirection || _visualRoot == null)
            return;

        Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            flatDirection = _lastFacingDirection;
            if (flatDirection.sqrMagnitude <= 0.0001f)
                return;
        }

        _lastFacingDirection = flatDirection.normalized;

        Quaternion lookRotation = Quaternion.LookRotation(_lastFacingDirection, Vector3.up);
        Quaternion finalWorldRotation = lookRotation * Quaternion.Euler(0f, _visualYawOffset, 0f);

        if (_visualRoot.parent != null)
            _visualRoot.localRotation = Quaternion.Inverse(_visualRoot.parent.rotation) * finalWorldRotation * _visualBaseLocalRotation;
        else
            _visualRoot.rotation = finalWorldRotation * _visualBaseLocalRotation;
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
