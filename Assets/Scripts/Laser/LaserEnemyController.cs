using UnityEngine;

public class LaserEnemyController : MonoBehaviour, ILevelResettable
{
    private enum LaserMode
    {
        Static,
        Sweep,
        Intermittent
    }

    [Header("Mode")]
    [SerializeField] private LaserMode _mode = LaserMode.Static;
    [SerializeField] private bool _laserStartsActive = true;

    [Header("Scene References")]
    [SerializeField] private RobotController _player;
    [SerializeField] private LevelDeathManager _deathManager;
    [SerializeField] private Transform _headRoot;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _laserVfxRoot;
    [SerializeField] private Transform _laserCastOrigin;
    [SerializeField] private Transform _laserDirectionRoot;
    [SerializeField] private GameObject _explosionPrefab;

    [Header("Laser Damage")]
    [SerializeField] private float _laserLength = 18f;
    [SerializeField] private float _laserHitRadius = 0.18f;
    [SerializeField] private LayerMask _laserHitMask = ~0;
    [SerializeField] private Vector3 _laserDirectionLocal = Vector3.forward;
    [SerializeField] private Vector3 _explosionOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float _killCooldown = 0.25f;

    [Header("VFX Follow")]
    [SerializeField] private bool _alignLaserVfxToFirePoint = true;
    [SerializeField] private bool _preserveInitialVfxOffset = true;
    [SerializeField] private bool _forceVfxParticlesToFollowLaser = true;
    [SerializeField] private bool _forceLaserTrailsToFollowLaser = true;

    [Header("Sweep Mode")]
    [SerializeField] private float _sweepMinAngle = -55f;
    [SerializeField] private float _sweepMaxAngle = 55f;
    [SerializeField] private float _sweepSpeed = 70f;
    [SerializeField] private Vector3 _sweepAxisLocal = Vector3.up;

    [Header("Intermittent Mode")]
    [SerializeField] private float _intermittentOnSeconds = 1.25f;
    [SerializeField] private float _intermittentOffSeconds = 1f;
    [SerializeField] private bool _intermittentStartsOn = true;

    [Header("Debug")]
    [SerializeField] private bool _laserActive;
    [SerializeField] private float _currentSweepAngle;
    [SerializeField] private float _sweepDirection = 1f;
    [SerializeField] private float _intermittentTimer;

    private readonly RaycastHit[] _hits = new RaycastHit[16];

    private ParticleSystem[] _laserParticleSystems = new ParticleSystem[0];
    private Quaternion _baseHeadLocalRotation;
    private Vector3 _initialVfxLocalPosition;
    private Quaternion _initialVfxLocalRotation;
    private Vector3 _vfxOffsetFromFirePoint;
    private Quaternion _vfxRotationOffsetFromFirePoint;
    private float _lastKillTime = -999f;

    private void Awake()
    {
        AutoResolveReferences();
        ResolveInvalidSceneReferences();

        if (_player == null)
            _player = FindObjectOfType<RobotController>();

        if (_deathManager == null)
            _deathManager = FindObjectOfType<LevelDeathManager>();

        if (_headRoot == null)
            _headRoot = transform;

        if (_laserDirectionRoot == null)
            _laserDirectionRoot = _laserVfxRoot != null ? _laserVfxRoot : _firePoint;

        if (_laserCastOrigin == null)
            _laserCastOrigin = _laserVfxRoot != null ? _laserVfxRoot : _firePoint;

        _baseHeadLocalRotation = _headRoot != null ? _headRoot.localRotation : Quaternion.identity;

        if (_laserVfxRoot != null)
        {
            _initialVfxLocalPosition = _laserVfxRoot.localPosition;
            _initialVfxLocalRotation = _laserVfxRoot.localRotation;
        }

        CacheVfxFollowOffset();
        RefreshLaserParticleCache();
        ConfigureLaserParticlesForFollowing();
    }

    private void Start()
    {
        ResetModeRuntimeValues();
        ApplyLaserActive(GetInitialLaserState());
        UpdateLaserVfxTransform();
    }

    private void Update()
    {
        UpdateMode();
    }

    private void LateUpdate()
    {
        UpdateLaserVfxTransform();

        if (_laserActive)
            CheckLaserHit();
    }

    private void UpdateMode()
    {
        switch (_mode)
        {
            case LaserMode.Sweep:
                UpdateSweep();
                break;
            case LaserMode.Intermittent:
                UpdateIntermittent();
                break;
            default:
                ApplyLaserActive(_laserStartsActive);
                break;
        }
    }

