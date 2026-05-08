using System.Collections;
using UnityEngine;

public class WaterShooter : MonoBehaviour, ILevelResettable
{
    [Header("Scene References")]
    [SerializeField] private RobotController _player;
    [SerializeField] private Transform _beamRoot;
    [SerializeField] private Transform _pushPoint;
    [SerializeField] private GameObject _warningVfxRoot;
    [SerializeField] private GameObject _bubblesRoot;

    [Header("Cycle")]
    [SerializeField] private bool _startOnEnable = true;
    [SerializeField] private float _startDelay;
    [SerializeField] private int _warningRepeats = 3;
    [SerializeField] private float _warningFallbackDuration = 0.5f;
    [SerializeField] private float _warningPause = 0.25f;
    [SerializeField] private float _postShotPause = 1f;
    [SerializeField] private bool _resetCycleOnLevelReset = true;

    [Header("Push Area")]
    [SerializeField] private float _pushLength = 8f;
    [SerializeField] private float _pushRadius = 0.85f;
    [SerializeField] private Vector3 _beamForwardLocal = Vector3.forward;
    [SerializeField] private Vector3 _pushOriginLocalOffset;
    [SerializeField] private LayerMask _pushMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Push Force")]
    [SerializeField] private float _initialImpulse = 8f;
    [SerializeField] private float _continuousAcceleration = 22f;
    [SerializeField] private float _maxExternalSpeed = 18f;
    [SerializeField] private float _entryImpulseCooldown = 0.15f;
    [SerializeField] private bool _dampenExternalVelocityWhenBeamEnds = true;
    [SerializeField, Range(0f, 1f)] private float _endVelocityMultiplier = 0.65f;
    [SerializeField] private float _postBeamExtraDrag = 35f;
    [SerializeField] private float _postBeamExtraDragDuration = 0.75f;
    [SerializeField] private AnimationCurve _forceOverBeamLifetime = new AnimationCurve(
        new Keyframe(0f, 0.15f),
        new Keyframe(0.35f, 0.55f),
        new Keyframe(1f, 1f)
    );

    [Header("VFX")]
    [SerializeField] private bool _disableBeamAtStart = true;
    [SerializeField] private bool _playBubblesWithBeam = true;
    [SerializeField] private bool _clearParticlesOnStop = true;

    [Header("Debug")]
    [SerializeField] private bool _isFiring;
    [SerializeField] private bool _playerInsidePush;
    [SerializeField] private float _beamDuration;
    [SerializeField] private float _beamElapsed;
    [SerializeField] private float _currentForce01;

    private readonly Collider[] _overlapHits = new Collider[24];
    private Coroutine _cycleRoutine;
    private bool _wasPlayerInsidePush;
    private float _lastEntryImpulseTime = -999f;
    private RobotController _lastPushedRobot;

    private void Awake()
    {
        AutoResolveReferences();
    }

    private void OnEnable()
    {
        AutoResolveReferences();
        ConfigureInitialVfxState();

        if (_startOnEnable)
            RestartCycle();
    }

    private void OnDisable()
    {
        StopCycleRoutine();
        _isFiring = false;
        _playerInsidePush = false;
        _wasPlayerInsidePush = false;
    }

    private void RestartCycle()
    {
        StopCycleRoutine();
        _cycleRoutine = StartCoroutine(ShootCycle());
    }

    private void StopCycleRoutine()
    {
        if (_cycleRoutine == null)
            return;

        StopCoroutine(_cycleRoutine);
        _cycleRoutine = null;
    }

    private IEnumerator ShootCycle()
    {
        if (_startDelay > 0f)
            yield return new WaitForSeconds(_startDelay);

        while (isActiveAndEnabled)
        {
            yield return PlayWarnings();
            yield return FireBeam();

            if (_postShotPause > 0f)
                yield return new WaitForSeconds(_postShotPause);
        }
    }

