using UnityEngine;

public class FlamethrowerRobotController : MonoBehaviour, ILevelResettable
{
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

    private Quaternion _initialRotationRootLocalRotation;
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

        _initialRotationRootLocalRotation = _rotationRoot.localRotation;
    }

    private void Start()
    {
        if (_playFireOnStart)
            StartFireVfx();
    }

    private void Update()
    {
        RotateRobot();
        UpdateFireVfxTransform();
        UpdateFireDamage();
    }

    private void RotateRobot()
    {
        if (!_rotateContinuously || _rotationRoot == null)
            return;

        Vector3 axis = _rotationAxis.sqrMagnitude > 0.0001f ? _rotationAxis.normalized : Vector3.up;
        _rotationRoot.Rotate(axis, _rotationSpeed * Time.deltaTime, _rotationSpace);
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

    public void ResetLevelState()
    {
        _damageTimer = 0f;
        _nextAllowedKillTime = 0f;
        _playerInsideFire = false;
        _lineBlocked = false;

        if (_resetRotationOnLevelReset && _rotationRoot != null)
            _rotationRoot.localRotation = _initialRotationRootLocalRotation;

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
    }
}
