using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TurretEnemyController : MonoBehaviour, ILevelResettable
{
    private enum TurretState
    {
        Vigilant,
        Alerting,
        Opening,
        Focus,
        Firing
    }

    private enum ParticlePathAxis
    {
        X,
        Y,
        Z
    }

    [Header("Scene References")]
    [SerializeField] private RobotController _player;
    [SerializeField] private LevelDeathManager _deathManager;
    [SerializeField] private Transform _yawRoot;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private Animator _bodyAnimator;
    [SerializeField] private Transform _pitchPivot;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private SpriteRenderer _alertSprite;

    [Header("VFX")]
    [SerializeField] private GameObject _plasmaChargePrefab;
    [SerializeField] private GameObject _plasmaBurstPrefab;
    [SerializeField] private GameObject _plasmaShotPrefab;
    [SerializeField] private GameObject _explosionPrefab;
    [SerializeField] private bool _spawnChargeVfxDuringFocus = true;
    [SerializeField] private bool _followFirePointWhileCharging = true;
    [SerializeField] private bool _spawnFinalBurstOnShot = true;
    [SerializeField] private Vector3 _chargeRotationOffsetEuler;
    [SerializeField] private Vector3 _burstRotationOffsetEuler;
    [SerializeField] private Vector3 _shotRotationOffsetEuler;
    [SerializeField] private float _fallbackChargeLifetime = 4f;
    [SerializeField] private float _fallbackBurstLifetime = 2f;

    [Header("Detection")]
    [SerializeField] private float _detectionRange = 16f;
    [SerializeField] private float _scanMinYaw = -55f;
    [SerializeField] private float _scanMaxYaw = 55f;
    [SerializeField] private float _targetCheckInterval = 0.08f;
    [SerializeField] private float _lineOfSightInterval = 0.08f;
    [SerializeField] private float _loseFocusAfterNoSightSeconds = 2f;
    [SerializeField] private float _chargeToFireSeconds = 3f;
    [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private LayerMask _lineOfSightMask = ~0;
    [SerializeField] private bool _useUpperHemisphereDetection = true;
    [SerializeField] private float _lookZoneRange = 16f;
    [SerializeField] [Range(1f, 89f)] private float _lookZoneHalfAngle = 16f;

    [Header("Rotation")]
    [SerializeField] private float _scanYawSpeed = 70f;
    [SerializeField] private float _focusYawSpeed = 220f;
    [SerializeField] private float _focusPitchSpeed = 180f;
    [SerializeField] private float _minPitch = -35f;
    [SerializeField] private float _maxPitch = 35f;

    [Header("Yaw Rig")]
    [SerializeField] private float _focusYawVisualOffset = 180f;

    [Header("Pitch Rig")]
    [SerializeField] private Vector3 _pitchAxisLocal = Vector3.right;
    [SerializeField] private Vector3 _barrelForwardLocal = Vector3.up;

    [Header("Alert")]
    [SerializeField] private float _alertDuration = 0.6f;
    [SerializeField] private float _alertStretchYMultiplier = 1.18f;
    [SerializeField] private float _alertSpriteFadeDuration = 0.15f;

    [Header("Animator States")]
    [SerializeField] private string _openStateName = "TurretOpen";
    [SerializeField] private float _openStateDuration = 0.46666667f;
    [SerializeField] private string _closeStateName = "TurretClose";
    [SerializeField] private float _closeStateDuration = 0.43333334f;
    [SerializeField] private string _fireStateName = "TurretFire";
    [SerializeField] private float _fireCrossFade = 0.03f;
    [SerializeField] private float _openCrossFade = 0.03f;
    [SerializeField] private float _closeCrossFade = 0.03f;

    [Header("Projectile")]
    [SerializeField] private float _projectileSpeed = 20f;
    [SerializeField] private float _projectileHitRadius = 0.2f;
    [SerializeField] private float _projectileLifetime = 4f;
    [SerializeField] private LayerMask _projectileHitMask = ~0;
    [SerializeField] private bool _destroyProjectileOnObstacleHit = true;

    [Header("Projectile Particle Path")]
    [SerializeField] private bool _keepShotVisualRootAtStart = true;
    [SerializeField] private bool _configureShotShapePath = true;
    [SerializeField] private bool _useTargetAsShotPathEnd = true;
    [SerializeField] private float _shotPathMaxDistance = 40f;
    [SerializeField] private bool _overrideShotShapePosition = true;
    [SerializeField] private Vector3 _shotShapeLocalStart = Vector3.zero;
    [SerializeField] private bool _overrideShotShapeRotation = true;
    [SerializeField] private Vector3 _shotShapeRotationOffsetEuler = Vector3.zero;
    [SerializeField] private bool _overrideShotShapeScale = true;
    [SerializeField] private ParticlePathAxis _shotShapeLengthAxis = ParticlePathAxis.Z;
    [SerializeField] private Vector3 _shotShapeBaseScale = Vector3.one;
    [SerializeField] private float _shotShapeDistanceMultiplier = 1f;
    [SerializeField] private bool _overrideShotShapeLength = true;
    [SerializeField] private bool _fitShotLifetimeToPathDistance;

    [Header("Debug")]
    [SerializeField] private TurretState _state = TurretState.Vigilant;
    [SerializeField] private bool _hasLineOfSight;
    [SerializeField] private float _focusTimer;
    [SerializeField] private float _noSightTimer;
    [SerializeField] private float _currentPitch;
    [SerializeField] private float _currentScanYaw;

    private readonly RaycastHit[] _lineOfSightHits = new RaycastHit[8];

    private float _baseYawWorld;
    private float _detectTimer;
    private float _lineOfSightTimer;
    private float _openingTimer;
    private float _scanDirection = 1f;

    private Quaternion _basePitchLocalRotation;
    private Vector3 _baseVisualScale;
    private Color _alertSpriteBaseColor;

    private Tween _alertTween;
    private Coroutine _fireRoutine;
    private Coroutine _burstCleanupRoutine;
    private Coroutine _chargeCleanupRoutine;
    private GameObject _activeBurstInstance;
    private GameObject _activeChargeInstance;

    private bool _fireQueued;

    private void Awake()
    {
        AutoResolveReferences();

        if (_player == null)
            _player = FindObjectOfType<RobotController>();

        if (_deathManager == null)
            _deathManager = FindObjectOfType<LevelDeathManager>();

        if (_yawRoot == null)
            _yawRoot = transform;

        if (_visualRoot == null)
            _visualRoot = transform;

        _baseYawWorld = _yawRoot.eulerAngles.y;
        _basePitchLocalRotation = _pitchPivot != null ? _pitchPivot.localRotation : Quaternion.identity;
        _baseVisualScale = _visualRoot.localScale;

        if (_alertSprite != null)
        {
            _alertSpriteBaseColor = _alertSprite.color;
            Color c = _alertSpriteBaseColor;
            c.a = 0f;
            _alertSprite.color = c;
        }
    }

    private void Update()
    {
        if (_player == null)
            return;

        switch (_state)
        {
            case TurretState.Vigilant:
                UpdateVigilant();
                break;
            case TurretState.Alerting:
                UpdateAlerting();
                break;
            case TurretState.Opening:
                UpdateOpening();
                break;
            case TurretState.Focus:
                UpdateFocus();
                break;
            case TurretState.Firing:
                UpdateFiring();
                break;
        }
    }

    private void UpdateVigilant()
    {
        UpdateScanRotation();

        _detectTimer += Time.deltaTime;
        if (_detectTimer < _targetCheckInterval)
            return;

        _detectTimer = 0f;

        if (IsPlayerInsideDetectionZone())
            BeginAlert();
    }

    private void UpdateAlerting()
    {
        UpdateScanRotation();
    }

    private void UpdateOpening()
    {
        _openingTimer += Time.deltaTime;
        UpdateScanRotation();

        if (_openingTimer >= _openStateDuration)
            BeginFocus();
    }

    private void UpdateFocus()
    {
        UpdateAimTowardsPlayer();
        UpdateLineOfSight();
        UpdateChargeFollow();

        _focusTimer += Time.deltaTime;

        if (_hasLineOfSight)
        {
            _noSightTimer = 0f;
        }
        else
        {
            _noSightTimer += Time.deltaTime;
            if (_noSightTimer >= _loseFocusAfterNoSightSeconds)
            {
                ReturnToVigilant(true);
                return;
            }
        }

        if (_focusTimer >= _chargeToFireSeconds && !_fireQueued && _hasLineOfSight)
        {
            _fireQueued = true;
            _fireRoutine = StartCoroutine(FireRoutine());
        }
    }

    private void UpdateFiring()
    {
        UpdateAimTowardsPlayer();
        UpdateLineOfSight();
        UpdateChargeFollow();

        if (_hasLineOfSight)
        {
            _noSightTimer = 0f;
        }
        else
        {
            _noSightTimer += Time.deltaTime;
            if (_noSightTimer >= _loseFocusAfterNoSightSeconds)
                CancelPendingShotAndReturnToVigilant();
        }
    }

    private void BeginAlert()
    {
        if (_state != TurretState.Vigilant)
            return;

        _state = TurretState.Alerting;
        _focusTimer = 0f;
        _noSightTimer = 0f;
        _fireQueued = false;

        if (_alertTween != null && _alertTween.IsActive())
            _alertTween.Kill();

        if (_visualRoot != null)
            _visualRoot.localScale = _baseVisualScale;

        if (_alertSprite != null)
        {
            Color c = _alertSpriteBaseColor;
            c.a = 0f;
            _alertSprite.color = c;
        }

        Sequence sequence = DOTween.Sequence();

        if (_visualRoot != null)
        {
            Vector3 stretched = _baseVisualScale;
            stretched.y *= _alertStretchYMultiplier;

            sequence.Append(_visualRoot.DOScale(stretched, _alertDuration * 0.4f).SetEase(Ease.OutQuad));
            sequence.Append(_visualRoot.DOScale(_baseVisualScale, _alertDuration * 0.6f).SetEase(Ease.OutBack));
        }
        else
        {
            sequence.AppendInterval(_alertDuration);
        }

        if (_alertSprite != null)
        {
            sequence.Join(_alertSprite.DOFade(_alertSpriteBaseColor.a, _alertSpriteFadeDuration));
            sequence.Append(_alertSprite.DOFade(0f, _alertDuration * 0.4f));
        }

        _alertTween = sequence.OnComplete(BeginOpening);
    }

    private void BeginOpening()
    {
        _state = TurretState.Opening;
        _openingTimer = 0f;

        PlayState(_openStateName, _openCrossFade);
    }

    private void BeginFocus()
    {
        _state = TurretState.Focus;
        _focusTimer = 0f;
        _noSightTimer = 0f;
        _lineOfSightTimer = _lineOfSightInterval;
        _hasLineOfSight = CheckLineOfSight();

        if (_spawnChargeVfxDuringFocus)
            StartChargeVfx();
    }

    private IEnumerator FireRoutine()
    {
        _state = TurretState.Firing;

        StopChargeVfx();

        if (_spawnFinalBurstOnShot)
            SpawnBurst();

        PlayState(_fireStateName, _fireCrossFade);
        yield return null;
        FireProjectile();

        _state = TurretState.Focus;
        _fireRoutine = null;
    }

    private void StartChargeVfx()
    {
        StopChargeVfx();

        GameObject chargePrefab = _plasmaChargePrefab != null ? _plasmaChargePrefab : _plasmaBurstPrefab;
        if (chargePrefab == null || _firePoint == null)
            return;

        _activeChargeInstance = Instantiate(chargePrefab, _firePoint.position, GetChargeRotation());
        AdjustNonLoopParticleLifetimes(_activeChargeInstance, Mathf.Max(0.05f, _chargeToFireSeconds));
        PlayParticles(_activeChargeInstance);

        if (_chargeCleanupRoutine != null)
        {
            StopCoroutine(_chargeCleanupRoutine);
            _chargeCleanupRoutine = null;
        }

        float fallbackLifetime = Mathf.Max(_fallbackChargeLifetime, _chargeToFireSeconds + 0.25f);
        _chargeCleanupRoutine = StartCoroutine(DestroyAfterParticlesOrFallback(_activeChargeInstance, fallbackLifetime, isCharge: true));
    }

    private void StopChargeVfx()
    {
        if (_chargeCleanupRoutine != null)
        {
            StopCoroutine(_chargeCleanupRoutine);
            _chargeCleanupRoutine = null;
        }

        if (_activeChargeInstance != null)
            Destroy(_activeChargeInstance);

        _activeChargeInstance = null;
    }

    private void SpawnBurst()
    {
        ReleaseBurstInstance();

        if (_plasmaBurstPrefab == null || _firePoint == null)
            return;

        _activeBurstInstance = Instantiate(_plasmaBurstPrefab, _firePoint.position, GetBurstRotation());
        PlayParticles(_activeBurstInstance);

        if (_burstCleanupRoutine != null)
        {
            StopCoroutine(_burstCleanupRoutine);
            _burstCleanupRoutine = null;
        }

        _burstCleanupRoutine = StartCoroutine(DestroyAfterParticlesOrFallback(_activeBurstInstance, _fallbackBurstLifetime, isCharge: false));
    }

    private void UpdateChargeFollow()
    {
        if (!_followFirePointWhileCharging || _activeChargeInstance == null || _firePoint == null)
            return;

        _activeChargeInstance.transform.SetPositionAndRotation(_firePoint.position, GetChargeRotation());
    }

    private void ReleaseBurstInstance()
    {
        _activeBurstInstance = null;
    }

    private void FireProjectile()
    {
        if (_plasmaShotPrefab == null || _firePoint == null)
            return;

        Vector3 startPosition = _firePoint.position;
        Vector3 targetPosition = GetTargetPosition();
        Vector3 direction = targetPosition - startPosition;

        if (direction.sqrMagnitude < 0.0001f)
            direction = _firePoint.forward;

        direction.Normalize();

        Vector3 endPosition = ResolveShotPathEnd(startPosition, targetPosition, direction);
        Quaternion shotRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(_shotRotationOffsetEuler);
        GameObject shot = Instantiate(_plasmaShotPrefab, startPosition, shotRotation);

        if (_configureShotShapePath)
            ConfigureShotParticlePath(shot, startPosition, endPosition);

        TurretPlasmaProjectile projectile = shot.GetComponent<TurretPlasmaProjectile>();
        if (projectile == null)
            projectile = shot.AddComponent<TurretPlasmaProjectile>();

        projectile.Initialize(
            ownerRoot: transform,
            direction: direction,
            speed: _projectileSpeed,
            hitRadius: _projectileHitRadius,
            lifetime: _projectileLifetime,
            hitMask: _projectileHitMask,
            player: _player,
            deathManager: _deathManager,
            explosionPrefab: _explosionPrefab,
            destroyOnObstacleHit: _destroyProjectileOnObstacleHit,
            moveVisualRoot: !_keepShotVisualRootAtStart);
    }

    private Vector3 ResolveShotPathEnd(Vector3 startPosition, Vector3 targetPosition, Vector3 direction)
    {
        float targetDistance = Vector3.Distance(startPosition, targetPosition);
        float fallbackDistance = Mathf.Max(0.01f, _projectileSpeed * _projectileLifetime);
        float maxDistance = _useTargetAsShotPathEnd && targetDistance > 0.01f ? targetDistance : fallbackDistance;

        if (_shotPathMaxDistance > 0f)
            maxDistance = Mathf.Min(maxDistance, _shotPathMaxDistance);

        Vector3 endPosition = startPosition + direction * maxDistance;
        RaycastHit[] hits = Physics.SphereCastAll(startPosition, _projectileHitRadius, direction, maxDistance, _projectileHitMask, QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            Transform hitTransform = hitCollider.transform;
            if (hitTransform != null && hitTransform.IsChildOf(transform))
                continue;

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                endPosition = hits[i].point;
            }
        }

        return endPosition;
    }

    private void ConfigureShotParticlePath(GameObject shot, Vector3 startPosition, Vector3 endPosition)
    {
        if (shot == null)
            return;

        float distance = Mathf.Max(0.01f, Vector3.Distance(startPosition, endPosition));
        float scaledDistance = Mathf.Max(0.01f, distance * Mathf.Max(0.0001f, _shotShapeDistanceMultiplier));
        ParticleSystem[] particleSystems = shot.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            ParticleSystem.ShapeModule shape = ps.shape;
            if (shape.enabled)
            {
                if (_overrideShotShapePosition)
                    shape.position = _shotShapeLocalStart;

                if (_overrideShotShapeRotation)
                    shape.rotation = _shotShapeRotationOffsetEuler;

                if (_overrideShotShapeScale)
                    shape.scale = GetShotShapeScale(scaledDistance);

                if (_overrideShotShapeLength)
                    shape.length = scaledDistance;
            }

            if (_fitShotLifetimeToPathDistance)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startLifetime = Mathf.Max(0.05f, distance / Mathf.Max(0.01f, _projectileSpeed));
            }
        }
    }

    private Vector3 GetShotShapeScale(float distance)
    {
        Vector3 scale = _shotShapeBaseScale;

        switch (_shotShapeLengthAxis)
        {
            case ParticlePathAxis.X:
                scale.x = distance;
                break;
            case ParticlePathAxis.Y:
                scale.y = distance;
                break;
            default:
                scale.z = distance;
                break;
        }

        return scale;
    }

    private void ReturnToVigilant(bool playCloseAnimation)
    {
        _state = TurretState.Vigilant;
        _focusTimer = 0f;
        _noSightTimer = 0f;
        _detectTimer = 0f;
        _lineOfSightTimer = 0f;
        _fireQueued = false;
        _hasLineOfSight = false;
        _currentPitch = 0f;
        _scanDirection = Mathf.Approximately(_scanDirection, 0f) ? 1f : _scanDirection;

        StopChargeVfx();

        if (_pitchPivot != null)
            _pitchPivot.localRotation = _basePitchLocalRotation;

        if (playCloseAnimation)
            PlayState(_closeStateName, _closeCrossFade);
    }

    private void CancelPendingShotAndReturnToVigilant()
    {
        if (_fireRoutine != null)
        {
            StopCoroutine(_fireRoutine);
            _fireRoutine = null;
        }

        ReturnToVigilant(true);
    }

    private void UpdateScanRotation()
    {
        _currentScanYaw += _scanDirection * _scanYawSpeed * Time.deltaTime;

        if (_currentScanYaw >= _scanMaxYaw)
        {
            _currentScanYaw = _scanMaxYaw;
            _scanDirection = -1f;
        }
        else if (_currentScanYaw <= _scanMinYaw)
        {
            _currentScanYaw = _scanMinYaw;
            _scanDirection = 1f;
        }

        if (_yawRoot != null)
            _yawRoot.rotation = Quaternion.Euler(0f, _baseYawWorld + _currentScanYaw, 0f);
    }

    private void UpdateAimTowardsPlayer()
    {
        if (_player == null || _yawRoot == null)
            return;

        Vector3 targetPosition = GetTargetPosition();
        Vector3 flatDirection = targetPosition - _yawRoot.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredYaw = GetDesiredFocusYaw(flatDirection.normalized);
            _yawRoot.rotation = Quaternion.RotateTowards(_yawRoot.rotation, desiredYaw, _focusYawSpeed * Time.deltaTime);
        }

        UpdatePitchTowards(targetPosition);
    }

    private Quaternion GetDesiredFocusYaw(Vector3 flatDirectionNormalized)
    {
        return Quaternion.LookRotation(flatDirectionNormalized, Vector3.up) * Quaternion.Euler(0f, _focusYawVisualOffset, 0f);
    }

    private void UpdatePitchTowards(Vector3 targetPosition)
    {
        if (_pitchPivot == null || _pitchPivot.parent == null)
            return;

        Vector3 worldDirection = (targetPosition - _pitchPivot.position).normalized;
        if (worldDirection.sqrMagnitude < 0.0001f)
            return;

        Vector3 directionInParentSpace = _pitchPivot.parent.InverseTransformDirection(worldDirection);
        Vector3 aimInBaseSpace = Quaternion.Inverse(_basePitchLocalRotation) * directionInParentSpace;

        Vector3 barrelForward = _barrelForwardLocal.sqrMagnitude > 0.0001f ? _barrelForwardLocal.normalized : Vector3.up;
        Vector3 pitchAxis = _pitchAxisLocal.sqrMagnitude > 0.0001f ? _pitchAxisLocal.normalized : Vector3.right;

        Vector3 aimFlat = Vector3.ProjectOnPlane(aimInBaseSpace, pitchAxis);
        if (aimFlat.sqrMagnitude < 0.0001f)
            return;

        float desiredPitch = Vector3.SignedAngle(barrelForward, aimFlat.normalized, pitchAxis);
        desiredPitch = Mathf.Clamp(desiredPitch, _minPitch, _maxPitch);
        _currentPitch = Mathf.MoveTowards(_currentPitch, desiredPitch, _focusPitchSpeed * Time.deltaTime);

        _pitchPivot.localRotation = _basePitchLocalRotation * Quaternion.AngleAxis(_currentPitch, pitchAxis);
    }

    private void UpdateLineOfSight()
    {
        _lineOfSightTimer += Time.deltaTime;
        if (_lineOfSightTimer < _lineOfSightInterval)
            return;

        _lineOfSightTimer = 0f;
        _hasLineOfSight = CheckLineOfSight();
    }

    private bool IsPlayerInsideDetectionZone()
    {
        if (_player == null)
            return false;

        Vector3 targetPosition = GetTargetPosition();
        Vector3 toTarget = targetPosition - transform.position;
        float sqrDistance = toTarget.sqrMagnitude;
        if (sqrDistance > _detectionRange * _detectionRange)
            return false;

        if (_useUpperHemisphereDetection && Vector3.Dot(toTarget, Vector3.up) < 0f)
            return false;

        if (!IsInsideBroadScanSector(toTarget))
            return false;

        return IsInsideCurrentLookZone(targetPosition);
    }

    private bool IsInsideBroadScanSector(Vector3 toTarget)
    {
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        if (flatToTarget.sqrMagnitude < 0.0001f)
            return true;

        float signedYaw = Vector3.SignedAngle(YawBaseForward(), flatToTarget.normalized, Vector3.up);
        return signedYaw >= _scanMinYaw && signedYaw <= _scanMaxYaw;
    }

    private bool IsInsideCurrentLookZone(Vector3 targetPosition)
    {
        Transform originTransform = _yawRoot != null ? _yawRoot : transform;
        Vector3 toTarget = targetPosition - originTransform.position;
        float maxRange = _lookZoneRange > 0f ? _lookZoneRange : _detectionRange;

        if (toTarget.sqrMagnitude > maxRange * maxRange)
            return false;

        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        if (flatToTarget.sqrMagnitude < 0.0001f)
            return true;

        Vector3 lookForward = CurrentVisualLookForward();
        Vector3 flatLookForward = Vector3.ProjectOnPlane(lookForward, Vector3.up);

        if (flatLookForward.sqrMagnitude < 0.0001f)
            flatLookForward = YawBaseForward();

        float lookAngle = Vector3.Angle(flatLookForward.normalized, flatToTarget.normalized);
        return lookAngle <= _lookZoneHalfAngle;
    }

    private Vector3 CurrentVisualLookForward()
    {
        Transform source = _yawRoot != null ? _yawRoot : transform;
        return (source.rotation * Quaternion.Euler(0f, -_focusYawVisualOffset, 0f)) * Vector3.forward;
    }

    private Vector3 YawBaseForward()
    {
        return Quaternion.Euler(0f, _baseYawWorld - _focusYawVisualOffset, 0f) * Vector3.forward;
    }

    private bool CheckLineOfSight()
    {
        if (_player == null)
            return false;

        Vector3 origin = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 1.25f;
        Vector3 target = GetTargetPosition();
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance < 0.0001f)
            return true;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction.normalized,
            _lineOfSightHits,
            distance,
            _lineOfSightMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
            return true;

        float nearestDistance = float.MaxValue;
        Transform nearestTransform = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _lineOfSightHits[i].collider;
            if (hitCollider == null)
                continue;

            Transform hitTransform = hitCollider.transform;
            if (hitTransform.IsChildOf(transform))
                continue;

            float hitDistance = _lineOfSightHits[i].distance;
            if (hitDistance < nearestDistance)
            {
                nearestDistance = hitDistance;
                nearestTransform = hitTransform;
            }
        }

        if (nearestTransform == null)
            return true;

        return nearestTransform.GetComponentInParent<RobotController>() != null;
    }

    private Vector3 GetTargetPosition()
    {
        return _player.transform.position + _targetOffset;
    }

    private Quaternion GetChargeRotation()
    {
        if (_firePoint == null)
            return Quaternion.Euler(_chargeRotationOffsetEuler);

        return _firePoint.rotation * Quaternion.Euler(_chargeRotationOffsetEuler);
    }

    private Quaternion GetBurstRotation()
    {
        if (_firePoint == null)
            return Quaternion.Euler(_burstRotationOffsetEuler);

        return _firePoint.rotation * Quaternion.Euler(_burstRotationOffsetEuler);
    }

    private void PlayState(string stateName, float crossFadeDuration)
    {
        if (_bodyAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        _bodyAnimator.CrossFadeInFixedTime(stateName, crossFadeDuration);
    }

    private void PlayParticles(GameObject obj)
    {
        if (obj == null)
            return;

        ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
            particleSystems[i].Play(true);
    }

    private void AdjustNonLoopParticleLifetimes(GameObject obj, float lifetime)
    {
        if (obj == null)
            return;

        ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            var main = ps.main;
            if (!main.loop)
                main.startLifetime = lifetime;
        }
    }

    private IEnumerator DestroyAfterParticlesOrFallback(GameObject obj, float fallbackSeconds, bool isCharge)
    {
        if (obj == null)
            yield break;

        ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>(true);
        float elapsed = 0f;

        while (obj != null)
        {
            bool anyAlive = false;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                if (ps != null && ps.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive && elapsed > 0.05f)
                break;

            elapsed += Time.deltaTime;
            if (fallbackSeconds > 0f && elapsed >= fallbackSeconds)
                break;

            yield return null;
        }

        if (obj != null)
            Destroy(obj);

        if (isCharge)
        {
            if (_activeChargeInstance == obj)
                _activeChargeInstance = null;
            _chargeCleanupRoutine = null;
        }
        else
        {
            if (_activeBurstInstance == obj)
                _activeBurstInstance = null;
            _burstCleanupRoutine = null;
        }
    }

    private void AutoResolveReferences()
    {
        if (_yawRoot == null)
            _yawRoot = transform;

        if (_visualRoot == null)
        {
            Transform body = transform.Find("Body");
            if (body != null)
                _visualRoot = body;
        }

        if (_bodyAnimator == null && _visualRoot != null)
            _bodyAnimator = _visualRoot.GetComponent<Animator>();

        if (_pitchPivot == null)
        {
            Transform resolved = transform.Find("Body/Armature.001/Bone.001");
            if (resolved != null)
                _pitchPivot = resolved;
        }

        if (_firePoint == null)
        {
            Transform resolved = transform.Find("Body/Armature.001/Bone.001/Bone.001_end");
            if (resolved != null)
                _firePoint = resolved;
        }
    }

    public void ResetLevelState()
    {
        if (_fireRoutine != null)
        {
            StopCoroutine(_fireRoutine);
            _fireRoutine = null;
        }

        if (_burstCleanupRoutine != null)
        {
            StopCoroutine(_burstCleanupRoutine);
            _burstCleanupRoutine = null;
        }

        if (_chargeCleanupRoutine != null)
        {
            StopCoroutine(_chargeCleanupRoutine);
            _chargeCleanupRoutine = null;
        }

        if (_alertTween != null && _alertTween.IsActive())
            _alertTween.Kill();

        if (_activeBurstInstance != null)
            Destroy(_activeBurstInstance);

        if (_activeChargeInstance != null)
            Destroy(_activeChargeInstance);

        _activeBurstInstance = null;
        _activeChargeInstance = null;
        _state = TurretState.Vigilant;
        _focusTimer = 0f;
        _noSightTimer = 0f;
        _detectTimer = 0f;
        _lineOfSightTimer = 0f;
        _openingTimer = 0f;
        _fireQueued = false;
        _hasLineOfSight = false;
        _currentPitch = 0f;
        _currentScanYaw = 0f;
        _scanDirection = 1f;

        if (_yawRoot != null)
            _yawRoot.rotation = Quaternion.Euler(0f, _baseYawWorld, 0f);

        if (_pitchPivot != null)
            _pitchPivot.localRotation = _basePitchLocalRotation;

        if (_visualRoot != null)
            _visualRoot.localScale = _baseVisualScale;

        if (_alertSprite != null)
        {
            Color c = _alertSpriteBaseColor;
            c.a = 0f;
            _alertSprite.color = c;
        }

        PlayState(_closeStateName, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        float baseYaw = Application.isPlaying ? _baseYawWorld : transform.eulerAngles.y;
        Vector3 baseForward = Quaternion.Euler(0f, baseYaw - _focusYawVisualOffset, 0f) * Vector3.forward;

        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.8f);
        DrawHorizontalRay(origin, baseForward, _scanMinYaw, _detectionRange);
        DrawHorizontalRay(origin, baseForward, _scanMaxYaw, _detectionRange);
        DrawHorizontalArc(origin, baseForward, _scanMinYaw, _scanMaxYaw, _detectionRange, 24);

        if (_useUpperHemisphereDetection)
        {
            Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.25f);
            DrawUpperHemisphere(origin, _detectionRange, 16);
        }

        Vector3 currentLookForward = Application.isPlaying && _yawRoot != null ? CurrentVisualLookForward() : baseForward;

        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.9f);
        float lookRange = _lookZoneRange > 0f ? _lookZoneRange : _detectionRange;
        DrawHorizontalRay(origin, currentLookForward, -_lookZoneHalfAngle, lookRange);
        DrawHorizontalRay(origin, currentLookForward, _lookZoneHalfAngle, lookRange);
        DrawHorizontalArc(origin, currentLookForward, -_lookZoneHalfAngle, _lookZoneHalfAngle, lookRange, 18);

        if (_firePoint != null && _player != null)
        {
            Gizmos.color = _hasLineOfSight ? Color.green : Color.red;
            Gizmos.DrawLine(_firePoint.position, GetTargetPosition());
        }
    }

    private void DrawHorizontalRay(Vector3 origin, Vector3 forward, float yawOffset, float length)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;

        Vector3 direction = Quaternion.Euler(0f, yawOffset, 0f) * flatForward.normalized;
        Gizmos.DrawLine(origin, origin + direction * length);
    }

    private void DrawHorizontalArc(Vector3 origin, Vector3 forward, float minAngle, float maxAngle, float radius, int segments)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;

        Vector3 previous = origin + (Quaternion.Euler(0f, minAngle, 0f) * flatForward.normalized) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(minAngle, maxAngle, t);
            Vector3 next = origin + (Quaternion.Euler(0f, angle, 0f) * flatForward.normalized) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    private void DrawUpperHemisphere(Vector3 origin, float radius, int segments)
    {
        DrawUpperArc(origin, Vector3.forward, Vector3.up, radius, segments);
        DrawUpperArc(origin, Vector3.right, Vector3.up, radius, segments);
        DrawUpperArc(origin, (Vector3.forward + Vector3.right).normalized, Vector3.up, radius, segments);
        DrawUpperArc(origin, (Vector3.forward - Vector3.right).normalized, Vector3.up, radius, segments);
    }

    private void DrawUpperArc(Vector3 origin, Vector3 horizontalDirection, Vector3 verticalDirection, float radius, int segments)
    {
        Vector3 previous = origin + horizontalDirection.normalized * radius;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(0f, 180f, t);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.Cross(horizontalDirection, verticalDirection).normalized) * horizontalDirection.normalized;
            Vector3 next = origin + direction * radius;
            if (previous.y >= origin.y - 0.001f && next.y >= origin.y - 0.001f)
                Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

}
