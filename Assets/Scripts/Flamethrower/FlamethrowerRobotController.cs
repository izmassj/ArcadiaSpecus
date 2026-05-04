using System.Collections.Generic;
using UnityEngine;

public class FlamethrowerRobotController : MonoBehaviour, ILevelResettable
{
    private enum RotationTargetMode
    {
        SingleRoot,
        SeparatePartsAroundPivot,
        SeparatePartsSelfRotation
    }

    [Header("Scene References")]
    [SerializeField] private RobotController _player;
    [SerializeField] private LevelDeathManager _deathManager;
    [SerializeField] private Transform _rotationRoot;
    [SerializeField] private Transform _firePoint;

    [Header("VFX")]
    [SerializeField] private GameObject _fireVfxRoot;
    [SerializeField] private GameObject _fireVfxPrefab;
    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private bool _playFireOnStart = true;
    [SerializeField] private bool _spawnFireVfxAsChild = true;
    [SerializeField] private bool _followFirePointWithVfx = true;
    [SerializeField] private Vector3 _fireVfxLocalPosition;
    [SerializeField] private Vector3 _fireVfxRotationOffsetEuler;

    [Header("Rotation")]
    [SerializeField] private bool _rotateContinuously = true;
    [SerializeField] private float _rotationSpeed = 90f;
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;
    [SerializeField] private Space _rotationSpace = Space.Self;
    [SerializeField] private bool _resetRotationOnLevelReset = true;

    [Header("Rotation Target")]
    [SerializeField] private RotationTargetMode _rotationTargetMode = RotationTargetMode.SingleRoot;
    [SerializeField] private bool _rotateInLateUpdate = true;
    [SerializeField] private bool _autoUseFirePointParentAsRotationRoot = true;
    [SerializeField] private Transform _rotationPivot;
    [SerializeField] private Transform[] _separateRotatingParts;

    [Header("Fire Damage")]
    [SerializeField] private bool _fireDamageEnabled = true;
    [SerializeField] private float _fireLength = 5f;
    [SerializeField] private float _fireRadius = 0.65f;
    [SerializeField] private Vector3 _fireForwardLocal = Vector3.forward;
    [SerializeField] private Vector3 _damageOriginLocalOffset;
    [SerializeField] private Vector3 _explosionOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float _damageCheckInterval = 0.04f;
    [SerializeField] private float _killCooldownAfterHit = 3f;
    [SerializeField] private LayerMask _damageMask = ~0;
    [SerializeField] private QueryTriggerInteraction _damageTriggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Obstacle Blocking")]
    [SerializeField] private bool _blockFireWithObstacles = true;
    [SerializeField] private LayerMask _obstacleMask = ~0;
    [SerializeField] private QueryTriggerInteraction _obstacleTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Debug")]
    [SerializeField] private bool _playerInsideFire;
    [SerializeField] private bool _lineBlocked;

    private readonly Collider[] _overlapHits = new Collider[24];
    private readonly RaycastHit[] _rayHits = new RaycastHit[24];
    private readonly List<Transform> _runtimeRotatingParts = new List<Transform>(8);

    private Quaternion _initialRotationRootLocalRotation;
    private Vector3 _initialRotationRootLocalPosition;
    private Vector3[] _initialPartLocalPositions;
    private Quaternion[] _initialPartLocalRotations;

    private float _damageTimer;
    private float _nextAllowedKillTime;
    private GameObject _spawnedFireVfx;

    private void Awake()
    {
        AutoResolveReferences();

        if (_player == null)
            _player = FindObjectOfType<RobotController>();

        if (_deathManager == null)
            _deathManager = FindObjectOfType<LevelDeathManager>();

        if (_rotationRoot == null)
            _rotationRoot = transform;

        if (_firePoint == null)
            _firePoint = _rotationRoot;

        if (_rotationPivot == null)
            _rotationPivot = _firePoint != null ? _firePoint : _rotationRoot;

        if (_autoUseFirePointParentAsRotationRoot && _rotationTargetMode == RotationTargetMode.SingleRoot)
            TryUseFirePointParentAsRotationRoot();

        CacheInitialRotationState();
    }

    private void Start()
    {
        if (_playFireOnStart)
            StartFireVfx();
    }

    private void Update()
    {
        if (_rotateInLateUpdate)
            return;

        TickFlamethrower();
    }

    private void LateUpdate()
    {
        if (!_rotateInLateUpdate)
            return;

        TickFlamethrower();
    }

    private void TickFlamethrower()
    {
        RotateFlamethrower();
        UpdateFireVfxTransform();
        UpdateFireDamage();
    }

    private void RotateFlamethrower()
    {
        if (!_rotateContinuously)
            return;

        Vector3 axis = _rotationAxis.sqrMagnitude > 0.0001f ? _rotationAxis.normalized : Vector3.up;
        float angle = _rotationSpeed * Time.deltaTime;

        switch (_rotationTargetMode)
        {
            case RotationTargetMode.SingleRoot:
                RotateSingleRoot(axis, angle);
                break;

            case RotationTargetMode.SeparatePartsAroundPivot:
                RotateSeparatePartsAroundPivot(axis, angle);
                break;

            case RotationTargetMode.SeparatePartsSelfRotation:
                RotateSeparatePartsSelf(axis, angle);
                break;
        }
    }

