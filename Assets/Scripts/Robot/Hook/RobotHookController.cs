using UnityEngine;
using UnityEngine.InputSystem;

public class RobotHookController : MonoBehaviour
{
    private enum HookState
    {
        Ready,
        Shooting,
        Pulling,
        Carrying,
        Cooldown
    }

    [Header("References")]
    [SerializeField] private RobotController _robotController;
    [SerializeField] private Transform _hookOriginPoint;
    [SerializeField] private Transform _carryPoint;
    [SerializeField] private Camera _aimCamera;
    [SerializeField] private HookRopeSegmentsRenderer _ropeRenderer;
    [SerializeField] private Transform _hookHeadVisual;
    [SerializeField] private GameObject _targetIndicatorPrefab;
    [SerializeField] private Transform _runtimeVisualParent;

    [Header("Existing Hook Visual")]
    [SerializeField] private bool _restoreHookVisualToOriginalPose = true;
    [SerializeField] private bool _carryObjectOnHookVisual = true;
    [SerializeField] private bool _hideHookVisualWhenDisabled = false;

    [Header("Input")]
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private string _gameplayMapName = "Gameplay";
    [SerializeField] private string _hookActionName = "Hook";

    [Header("Target Search")]
    [SerializeField] private LayerMask _grabbableMask = ~0;
    [SerializeField] private LayerMask _lineOfSightMask = ~0;
    [SerializeField] private float _maxTargetRange = 12f;
    [SerializeField] private float _targetRefreshInterval = 0.05f;
    [SerializeField] [Range(0.02f, 0.6f)] private float _maxViewportDistanceFromCenter = 0.28f;
    [SerializeField] [Range(1f, 89f)] private float _fallbackHalfAngle = 24f;
    [SerializeField] private float _distanceScoreWeight = 0.25f;
    [SerializeField] private bool _requireLineOfSight = true;

    [Header("Shooting")]
    [SerializeField] private float _shootSpeed = 28f;
    [SerializeField] private float _attachDistance = 0.35f;
    [SerializeField] private float _shootTimeout = 0.7f;
    [SerializeField] private float _cooldownSeconds = 0.35f;

    [Header("Pull")]
    [SerializeField] private float _pullMinSpeed = 4.5f;
    [SerializeField] private float _pullMaxSpeed = 14f;
    [SerializeField] private float _robotMovementSpeedBonus = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float _verticalPullMultiplier = 0.35f;
    [SerializeField] private float _carryDistance = 0.75f;
    [SerializeField] private float _maxAttachedRopeDistance = 16f;
    [SerializeField] private float _stuckBreakSeconds = 0.85f;
    [SerializeField] private float _stuckMinProgress = 0.08f;
    [SerializeField] private bool _breakWhenLineOfSightBlocked = true;

    [Header("Carrying")]
    [SerializeField] private bool _detectCollisionsWhileCarried = false;
    [SerializeField] private bool _carryWithoutParenting = true;
    [SerializeField] private bool _lockMovementWhileShootingAndPulling = true;
    [SerializeField] private bool _lockMovementWhileCarrying = false;
    [SerializeField] private bool _forceThirdPersonWhenUsingHook = true;

    [Header("VFX")]
    [SerializeField] private GameObject _shootVfxPrefab;
    [SerializeField] private GameObject _attachVfxPrefab;
    [SerializeField] private GameObject _releaseVfxPrefab;

    [Header("Debug")]
    [SerializeField] private HookState _state = HookState.Ready;
    [SerializeField] private HookGrabbableObject _currentTarget;
    [SerializeField] private HookGrabbableObject _carriedObject;
    [SerializeField] private float _currentDistance;

    private readonly Collider[] _targetHits = new Collider[64];
    private readonly RaycastHit[] _lineHits = new RaycastHit[16];

    private InputActionMap _gameplayMap;
    private InputAction _hookAction;
    private HookTargetIndicator _indicatorInstance;
    private float _targetRefreshTimer;
    private float _stateTimer;
    private float _cooldownTimer;
    private float _pullStartDistance;
    private float _lastBestPullDistance;
    private float _stuckTimer;
    private Vector3 _hookHeadPosition;
    private bool _robotLockedByHook;

