using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RobotShockController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotController _robotController;
    [SerializeField] private Transform _shockCenter;
    [SerializeField] private GameObject _shockEffectObject;
    [SerializeField] private ParticleSystem[] _shockParticles;

    [Header("Input")]
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private string _gameplayMapName = "Gameplay";
    [SerializeField] private string _shockActionName = "Shock";

    [Header("Shock Area")]
    [SerializeField] private float _shockRadius = 3.5f;
    [SerializeField] private LayerMask _shockableMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField] private bool _requireLineOfSight;
    [SerializeField] private LayerMask _lineOfSightMask = ~0;

    [Header("Timing")]
    [SerializeField] private float _cooldownSeconds = 2f;
    [SerializeField] private float _expandDuration = 0.35f;
    [SerializeField] private bool _useParticleDuration = true;
    [SerializeField] private float _activeDurationFallback = 1.25f;

    [Header("Effect Scale")]
    [SerializeField] private bool _scaleEffectWithRadius = true;
    [SerializeField] [Range(0f, 1f)] private float _startScale01 = 0.05f;
    [SerializeField] private float _radiusScaleMultiplier = 1f;
    [SerializeField] private bool _deactivateEffectOnAwake = true;
    [SerializeField] private bool _clearParticlesBeforePlay = true;
    [SerializeField] private bool _disableCartoonFxAutoDestroy = true;

    [Header("Debug")]
    [SerializeField] private bool _isShocking;
    [SerializeField] private float _cooldownTimer;
    [SerializeField] private float _shockTimer;
    [SerializeField] private float _currentActiveDuration;
    [SerializeField] private int _lastShockHitCount;

    private const int MaxHits = 96;

    private readonly Collider[] _hits = new Collider[MaxHits];
    private readonly HashSet<MonoBehaviour> _processedShockables = new HashSet<MonoBehaviour>();

    private InputActionMap _gameplayMap;
    private InputAction _shockAction;
    private Vector3 _effectOriginalLocalScale = Vector3.one;
    private bool _hasStoredEffectScale;

    public bool IsShocking => _isShocking;
    public bool IsOnCooldown => _cooldownTimer > 0f;
    public float Cooldown01 => _cooldownSeconds <= 0f ? 0f : Mathf.Clamp01(_cooldownTimer / _cooldownSeconds);

    private void Reset()
    {
        _robotController = GetComponent<RobotController>();
        _shockCenter = transform;
    }

    private void Awake()
    {
        if (_robotController == null)
            _robotController = GetComponent<RobotController>();

        if (_shockCenter == null)
            _shockCenter = transform;

        if (_shockEffectObject == null)
            _shockEffectObject = FindChildGameObjectByName(transform, "CFXR Electrified 3");

        CacheEffectReferences();
        StoreEffectScale();

        if (_deactivateEffectOnAwake && _shockEffectObject != null)
            _shockEffectObject.SetActive(false);
    }

    private void OnEnable()
    {
        StartInputActions();
    }

    private void OnDisable()
    {
        if (_shockEffectObject != null)
            _shockEffectObject.SetActive(false);

        _isShocking = false;
    }

    private void Update()
    {
        UpdateCooldown();
        HandleInput();
        UpdateShockEffect();
    }

    private void StartInputActions()
    {
        if (_inputActions == null)
            return;

        _gameplayMap = _inputActions.FindActionMap(_gameplayMapName, false);
        if (_gameplayMap == null)
            return;

        _shockAction = _gameplayMap.FindAction(_shockActionName, false);

        if (!_gameplayMap.enabled)
            _gameplayMap.Enable();
    }

    private void HandleInput()
    {
        if (_shockAction == null || !_shockAction.WasPressedThisFrame())
            return;

        TryShock();
    }

    public bool TryShock()
    {
        if (_cooldownTimer > 0f)
            return false;

        BeginShock();
        return true;
    }

    private void BeginShock()
    {
        _cooldownTimer = Mathf.Max(0f, _cooldownSeconds);
        _shockTimer = 0f;
        _isShocking = true;
        _currentActiveDuration = GetEffectActiveDuration();

        PlayShockEffect();
        ApplyShockOnce();
    }

    private void UpdateCooldown()
    {
        if (_cooldownTimer <= 0f)
            return;

        _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
    }

    private void UpdateShockEffect()
    {
        if (!_isShocking)
            return;

        _shockTimer += Time.deltaTime;
        UpdateEffectScale();

        if (_shockTimer < _currentActiveDuration)
            return;

        _isShocking = false;

        if (_shockEffectObject != null)
            _shockEffectObject.SetActive(false);
    }

    private void PlayShockEffect()
    {
        CacheEffectReferences();
        StoreEffectScale();
        DisableCartoonFxAutoDestroyIfNeeded();

        if (_shockEffectObject != null)
        {
            _shockEffectObject.SetActive(true);
            SetEffectScale(_startScale01);
        }

        if (_shockParticles == null)
            return;

        for (int i = 0; i < _shockParticles.Length; i++)
        {
            ParticleSystem particle = _shockParticles[i];
            if (particle == null)
                continue;

            if (_clearParticlesBeforePlay)
                particle.Clear(true);

            particle.Play(true);
        }
    }

    private void ApplyShockOnce()
    {
        _lastShockHitCount = 0;
        _processedShockables.Clear();

        Vector3 center = GetShockCenter();
        int hitCount = Physics.OverlapSphereNonAlloc(center, _shockRadius, _hits, _shockableMask, _triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hits[i];
            if (hit == null)
                continue;

            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>(true);
            for (int j = 0; j < behaviours.Length; j++)
            {
                MonoBehaviour behaviour = behaviours[j];
                if (behaviour == null || !(behaviour is IShockable shockable))
                    continue;

                if (_processedShockables.Contains(behaviour))
                    continue;

                Vector3 targetPoint = GetTargetPoint(hit, behaviour.transform);
                if (_requireLineOfSight && IsLineOfSightBlocked(center, targetPoint, behaviour.transform))
                    continue;

                Vector3 direction = targetPoint - center;
                float distance = direction.magnitude;
                Vector3 normal = distance > 0.0001f ? direction / distance : Vector3.zero;
                ShockInfo info = new ShockInfo(gameObject, transform, center, _shockRadius, distance, normal);

                _processedShockables.Add(behaviour);
                shockable.OnShock(info);
                _lastShockHitCount++;
            }
        }
    }

    private Vector3 GetShockCenter()
    {
        return _shockCenter != null ? _shockCenter.position : transform.position;
    }

    private Vector3 GetTargetPoint(Collider hit, Transform fallback)
    {
        Vector3 center = GetShockCenter();
        Vector3 point = hit.ClosestPoint(center);

        if ((point - center).sqrMagnitude > 0.0001f)
            return point;

        if (hit.attachedRigidbody != null)
            return hit.attachedRigidbody.worldCenterOfMass;

        if (fallback != null)
            return fallback.position;

        return hit.bounds.center;
    }

    private bool IsLineOfSightBlocked(Vector3 start, Vector3 end, Transform targetRoot)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.0001f)
            return false;

        if (!Physics.Raycast(start, direction / distance, out RaycastHit rayHit, distance, _lineOfSightMask, QueryTriggerInteraction.Ignore))
            return false;

        if (targetRoot != null && rayHit.collider.transform.IsChildOf(targetRoot))
            return false;

        return true;
    }

    private void UpdateEffectScale()
    {
        if (_shockEffectObject == null)
            return;

        if (_expandDuration <= 0f)
        {
            SetEffectScale(1f);
            return;
        }

        float t = Mathf.Clamp01(_shockTimer / _expandDuration);
        t = 1f - ((1f - t) * (1f - t));
        SetEffectScale(Mathf.Lerp(_startScale01, 1f, t));
    }

    private void SetEffectScale(float scale01)
    {
        if (_shockEffectObject == null)
            return;

        if (!_scaleEffectWithRadius)
        {
            _shockEffectObject.transform.localScale = _effectOriginalLocalScale * Mathf.Max(0f, scale01);
            return;
        }

        float radiusScale = Mathf.Max(0.0001f, _shockRadius * _radiusScaleMultiplier);
        _shockEffectObject.transform.localScale = _effectOriginalLocalScale * radiusScale * Mathf.Max(0f, scale01);
    }

    private float GetEffectActiveDuration()
    {
        if (!_useParticleDuration || _shockParticles == null || _shockParticles.Length == 0)
            return Mathf.Max(0.01f, _activeDurationFallback);

        float duration = 0f;

        for (int i = 0; i < _shockParticles.Length; i++)
        {
            ParticleSystem particle = _shockParticles[i];
            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;
            float particleDuration = main.duration + GetMaxStartLifetime(main);
            duration = Mathf.Max(duration, particleDuration);
        }

        if (duration <= 0f)
            duration = _activeDurationFallback;

        return Mathf.Max(0.01f, duration);
    }

    private float GetMaxStartLifetime(ParticleSystem.MainModule main)
    {
        ParticleSystem.MinMaxCurve lifetime = main.startLifetime;

        switch (lifetime.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return lifetime.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return lifetime.constantMax;
            case ParticleSystemCurveMode.Curve:
                return lifetime.curveMultiplier;
            case ParticleSystemCurveMode.TwoCurves:
                return lifetime.curveMultiplier;
            default:
                return 0f;
        }
    }

    private void CacheEffectReferences()
    {
        if (_shockEffectObject != null && (_shockParticles == null || _shockParticles.Length == 0))
            _shockParticles = _shockEffectObject.GetComponentsInChildren<ParticleSystem>(true);
    }

    private void StoreEffectScale()
    {
        if (_hasStoredEffectScale || _shockEffectObject == null)
            return;

        _effectOriginalLocalScale = _shockEffectObject.transform.localScale;
        _hasStoredEffectScale = true;
    }

    private void DisableCartoonFxAutoDestroyIfNeeded()
    {
        if (!_disableCartoonFxAutoDestroy || _shockEffectObject == null)
            return;

        MonoBehaviour[] behaviours = _shockEffectObject.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            System.Type behaviourType = behaviour.GetType();
            if (behaviourType.FullName != "CartoonFX.CFXR_Effect")
                continue;

            System.Reflection.FieldInfo clearBehaviorField = behaviourType.GetField(
                "clearBehavior",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
            );

            if (clearBehaviorField == null || !clearBehaviorField.FieldType.IsEnum)
                continue;

            object noneValue = System.Enum.ToObject(clearBehaviorField.FieldType, 0);
            clearBehaviorField.SetValue(behaviour, noneValue);
        }
    }

    private GameObject FindChildGameObjectByName(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
            return null;

        if (root.name == objectName)
            return root.gameObject;

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject found = FindChildGameObjectByName(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Transform centerTransform = _shockCenter != null ? _shockCenter : transform;
        Gizmos.DrawWireSphere(centerTransform.position, _shockRadius);
    }
}