    private void RotateSingleRoot(Vector3 axis, float angle)
    {
        if (_rotationRoot == null)
            return;

        _rotationRoot.Rotate(axis, angle, _rotationSpace);
    }

    private void RotateSeparatePartsAroundPivot(Vector3 axis, float angle)
    {
        Transform pivot = _rotationPivot != null ? _rotationPivot : _rotationRoot;
        if (pivot == null)
            return;

        Vector3 worldAxis = _rotationSpace == Space.Self ? pivot.TransformDirection(axis) : axis;
        if (worldAxis.sqrMagnitude < 0.0001f)
            worldAxis = Vector3.up;

        worldAxis.Normalize();

        for (int i = 0; i < _runtimeRotatingParts.Count; i++)
        {
            Transform part = _runtimeRotatingParts[i];
            if (part == null || part == pivot)
                continue;

            part.RotateAround(pivot.position, worldAxis, angle);
        }
    }

    private void RotateSeparatePartsSelf(Vector3 axis, float angle)
    {
        for (int i = 0; i < _runtimeRotatingParts.Count; i++)
        {
            Transform part = _runtimeRotatingParts[i];
            if (part == null)
                continue;

            part.Rotate(axis, angle, _rotationSpace);
        }
    }

    private void UpdateFireDamage()
    {
        _playerInsideFire = false;
        _lineBlocked = false;

        if (!_fireDamageEnabled || _player == null || Time.time < _nextAllowedKillTime)
            return;

        _damageTimer += Time.deltaTime;
        if (_damageTimer < _damageCheckInterval)
            return;

        _damageTimer = 0f;

        Vector3 start = GetFireOrigin();
        Vector3 end = start + GetFireForward() * _fireLength;
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            start,
            end,
            Mathf.Max(0.01f, _fireRadius),
            _overlapHits,
            _damageMask,
            _damageTriggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapHits[i];
            if (hit == null)
                continue;

            RobotController robot = hit.GetComponentInParent<RobotController>();
            if (robot == null)
                continue;

            _playerInsideFire = true;

            if (_blockFireWithObstacles && IsBlockedByObstacle(start, robot))
            {
                _lineBlocked = true;
                continue;
            }

            KillRobot(robot);
            return;
        }
    }

    private bool IsBlockedByObstacle(Vector3 origin, RobotController robot)
    {
        if (robot == null)
            return false;

        Vector3 target = robot.transform.position + _explosionOffset;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance < 0.0001f)
            return false;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction.normalized,
            _rayHits,
            distance,
            _obstacleMask,
            _obstacleTriggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _rayHits[i].collider;
            if (hit == null)
                continue;

            Transform hitTransform = hit.transform;

            if (hitTransform.IsChildOf(transform))
                continue;

            if (hit.GetComponentInParent<RobotController>() == robot)
                continue;

            return true;
        }

        return false;
    }

    private void KillRobot(RobotController robot)
    {
        _nextAllowedKillTime = Time.time + Mathf.Max(0.1f, _killCooldownAfterHit);

        if (_explosionPrefab != null)
            Instantiate(_explosionPrefab, robot.transform.position + _explosionOffset, Quaternion.identity);

        if (_deathManager != null)
            _deathManager.KillPlayer();
    }

    private Vector3 GetFireOrigin()
    {
        Transform source = _firePoint != null ? _firePoint : transform;
        return source.TransformPoint(_damageOriginLocalOffset);
    }

    private Vector3 GetFireForward()
    {
        Transform source = _firePoint != null ? _firePoint : transform;
        Vector3 localForward = _fireForwardLocal.sqrMagnitude > 0.0001f ? _fireForwardLocal.normalized : Vector3.forward;
        Vector3 forward = source.TransformDirection(localForward);
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : source.forward;
    }

    private void StartFireVfx()
    {
        if (_fireVfxRoot == null && _spawnedFireVfx == null && _fireVfxPrefab != null)
        {
            Transform parent = _spawnFireVfxAsChild && _firePoint != null ? _firePoint : null;
            _spawnedFireVfx = Instantiate(_fireVfxPrefab, parent);
            _fireVfxRoot = _spawnedFireVfx;
        }

        if (_fireVfxRoot == null)
            return;

        _fireVfxRoot.SetActive(true);
        UpdateFireVfxTransform(true);
        PlayParticles(_fireVfxRoot);
    }

    private void UpdateFireVfxTransform(bool force = false)
    {
        if (!_followFirePointWithVfx && !force)
            return;

        if (_fireVfxRoot == null || _firePoint == null)
            return;

        if (_fireVfxRoot.transform.parent == _firePoint)
        {
            _fireVfxRoot.transform.localPosition = _fireVfxLocalPosition;
            _fireVfxRoot.transform.localRotation = Quaternion.Euler(_fireVfxRotationOffsetEuler);
        }
        else
        {
            _fireVfxRoot.transform.SetPositionAndRotation(
                _firePoint.TransformPoint(_fireVfxLocalPosition),
                _firePoint.rotation * Quaternion.Euler(_fireVfxRotationOffsetEuler));
        }
    }

    private void PlayParticles(GameObject root)
    {
        if (root == null)
            return;

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
            particles[i].Play(true);
    }

    private void StopParticles(GameObject root)
    {
        if (root == null)
            return;

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void AutoResolveReferences()
    {
        if (_rotationRoot == null)
        {
            Transform resolved = transform.Find("RotationRoot");
            if (resolved != null)
                _rotationRoot = resolved;
        }

        if (_firePoint == null)
        {
            Transform resolved = transform.Find("FirePoint");
            if (resolved != null)
                _firePoint = resolved;
        }
    }

    private void TryUseFirePointParentAsRotationRoot()
    {
        if (_firePoint == null || _firePoint.parent == null)
            return;

        if (_rotationRoot == null || _rotationRoot == transform || _rotationRoot.name == "Root")
            _rotationRoot = _firePoint.parent;
    }

    private void CacheInitialRotationState()
    {
        if (_rotationRoot != null)
        {
            _initialRotationRootLocalPosition = _rotationRoot.localPosition;
            _initialRotationRootLocalRotation = _rotationRoot.localRotation;
        }

        RebuildRuntimeRotatingParts();

        _initialPartLocalPositions = new Vector3[_runtimeRotatingParts.Count];
        _initialPartLocalRotations = new Quaternion[_runtimeRotatingParts.Count];

        for (int i = 0; i < _runtimeRotatingParts.Count; i++)
        {
            Transform part = _runtimeRotatingParts[i];
            if (part == null)
                continue;

            _initialPartLocalPositions[i] = part.localPosition;
            _initialPartLocalRotations[i] = part.localRotation;
        }
    }

    private void RebuildRuntimeRotatingParts()
    {
        _runtimeRotatingParts.Clear();

        if (_separateRotatingParts != null)
        {
            for (int i = 0; i < _separateRotatingParts.Length; i++)
                AddRuntimeRotatingPart(_separateRotatingParts[i]);
        }

        if (_runtimeRotatingParts.Count == 0 && _rotationRoot != null)
            AddRuntimeRotatingPart(_rotationRoot);
    }

    private void AddRuntimeRotatingPart(Transform part)
    {
        if (part == null)
            return;

        if (_runtimeRotatingParts.Contains(part))
            return;

        _runtimeRotatingParts.Add(part);
    }

    public void ResetLevelState()
    {
        _damageTimer = 0f;
        _nextAllowedKillTime = 0f;
        _playerInsideFire = false;
        _lineBlocked = false;

        if (_resetRotationOnLevelReset)
        {
            if (_rotationRoot != null)
            {
                _rotationRoot.localPosition = _initialRotationRootLocalPosition;
                _rotationRoot.localRotation = _initialRotationRootLocalRotation;
            }

            if (_initialPartLocalPositions != null && _initialPartLocalRotations != null)
            {
                for (int i = 0; i < _runtimeRotatingParts.Count; i++)
                {
                    Transform part = _runtimeRotatingParts[i];
                    if (part == null || i >= _initialPartLocalPositions.Length || i >= _initialPartLocalRotations.Length)
                        continue;

                    part.localPosition = _initialPartLocalPositions[i];
                    part.localRotation = _initialPartLocalRotations[i];
                }
            }
        }

        if (_fireVfxRoot != null)
        {
            StopParticles(_fireVfxRoot);
            StartFireVfx();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform source = _firePoint != null ? _firePoint : transform;
        Vector3 localForward = _fireForwardLocal.sqrMagnitude > 0.0001f ? _fireForwardLocal.normalized : Vector3.forward;
        Vector3 start = source.TransformPoint(_damageOriginLocalOffset);
        Vector3 forward = source.TransformDirection(localForward);

        if (forward.sqrMagnitude < 0.0001f)
            forward = source.forward;

        forward.Normalize();
        Vector3 end = start + forward * _fireLength;

        Gizmos.color = _lineBlocked ? Color.red : new Color(1f, 0.45f, 0f, 0.9f);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, _fireRadius);
        Gizmos.DrawWireSphere(end, _fireRadius);

        DrawRotationDebugGizmos();
    }

    private void DrawRotationDebugGizmos()
    {
        Vector3 axis = _rotationAxis.sqrMagnitude > 0.0001f ? _rotationAxis.normalized : Vector3.up;

        if (_rotationTargetMode == RotationTargetMode.SingleRoot)
        {
            Transform root = _rotationRoot != null ? _rotationRoot : transform;
            Vector3 worldAxis = _rotationSpace == Space.Self ? root.TransformDirection(axis) : axis;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(root.position, worldAxis.normalized * 1.5f);
            return;
        }

        Transform pivot = _rotationPivot != null ? _rotationPivot : _rotationRoot;
        if (pivot == null)
            return;

        Vector3 pivotAxis = _rotationSpace == Space.Self ? pivot.TransformDirection(axis) : axis;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pivot.position, 0.18f);
        Gizmos.DrawRay(pivot.position, pivotAxis.normalized * 1.5f);
    }
}