    private IEnumerator PlayWarnings()
    {
        int repeats = Mathf.Max(0, _warningRepeats);

        for (int i = 0; i < repeats; i++)
        {
            float duration = GetParticleRootDuration(_warningVfxRoot, _warningFallbackDuration);

            if (_warningVfxRoot != null)
                PlayParticleRoot(_warningVfxRoot, true);

            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            if (_warningVfxRoot != null)
                StopParticleRoot(_warningVfxRoot, _clearParticlesOnStop);

            if (_warningPause > 0f)
                yield return new WaitForSeconds(_warningPause);
        }
    }

    private IEnumerator FireBeam()
    {
        _isFiring = true;
        _beamElapsed = 0f;
        _beamDuration = GetBeamDuration();
        _lastEntryImpulseTime = -999f;
        _wasPlayerInsidePush = false;
        _lastPushedRobot = null;

        GameObject beamObject = _beamRoot != null ? _beamRoot.gameObject : null;
        PlayParticleRoot(beamObject, true);

        if (_playBubblesWithBeam)
            PlayParticleRoot(_bubblesRoot, true);

        while (_beamElapsed < _beamDuration)
        {
            float t = _beamDuration > 0.0001f ? Mathf.Clamp01(_beamElapsed / _beamDuration) : 1f;
            _currentForce01 = _forceOverBeamLifetime != null ? Mathf.Max(0f, _forceOverBeamLifetime.Evaluate(t)) : 1f;
            UpdatePush();
            _beamElapsed += Time.deltaTime;
            yield return null;
        }

        StopParticleRoot(beamObject, _clearParticlesOnStop);

        if (_playBubblesWithBeam)
            StopParticleRoot(_bubblesRoot, _clearParticlesOnStop);

        DampenExternalVelocityAfterBeam();

        _isFiring = false;
        _playerInsidePush = false;
        _wasPlayerInsidePush = false;
        _currentForce01 = 0f;
    }

    private void UpdatePush()
    {
        _playerInsidePush = false;

        if (_player == null)
            _player = FindObjectOfType<RobotController>();

        if (_player == null)
        {
            _wasPlayerInsidePush = false;
            return;
        }

        Vector3 start = GetPushOrigin();
        Vector3 forward = GetHorizontalBeamForward();
        Vector3 end = start + forward * Mathf.Max(0.01f, _pushLength);

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            start,
            end,
            Mathf.Max(0.01f, _pushRadius),
            _overlapHits,
            _pushMask,
            _triggerInteraction
        );

        RobotController robot = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapHits[i];
            if (hit == null)
                continue;