    private void UpdateSweep()
    {
        if (_headRoot == null)
            return;

        _currentSweepAngle += _sweepDirection * _sweepSpeed * Time.deltaTime;

        if (_currentSweepAngle >= _sweepMaxAngle)
        {
            _currentSweepAngle = _sweepMaxAngle;
            _sweepDirection = -1f;
        }
        else if (_currentSweepAngle <= _sweepMinAngle)
        {
            _currentSweepAngle = _sweepMinAngle;
            _sweepDirection = 1f;
        }

        Vector3 axis = _sweepAxisLocal.sqrMagnitude > 0.0001f ? _sweepAxisLocal.normalized : Vector3.up;
        _headRoot.localRotation = _baseHeadLocalRotation * Quaternion.AngleAxis(_currentSweepAngle, axis);
        ApplyLaserActive(_laserStartsActive);
    }

    private void UpdateIntermittent()
    {
        _intermittentTimer -= Time.deltaTime;
        if (_intermittentTimer > 0f)
            return;

        bool nextState = !_laserActive;
        ApplyLaserActive(nextState);
        _intermittentTimer = nextState ? Mathf.Max(0.01f, _intermittentOnSeconds) : Mathf.Max(0.01f, _intermittentOffSeconds);
    }

    private void CheckLaserHit()
    {
        Transform originTransform = _laserCastOrigin != null ? _laserCastOrigin : transform;
        Vector3 origin = originTransform.position;
        Vector3 direction = GetLaserDirection();

        if (direction.sqrMagnitude < 0.0001f)
            return;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            Mathf.Max(0.01f, _laserHitRadius),
            direction.normalized,
            _hits,
            Mathf.Max(0.01f, _laserLength),
            _laserHitMask,
            QueryTriggerInteraction.Collide);

        if (hitCount <= 0)
            return;

        RobotController nearestRobot = null;
        float nearestRobotDistance = float.MaxValue;
        float nearestBlockingDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _hits[i].collider;
            if (hitCollider == null)
                continue;

            Transform hitTransform = hitCollider.transform;
            if (hitTransform == null || hitTransform.IsChildOf(transform))
                continue;

            float distance = _hits[i].distance;
            RobotController robot = hitCollider.GetComponentInParent<RobotController>();

            if (robot != null)
            {
                if (distance < nearestRobotDistance)
                {
                    nearestRobotDistance = distance;
                    nearestRobot = robot;
                }

                continue;
            }