    private Transform _hookVisualOriginalParent;
    private Vector3 _hookVisualOriginalLocalPosition;
    private Quaternion _hookVisualOriginalLocalRotation;
    private Vector3 _hookVisualOriginalLocalScale;
    private bool _hookVisualOriginalActive;
    private bool _hasStoredHookVisualPose;

    private void Reset()
    {
        _robotController = GetComponent<RobotController>();
    }

    private void Awake()
    {
        if (_robotController == null)
            _robotController = GetComponent<RobotController>();

        if (_hookOriginPoint == null)
            _hookOriginPoint = transform;

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_runtimeVisualParent == null)
            _runtimeVisualParent = transform;

        StoreHookVisualOriginalPose();
        CreateIndicatorInstance();
        ResetHookVisualToOrigin();
    }

    private void OnEnable()
    {
        StartInputActions();
        ResetHookVisualToOrigin();
    }

    private void Update()
    {
        RefreshTargetIfNeeded();
        HandleHookInput();
        UpdateState();
        UpdateVisuals();
    }

    private void OnDisable()
    {
        UnlockRobotMovement();

        if (_indicatorInstance != null)
            _indicatorInstance.SetTarget(null);

        if (_ropeRenderer != null)
            _ropeRenderer.Hide();

        ResetHookVisualToOrigin();

        if (_hideHookVisualWhenDisabled && _hookHeadVisual != null)
            _hookHeadVisual.gameObject.SetActive(false);
    }

    private void StartInputActions()
    {
        if (_inputActions == null)
            return;

        _gameplayMap = _inputActions.FindActionMap(_gameplayMapName, false);
        if (_gameplayMap == null)
            return;

        _hookAction = _gameplayMap.FindAction(_hookActionName, false);

        if (!_gameplayMap.enabled)
            _gameplayMap.Enable();
    }

    private void HandleHookInput()
    {
        if (_hookAction == null || !_hookAction.WasPressedThisFrame())
            return;

        if (_state == HookState.Cooldown)
            return;

        if (_carriedObject != null || _state == HookState.Carrying)
        {
            ReleaseCarriedObject();
            return;
        }

        if (_state != HookState.Ready)
            return;

        if (_currentTarget != null)
            BeginShooting(_currentTarget);
    }

    private void UpdateState()
    {
        _stateTimer += Time.deltaTime;

        switch (_state)
        {
            case HookState.Shooting:
                UpdateShooting();
                break;

            case HookState.Pulling:
                UpdatePulling();
                break;

            case HookState.Carrying:
                UpdateCarrying();
                break;

            case HookState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    private void RefreshTargetIfNeeded()
    {
        if (_state != HookState.Ready)
        {
            SetIndicatorTarget(null);
            return;
        }

        if (_carriedObject != null)
        {
            SetIndicatorTarget(null);
            return;
        }

        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer > 0f)
            return;

        _targetRefreshTimer = Mathf.Max(0.01f, _targetRefreshInterval);
        _currentTarget = FindBestTarget();
        SetIndicatorTarget(_currentTarget);
    }

    private HookGrabbableObject FindBestTarget()
    {
        Vector3 searchOrigin = transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(searchOrigin, _maxTargetRange, _targetHits, _grabbableMask, QueryTriggerInteraction.Collide);

        HookGrabbableObject best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _targetHits[i];
            if (hit == null)
                continue;

            HookGrabbableObject grabbable = hit.GetComponentInParent<HookGrabbableObject>();
            if (grabbable == null || !grabbable.CanBeGrabbed)
                continue;

            Vector3 targetPoint = grabbable.GetTargetPoint();
            float distance = Vector3.Distance(_hookOriginPoint.position, targetPoint);
            if (distance > _maxTargetRange)
                continue;

            if (_requireLineOfSight && !HasLineOfSight(targetPoint, grabbable.transform))
                continue;

            if (!TryGetAimScore(targetPoint, distance, out float score))
                continue;

            if (score < bestScore)
            {
                bestScore = score;
                best = grabbable;
            }
        }

        return best;
    }

    private bool TryGetAimScore(Vector3 targetPoint, float distance, out float score)
    {
        score = float.MaxValue;

        if (_aimCamera != null)
        {
            Vector3 viewport = _aimCamera.WorldToViewportPoint(targetPoint);
            if (viewport.z <= 0f)
                return false;

            Vector2 centerOffset = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            float centerDistance = centerOffset.magnitude;
            if (centerDistance > _maxViewportDistanceFromCenter)
                return false;

            float distance01 = Mathf.Clamp01(distance / Mathf.Max(0.01f, _maxTargetRange));
            score = centerDistance + distance01 * _distanceScoreWeight;
            return true;
        }

        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.001f)
            return false;

        float angle = Vector3.Angle(transform.forward, toTarget.normalized);
        if (angle > _fallbackHalfAngle)
            return false;

        float angle01 = angle / Mathf.Max(0.01f, _fallbackHalfAngle);
        float fallbackDistance01 = Mathf.Clamp01(distance / Mathf.Max(0.01f, _maxTargetRange));
        score = angle01 + fallbackDistance01 * _distanceScoreWeight;
        return true;
    }

    private bool HasLineOfSight(Vector3 targetPoint, Transform targetRoot)
    {
        Vector3 origin = _aimCamera != null ? _aimCamera.transform.position : _hookOriginPoint.position;
        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return true;

        int hitCount = Physics.RaycastNonAlloc(origin, direction / distance, _lineHits, distance, _lineOfSightMask, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
            return true;

        float closestDistance = float.MaxValue;
        Collider closest = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _lineHits[i].collider;
            if (hitCollider == null)
                continue;

            if (hitCollider.transform.IsChildOf(transform))
                continue;

            if (_lineHits[i].distance < closestDistance)
            {
                closestDistance = _lineHits[i].distance;
                closest = hitCollider;
            }
        }

        if (closest == null)
            return true;

        return closest.transform == targetRoot || closest.transform.IsChildOf(targetRoot);
    }

    private void BeginShooting(HookGrabbableObject target)
    {
        _currentTarget = target;
        _state = HookState.Shooting;
        _stateTimer = 0f;
        _hookHeadPosition = _hookOriginPoint.position;

        SetIndicatorTarget(null);
        LockRobotMovement();

        if (_forceThirdPersonWhenUsingHook && _robotController != null)
            _robotController.ForceThirdPerson(false);

        MoveHookVisualToWorldPose(_hookHeadPosition, _hookOriginPoint.rotation);

        if (_shootVfxPrefab != null)
            Instantiate(_shootVfxPrefab, _hookOriginPoint.position, _hookOriginPoint.rotation);
    }

    private void UpdateShooting()
    {
        if (_currentTarget == null || !_currentTarget.CanBeGrabbed)
        {
            CancelHookUse();
            return;
        }

        Vector3 targetPoint = _currentTarget.GetTargetPoint();
        _hookHeadPosition = Vector3.MoveTowards(_hookHeadPosition, targetPoint, _shootSpeed * Time.deltaTime);

        Quaternion hookRotation = GetHookRotation(targetPoint - _hookHeadPosition, _hookOriginPoint.rotation);
        MoveHookVisualToWorldPose(_hookHeadPosition, hookRotation);

        if (Vector3.Distance(_hookHeadPosition, targetPoint) <= _attachDistance)
        {
            BeginPulling();
            return;
        }

        if (_stateTimer >= _shootTimeout || Vector3.Distance(_hookOriginPoint.position, _hookHeadPosition) > _maxTargetRange + 0.25f)
            CancelHookUse();
    }

    private void BeginPulling()
    {
        if (_currentTarget == null)
        {
            CancelHookUse();
            return;
        }

        _currentTarget.BeginHooked(_hookHeadPosition);
        _state = HookState.Pulling;
        _stateTimer = 0f;
        _stuckTimer = 0f;
        _pullStartDistance = Vector3.Distance(_hookOriginPoint.position, _currentTarget.GetRopePoint());
        _lastBestPullDistance = _pullStartDistance;

        if (_attachVfxPrefab != null)
            Instantiate(_attachVfxPrefab, _currentTarget.GetRopePoint(), Quaternion.identity);
    }

    private void UpdatePulling()
    {
        if (_currentTarget == null || _currentTarget.Rigidbody == null)
        {
            CancelHookUse();
            return;
        }

        Vector3 targetPoint = _currentTarget.GetRopePoint();
        Vector3 origin = _hookOriginPoint.position;
        Vector3 toOrigin = origin - targetPoint;
        float distance = toOrigin.magnitude;
        _currentDistance = distance;

        if (distance <= _carryDistance)
        {
            BeginCarrying();
            return;
        }

        if (distance > _maxAttachedRopeDistance)
        {
            CancelHookUse();
            return;
        }

        if (_breakWhenLineOfSightBlocked && _requireLineOfSight && !HasLineOfSight(targetPoint, _currentTarget.transform))
        {
            CancelHookUse();
            return;
        }

        UpdateStuckTimer(distance);
        if (_stuckTimer >= _stuckBreakSeconds)
        {
            CancelHookUse();
            return;
        }

        float closeness01 = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, _pullStartDistance));
        float speed = Mathf.Lerp(_pullMinSpeed, _pullMaxSpeed, closeness01);

        if (_robotController != null)
            speed += _robotController.WorldVelocity.magnitude * _robotMovementSpeedBonus;

        Vector3 direction = toOrigin.normalized;
        direction.y *= _verticalPullMultiplier;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = toOrigin.normalized;
        else
            direction.Normalize();

        _currentTarget.Rigidbody.linearVelocity = direction * speed;
        _hookHeadPosition = targetPoint;

        Quaternion hookRotation = GetHookRotation(origin - targetPoint, _hookOriginPoint.rotation);
        MoveHookVisualToWorldPose(targetPoint, hookRotation);
    }

    private void UpdateStuckTimer(float distance)
    {
        if (distance < _lastBestPullDistance - _stuckMinProgress)
        {
            _lastBestPullDistance = distance;
            _stuckTimer = 0f;
            return;
        }

        _stuckTimer += Time.deltaTime;
    }

    private void BeginCarrying()
    {
        if (_currentTarget == null)
        {
            CancelHookUse();
            return;
        }

        ResetHookVisualToOrigin();

        _carriedObject = _currentTarget;
        _currentTarget = null;
        _state = HookState.Carrying;
        _stateTimer = 0f;

        Transform carryTarget = GetCarryTarget();
        _carriedObject.BeginCarried(carryTarget, _detectCollisionsWhileCarried, !_carryWithoutParenting);
        _carriedObject.UpdateCarriedPose();

        if (_ropeRenderer != null)
            _ropeRenderer.Hide();

        if (!_lockMovementWhileCarrying)
            UnlockRobotMovement();
        else
            LockRobotMovement();
    }

    private void UpdateCarrying()
    {
        if (_carriedObject == null)
        {
            StartCooldown();
            return;
        }

        if (_carriedObject != null)
            _carriedObject.UpdateCarriedPose();

        if (_lockMovementWhileCarrying)
            LockRobotMovement();
    }

    private Transform GetCarryTarget()
    {
        if (_carryPoint != null)
            return _carryPoint;

        if (_carryObjectOnHookVisual && _hookHeadVisual != null)
            return _hookHeadVisual;

        if (_hookOriginPoint != null)
            return _hookOriginPoint;

        return transform;
    }

    private void ReleaseCarriedObject()
    {
        if (_carriedObject == null)
        {
            StartCooldown();
            return;
        }

        Vector3 releasePosition = _carriedObject.transform.position;
        _carriedObject.ReleaseCarried(false);
        _carriedObject = null;

        if (_releaseVfxPrefab != null)
            Instantiate(_releaseVfxPrefab, releasePosition, Quaternion.identity);

        ResetHookVisualToOrigin();
        UnlockRobotMovement();
        StartCooldown();
    }

    private void CancelHookUse()
    {
        if (_currentTarget != null)
        {
            _currentTarget.CancelHook();
            _currentTarget = null;
        }

        ResetHookVisualToOrigin();

        if (_ropeRenderer != null)
            _ropeRenderer.Hide();

        UnlockRobotMovement();
        StartCooldown();
    }

    private void StartCooldown()
    {
        _state = HookState.Cooldown;
        _stateTimer = 0f;
        _cooldownTimer = Mathf.Max(0f, _cooldownSeconds);
        _currentTarget = null;
        SetIndicatorTarget(null);
    }

    private void UpdateCooldown()
    {
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f)
            return;

        _state = HookState.Ready;
        _stateTimer = 0f;
    }

    private void UpdateVisuals()
    {
        if (_ropeRenderer == null || _hookOriginPoint == null)
            return;

        if (_state == HookState.Shooting)
        {
            _ropeRenderer.SetRope(_hookOriginPoint.position, _hookHeadPosition, false);
            return;
        }

        if (_state == HookState.Pulling && _currentTarget != null)
        {
            _ropeRenderer.SetRope(_hookOriginPoint.position, _currentTarget.GetRopePoint(), true);
            return;
        }

        if (_state != HookState.Carrying)
            _ropeRenderer.Hide();
    }

    private void LockRobotMovement()
    {
        if (!_lockMovementWhileShootingAndPulling || _robotController == null || _robotLockedByHook)
            return;

        _robotLockedByHook = true;
        _robotController.SetInputLocked(true, true);
        _robotController.SetMovementLocked(true, true);
    }

    private void UnlockRobotMovement()
    {
        if (_robotController == null || !_robotLockedByHook)
            return;

        _robotLockedByHook = false;
        _robotController.SetInputLocked(false, false);
        _robotController.SetMovementLocked(false, false);
    }

    private void CreateIndicatorInstance()
    {
        if (_targetIndicatorPrefab == null || _indicatorInstance != null)
            return;

        GameObject instance = Instantiate(_targetIndicatorPrefab, _runtimeVisualParent != null ? _runtimeVisualParent : transform);
        _indicatorInstance = instance.GetComponent<HookTargetIndicator>();
        if (_indicatorInstance == null)
            _indicatorInstance = instance.AddComponent<HookTargetIndicator>();

        _indicatorInstance.SetCamera(_aimCamera);
        _indicatorInstance.SetTarget(null);
    }

    private void SetIndicatorTarget(HookGrabbableObject target)
    {
        if (_indicatorInstance == null)
            CreateIndicatorInstance();

        if (_indicatorInstance != null)
            _indicatorInstance.SetTarget(target);
    }

    private void StoreHookVisualOriginalPose()
    {
        if (_hookHeadVisual == null || _hasStoredHookVisualPose)
            return;

        _hookVisualOriginalParent = _hookHeadVisual.parent;
        _hookVisualOriginalLocalPosition = _hookHeadVisual.localPosition;
        _hookVisualOriginalLocalRotation = _hookHeadVisual.localRotation;
        _hookVisualOriginalLocalScale = _hookHeadVisual.localScale;
        _hookVisualOriginalActive = _hookHeadVisual.gameObject.activeSelf;
        _hasStoredHookVisualPose = true;
    }

    private void MoveHookVisualToWorldPose(Vector3 position, Quaternion rotation)
    {
        if (_hookHeadVisual == null)
            return;

        StoreHookVisualOriginalPose();

        Transform parent = _runtimeVisualParent != null ? _runtimeVisualParent : null;
        if (_hookHeadVisual.parent != parent)
            _hookHeadVisual.SetParent(parent, true);

        if (!_hookHeadVisual.gameObject.activeSelf)
            _hookHeadVisual.gameObject.SetActive(true);

        _hookHeadVisual.position = position;
        _hookHeadVisual.rotation = rotation;
    }

    private void ResetHookVisualToOrigin()
    {
        if (_hookHeadVisual == null)
            return;

        StoreHookVisualOriginalPose();

        if (_restoreHookVisualToOriginalPose && _hasStoredHookVisualPose)
        {
            _hookHeadVisual.SetParent(_hookVisualOriginalParent, false);
            _hookHeadVisual.localPosition = _hookVisualOriginalLocalPosition;
            _hookHeadVisual.localRotation = _hookVisualOriginalLocalRotation;
            _hookHeadVisual.localScale = _hookVisualOriginalLocalScale;
            _hookHeadVisual.gameObject.SetActive(_hookVisualOriginalActive);
            return;
        }

        _hookHeadVisual.SetParent(_hookOriginPoint, false);
        _hookHeadVisual.localPosition = Vector3.zero;
        _hookHeadVisual.localRotation = Quaternion.identity;
        _hookHeadVisual.gameObject.SetActive(true);
    }

    private Quaternion GetHookRotation(Vector3 direction, Quaternion fallback)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return fallback;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _maxTargetRange);

        if (_hookOriginPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_hookOriginPoint.position, 0.12f);
        }
    }
}