            robot = hit.GetComponentInParent<RobotController>();
            if (robot != null)
                break;
        }

        if (robot == null)
        {
            _wasPlayerInsidePush = false;
            return;
        }

        _playerInsidePush = true;
        _lastPushedRobot = robot;

        if (!_wasPlayerInsidePush && Time.time >= _lastEntryImpulseTime + _entryImpulseCooldown)
        {
            robot.AddExternalPlanarImpulse(forward * _initialImpulse * _currentForce01, _maxExternalSpeed);
            _lastEntryImpulseTime = Time.time;
        }

        robot.AddExternalPlanarAcceleration(forward * _continuousAcceleration * _currentForce01, _maxExternalSpeed);
        _wasPlayerInsidePush = true;
    }

    private Vector3 GetPushOrigin()
    {
        Transform source = GetSourceTransform();
        return source.TransformPoint(_pushOriginLocalOffset);
    }

    private Vector3 GetHorizontalBeamForward()
    {
        Transform source = GetSourceTransform();
        Vector3 localForward = _beamForwardLocal.sqrMagnitude > 0.0001f ? _beamForwardLocal.normalized : Vector3.forward;
        Vector3 forward = source.TransformDirection(localForward);
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = source.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private Transform GetSourceTransform()
    {
        if (_pushPoint != null)
            return _pushPoint;

        if (_beamRoot != null)
            return _beamRoot;

        return transform;
    }

    private float GetBeamDuration()
    {
        GameObject beamObject = _beamRoot != null ? _beamRoot.gameObject : null;
        return GetParticleRootEmissionDuration(beamObject, 1f);
    }

    private float GetParticleRootEmissionDuration(GameObject root, float fallback)
    {
        if (root == null)
            return Mathf.Max(0.01f, fallback);

        float maxDuration = 0f;
        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;
            float simulationSpeed = Mathf.Max(0.01f, main.simulationSpeed);
            float duration = main.duration + GetMinMaxCurveMax(main.startDelay);
            maxDuration = Mathf.Max(maxDuration, duration / simulationSpeed);
        }

        return Mathf.Max(0.01f, maxDuration > 0f ? maxDuration : fallback);
    }

    private float GetParticleRootDuration(GameObject root, float fallback)
    {
        if (root == null)
            return Mathf.Max(0.01f, fallback);

        float maxDuration = 0f;
        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;
            float simulationSpeed = Mathf.Max(0.01f, main.simulationSpeed);
            float duration = main.duration + GetMinMaxCurveMax(main.startDelay);

            if (!main.loop)
                duration += GetMinMaxCurveMax(main.startLifetime);

            maxDuration = Mathf.Max(maxDuration, duration / simulationSpeed);
        }

        return Mathf.Max(0.01f, maxDuration > 0f ? maxDuration : fallback);
    }

    private float GetMinMaxCurveMax(ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;

            case ParticleSystemCurveMode.TwoConstants:
                return curve.constantMax;

            case ParticleSystemCurveMode.Curve:
                return curve.curveMultiplier;

            case ParticleSystemCurveMode.TwoCurves:
                return curve.curveMultiplier;

            default:
                return 0f;
        }
    }

    private void DampenExternalVelocityAfterBeam()
    {
        if (!_dampenExternalVelocityWhenBeamEnds || _lastPushedRobot == null)
            return;

        _lastPushedRobot.ScaleExternalPlanarVelocity(_endVelocityMultiplier);
        _lastPushedRobot.AddTemporaryExternalPlanarDrag(_postBeamExtraDrag, _postBeamExtraDragDuration);
    }

    private void PlayParticleRoot(GameObject root, bool clearFirst)
    {
        if (root == null)
            return;

        if (!root.activeSelf)
            root.SetActive(true);

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            if (clearFirst)
                particle.Clear(true);

            particle.Play(true);
        }
    }

    private void StopParticleRoot(GameObject root, bool clear)
    {
        if (root == null)
            return;

        ParticleSystemStopBehavior stopBehavior = clear
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            particle.Stop(true, stopBehavior);
        }
    }

    private void ConfigureInitialVfxState()
    {
        if (!_disableBeamAtStart)
            return;

        GameObject beamObject = _beamRoot != null ? _beamRoot.gameObject : null;
        StopParticleRoot(beamObject, true);
        StopParticleRoot(_bubblesRoot, true);
        StopParticleRoot(_warningVfxRoot, true);
    }

    private void AutoResolveReferences()
    {
        if (_player == null)
            _player = FindObjectOfType<RobotController>();

        if (_beamRoot == null)
        {
            Transform resolved = FindChildRecursive(transform, "Water_Beam");
            if (resolved != null)
                _beamRoot = resolved;
        }

        if (_bubblesRoot == null)
        {
            Transform resolved = FindChildRecursive(transform, "Bubbles_Vertical_Loop");
            if (resolved != null)
                _bubblesRoot = resolved.gameObject;
        }
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    public void ResetLevelState()
    {
        _playerInsidePush = false;
        _wasPlayerInsidePush = false;
        _lastEntryImpulseTime = -999f;
        _lastPushedRobot = null;

        if (_player != null)
            _player.ClearExternalPlanarVelocity();

        if (!_resetCycleOnLevelReset || !isActiveAndEnabled)
            return;

        StopParticleRoot(_beamRoot != null ? _beamRoot.gameObject : null, true);
        StopParticleRoot(_bubblesRoot, true);
        StopParticleRoot(_warningVfxRoot, true);
        RestartCycle();
    }

    private void OnDrawGizmosSelected()
    {
        AutoResolveReferences();

        Vector3 start = GetPushOrigin();
        Vector3 forward = GetHorizontalBeamForward();
        Vector3 end = start + forward * Mathf.Max(0.01f, _pushLength);

        Gizmos.color = _playerInsidePush ? Color.cyan : new Color(0.1f, 0.65f, 1f, 0.9f);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, _pushRadius);
        Gizmos.DrawWireSphere(end, _pushRadius);
    }
}
