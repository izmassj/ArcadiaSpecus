using UnityEngine;

public class NPCRailWalker : MonoBehaviour
{
    [Header("Start")]
    [SerializeField] private RoomManager _startRoom;
    [SerializeField] private IntersectionSplineExpander _railOwner;
    [SerializeField] private int _splineIndex = -1;
    [SerializeField] private bool _pickRandomSplineIfNeeded = true;
    [SerializeField] private bool _randomizeStartOffset;
    [SerializeField, Range(0f, 1f)] private float _startOffsetNormalized;

    [Header("Movement")]
    [SerializeField] private bool _moveOnStart = true;
    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _pingPong;

    [Header("Visual")]
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private bool _flipVisualByDirection = true;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _walkStateName = "Walk";
    [SerializeField] private string _idleStateName = "Idle";

    [Header("Debug")]
    [SerializeField] private bool _isMoving;
    [SerializeField] private float _currentDistance;
    [SerializeField] private int _direction = 1;

    private float _visualBaseScaleX = 1f;
    private string _currentAnimationState;

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
        ResolveRail();

        if (_railOwner == null || !_railOwner.HasUsableSpline(_splineIndex))
        {
            SetMoving(false);
            return;
        }

        if (_randomizeStartOffset)
            _startOffsetNormalized = Random.value;

        _currentDistance = _railOwner.GetSplineLength(_splineIndex) * Mathf.Clamp01(_startOffsetNormalized);
        SnapToRail();
        SetMoving(_moveOnStart);
    }

    private void Update()
    {
        if (_railOwner == null || !_railOwner.HasUsableSpline(_splineIndex))
        {
            SetMoving(false);
            return;
        }

        if (!_isMoving)
            return;

        float splineLength = _railOwner.GetSplineLength(_splineIndex);

        if (splineLength <= 0.001f)
        {
            SetMoving(false);
            return;
        }

        _currentDistance += _moveSpeed * Time.deltaTime * _direction;
        HandleBounds(splineLength);
        SnapToRail();
    }

    public void InitializeFromRoom(RoomManager room)
    {
        _startRoom = room;
        _railOwner = room != null ? room.GetRailOwner() : null;
        _splineIndex = room != null ? room.GetBranchIndex() : -1;
        _currentDistance = 0f;
        ResolveRail();
        SnapToRail();
    }

    public void SetRail(IntersectionSplineExpander railOwner, int splineIndex)
    {
        _startRoom = null;
        _railOwner = railOwner;
        _splineIndex = splineIndex;
        _currentDistance = 0f;
        ResolveRail();
        SnapToRail();
    }

    public void SetMoving(bool value)
    {
        _isMoving = value;
        PlayAnimation(_isMoving ? _walkStateName : _idleStateName);
    }

    private void ResolveRail()
    {
        if (_startRoom != null)
        {
            _railOwner = _startRoom.GetRailOwner();
            _splineIndex = _startRoom.GetBranchIndex();
        }

        if (_railOwner == null)
            return;

        if (_railOwner.HasUsableSpline(_splineIndex))
            return;

        _splineIndex = _pickRandomSplineIfNeeded
            ? _railOwner.GetRandomUsableSplineIndex()
            : _railOwner.GetFirstUsableSplineIndex();
    }

    private void HandleBounds(float splineLength)
    {
        if (_loop)
        {
            if (_currentDistance > splineLength)
                _currentDistance -= splineLength;
            else if (_currentDistance < 0f)
                _currentDistance += splineLength;

            return;
        }

        if (_pingPong)
        {
            if (_currentDistance > splineLength)
            {
                _currentDistance = splineLength;
                _direction = -1;
            }
            else if (_currentDistance < 0f)
            {
                _currentDistance = 0f;
                _direction = 1;
            }

            return;
        }

        _currentDistance = Mathf.Clamp(_currentDistance, 0f, splineLength);

        if (_currentDistance <= 0f || _currentDistance >= splineLength)
            SetMoving(false);
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