            if (!hitCollider.isTrigger && distance < nearestBlockingDistance)
                nearestBlockingDistance = distance;
        }

        if (nearestRobot == null)
            return;

        if (nearestRobotDistance > nearestBlockingDistance + 0.05f)
            return;

        KillRobot(nearestRobot);
    }

    private void KillRobot(RobotController robot)
    {
        if (Time.time < _lastKillTime + _killCooldown)
            return;

        _lastKillTime = Time.time;

        if (_explosionPrefab != null)
            Instantiate(_explosionPrefab, robot.transform.position + _explosionOffset, Quaternion.identity);

        if (_deathManager == null)
            _deathManager = FindObjectOfType<LevelDeathManager>();

        if (_deathManager != null)
        {
            _deathManager.KillPlayer();
        }
        else
        {
            Debug.LogWarning($"{nameof(LaserEnemyController)}: el láser ha detectado al robot, pero no hay LevelDeathManager en la escena.", this);
        }
    }

    private Vector3 GetLaserDirection()
    {
        Transform directionRoot = _laserDirectionRoot != null ? _laserDirectionRoot : transform;
        Vector3 localDirection = _laserDirectionLocal.sqrMagnitude > 0.0001f ? _laserDirectionLocal.normalized : Vector3.forward;
        return directionRoot.TransformDirection(localDirection).normalized;
    }

    private void ApplyLaserActive(bool active)
    {
        bool stateChanged = _laserActive != active;
        _laserActive = active;

        if (_laserVfxRoot == null)
            return;

        bool rootStateChanged = _laserVfxRoot.gameObject.activeSelf != active;
        if (rootStateChanged)
            _laserVfxRoot.gameObject.SetActive(active);

        if (!stateChanged && !rootStateChanged)
            return;

        if (_laserParticleSystems == null || _laserParticleSystems.Length == 0)
            RefreshLaserParticleCache();

        for (int i = 0; i < _laserParticleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _laserParticleSystems[i];
            if (particleSystem == null)
                continue;

            if (active)
                particleSystem.Play(true);
            else
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void UpdateLaserVfxTransform()
    {
        if (!_alignLaserVfxToFirePoint || _laserVfxRoot == null || _firePoint == null)
            return;

        if (_preserveInitialVfxOffset)
        {
            _laserVfxRoot.SetPositionAndRotation(
                _firePoint.position + _firePoint.rotation * _vfxOffsetFromFirePoint,
                _firePoint.rotation * _vfxRotationOffsetFromFirePoint);
        }
        else
        {
            _laserVfxRoot.SetPositionAndRotation(_firePoint.position, _firePoint.rotation);
        }
    }

    private void RefreshLaserParticleCache()
    {
        _laserParticleSystems = _laserVfxRoot != null ? _laserVfxRoot.GetComponentsInChildren<ParticleSystem>(true) : new ParticleSystem[0];
    }

    private void ConfigureLaserParticlesForFollowing()
    {
        if (!_forceVfxParticlesToFollowLaser || _laserVfxRoot == null)
            return;

        if (_laserParticleSystems == null || _laserParticleSystems.Length == 0)
            RefreshLaserParticleCache();

        for (int i = 0; i < _laserParticleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _laserParticleSystems[i];
            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.customSimulationSpace = _laserVfxRoot;

            if (_forceLaserTrailsToFollowLaser)
            {
                ParticleSystem.TrailModule trails = particleSystem.trails;
                if (trails.enabled)
                {
                    trails.worldSpace = false;
                    trails.dieWithParticles = true;
                }
            }
        }
    }

    private void CacheVfxFollowOffset()
    {
        if (_laserVfxRoot == null || _firePoint == null)
            return;

        _vfxOffsetFromFirePoint = Quaternion.Inverse(_firePoint.rotation) * (_laserVfxRoot.position - _firePoint.position);
        _vfxRotationOffsetFromFirePoint = Quaternion.Inverse(_firePoint.rotation) * _laserVfxRoot.rotation;
    }

    private bool GetInitialLaserState()
    {
        if (_mode == LaserMode.Intermittent)
            return _intermittentStartsOn;

        return _laserStartsActive;
    }

    private void ResetModeRuntimeValues()
    {
        _currentSweepAngle = 0f;
        _sweepDirection = Mathf.Approximately(_sweepDirection, 0f) ? 1f : Mathf.Sign(_sweepDirection);
        _intermittentTimer = GetInitialLaserState() ? Mathf.Max(0.01f, _intermittentOnSeconds) : Mathf.Max(0.01f, _intermittentOffSeconds);
        _lastKillTime = -999f;

        if (_headRoot != null)
            _headRoot.localRotation = _baseHeadLocalRotation;

        if (_laserVfxRoot != null)
        {
            _laserVfxRoot.localPosition = _initialVfxLocalPosition;
            _laserVfxRoot.localRotation = _initialVfxLocalRotation;
        }

        CacheVfxFollowOffset();
        ConfigureLaserParticlesForFollowing();
    }

    private void AutoResolveReferences()
    {
        if (_headRoot == null)
        {
            Transform resolved = transform.Find("Model/Armature/Bone");
            if (resolved != null)
                _headRoot = resolved;
        }

        if (_firePoint == null)
        {
            Transform resolved = transform.Find("Model/Armature/Bone/Bone_end");
            if (resolved != null)
                _firePoint = resolved;
        }

        if (_laserVfxRoot == null)
        {
            Transform resolved = transform.Find("Particles/VFX_Flowing_Laser");
            if (resolved != null)
            {
                _laserVfxRoot = resolved;
            }
            else
            {
                Transform particlesRoot = transform.Find("Particles");
                if (particlesRoot != null)
                {
                    ParticleSystem ps = particlesRoot.GetComponentInChildren<ParticleSystem>(true);
                    if (ps != null)
                        _laserVfxRoot = GetHighestChildUnder(ps.transform, particlesRoot);
                }
            }
        }
    }

    private void ResolveInvalidSceneReferences()
    {
        if (_laserVfxRoot == null)
            return;

        if (_firePoint == null || _firePoint == _laserVfxRoot)
        {
            Transform resolved = transform.Find("Model/Armature/Bone/Bone_end");
            if (resolved != null)
                _firePoint = resolved;
        }
    }

    private Transform GetHighestChildUnder(Transform child, Transform parent)
    {
        Transform current = child;
        while (current.parent != null && current.parent != parent)
            current = current.parent;

        return current;
    }

    public void ResetLevelState()
    {
        ResetModeRuntimeValues();
        ApplyLaserActive(GetInitialLaserState());
        UpdateLaserVfxTransform();
    }

    private void OnDrawGizmosSelected()
    {
        Transform originTransform = _laserCastOrigin != null ? _laserCastOrigin : (_laserVfxRoot != null ? _laserVfxRoot : transform);
        Vector3 origin = originTransform.position;
        Vector3 direction = Application.isPlaying ? GetLaserDirection() : originTransform.TransformDirection(_laserDirectionLocal.sqrMagnitude > 0.0001f ? _laserDirectionLocal.normalized : Vector3.forward);

        Gizmos.color = _laserActive || !Application.isPlaying ? Color.red : Color.gray;
        Gizmos.DrawLine(origin, origin + direction.normalized * Mathf.Max(0.01f, _laserLength));
        Gizmos.DrawWireSphere(origin, Mathf.Max(0.01f, _laserHitRadius));
    }
}
