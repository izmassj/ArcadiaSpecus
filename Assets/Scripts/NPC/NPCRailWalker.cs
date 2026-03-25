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

    private float _visualBaseScaleX = 1f;
    private string _currentAnimationState;

    public bool IsMovingOnRail => _isMovingOnRail;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_visualRoot == null)
            _visualRoot = _animator != null ? _animator.transform : transform;

        _visualBaseScaleX = Mathf.Abs(_visualRoot.localScale.x);

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

        float maxStep = _moveSpeed * Time.deltaTime;
        _currentDistance = Mathf.MoveTowards(_currentDistance, _targetDistance, maxStep);
        SnapToRail();

        if (Mathf.Abs(_targetDistance - _currentDistance) <= _arrivalDistance)
        {
            _currentDistance = _targetDistance;
            SnapToRail();
            StopRailMovement();
        }
    }

    public RoomManager GetStartRoom()
    {
        return _startRoom;
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
        SnapToNormalized(owner, splineIndex, targetNormalized);
    }

    public void SnapToIntersectionPort(IntersectionSplineExpander intersection, IntersectionRailPort port)
    {
        if (intersection == null)
            return;

        switch (port)
        {
            case IntersectionRailPort.LeftRooms:
                SnapToNormalized(intersection, intersection.GetLeftBranchIndex(), 0f);
                break;

            case IntersectionRailPort.RightRooms:
                SnapToNormalized(intersection, intersection.GetRightBranchIndex(), 0f);
                break;

            case IntersectionRailPort.Elevator:
                SnapToNormalized(intersection, intersection.GetPreferredElevatorSplineIndex(), 1f);
                break;
        }
    }

    public bool MoveToRoom(RoomManager room)
    {
        if (room == null)
            return false;

        IntersectionSplineExpander owner = room.GetRailOwner();
        int splineIndex = room.GetBranchIndex();

        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return false;

        float targetNormalized = owner.GetRoomNormalizedT(room);
        return MoveToNormalized(owner, splineIndex, targetNormalized);
    }

    public bool MoveToBranchStart(RoomManager room)
    {
        if (room == null)
            return false;

        IntersectionSplineExpander owner = room.GetRailOwner();
        int splineIndex = room.GetBranchIndex();

        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return false;

        return MoveToNormalized(owner, splineIndex, 0f);
    }

    public bool MovePortToPort(IntersectionSplineExpander intersection, IntersectionRailPort from, IntersectionRailPort to)
    {
        if (intersection == null)
            return false;

        if (!intersection.TryGetTransition(from, to, out int splineIndex, out float targetNormalizedT))
            return false;

        return MoveToNormalized(intersection, splineIndex, targetNormalizedT);
    }

    public void SetWalkAnimation(bool value)
    {
        PlayAnimation(value ? _walkStateName : _idleStateName);
    }

    private bool MoveToNormalized(IntersectionSplineExpander owner, int splineIndex, float targetNormalized)
    {
        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return false;

        _railOwner = owner;
        _splineIndex = splineIndex;

        float splineLength = _railOwner.GetSplineLength(_splineIndex);

        if (splineLength <= 0.001f)
            return false;

        float startNormalized = _railOwner.GetClosestNormalizedT(_splineIndex, transform.position);
        _currentDistance = startNormalized * splineLength;
        _targetDistance = Mathf.Clamp01(targetNormalized) * splineLength;
        _isMovingOnRail = true;
        SetWalkAnimation(true);
        SnapToRail();
        return true;
    }

    private void SnapToNormalized(IntersectionSplineExpander owner, int splineIndex, float normalized)
    {
        if (owner == null || !owner.HasUsableSpline(splineIndex))
            return;

        _railOwner = owner;
        _splineIndex = splineIndex;
        _currentDistance = owner.GetSplineLength(splineIndex) * Mathf.Clamp01(normalized);
        _targetDistance = _currentDistance;
        _isMovingOnRail = false;
        SnapToRail();
        SetWalkAnimation(false);
    }

    private void StopRailMovement()
    {
        _isMovingOnRail = false;
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
        Vector3 worldPosition = _railOwner.EvaluatePositionWorld(_splineIndex, normalizedT);
        Vector3 tangent = _railOwner.EvaluateTangentWorld(_splineIndex, normalizedT);

        transform.position = worldPosition;
        UpdateVisualDirection(tangent);
    }

    private void UpdateVisualDirection(Vector3 tangent)
    {
        if (!_flipVisualByDirection || _visualRoot == null)
            return;

        if (Mathf.Abs(tangent.x) <= 0.001f)
            return;

        Vector3 localScale = _visualRoot.localScale;
        localScale.x = tangent.x >= 0f ? _visualBaseScaleX : -_visualBaseScaleX;
        _visualRoot.localScale = localScale;
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
