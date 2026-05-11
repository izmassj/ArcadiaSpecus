using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(75)]
[DisallowMultipleComponent]
[RequireComponent(typeof(ElectricNode))]
public class ElectricCentralBulb : MonoBehaviour
{
    public enum BarrierControlMode
    {
        NetworkPowered = 0,
        AnySmallBulbSendingPower = 1
    }

    [Header("Barrier")]
    [SerializeField] private ElectricBarrier _barrier;
    [SerializeField] private bool _autoFindBarrier = true;
    [SerializeField] private string[] _barrierNames = { "Barrier", "Barrera", "ElectricBarrier", "Electric Barrier" };
    [SerializeField] private bool _activateBarrierWhenPowered = true;
    [SerializeField] private BarrierControlMode _barrierControlMode = BarrierControlMode.AnySmallBulbSendingPower;

    [Header("Startup")]
    [SerializeField] private bool _doNotForceBarrierOffBeforeFirstNetworkUpdate = true;
    [SerializeField] private float _firstUpdateDelay = 0.05f;
    [SerializeField] private float _startupDelayBeforeFirstApply = 0.05f;

    [Header("Small Bulb Channelizer")]
    [SerializeField] private bool _autoFindSmallBulbs = true;
    [SerializeField] private string[] _smallBulbNameParts = { "SmallLightbulb", "Bombilla_peque", "Bombilla pequeña" };
    [SerializeField] private ElectricSmallBulb[] _linkedSmallBulbs;
    [SerializeField] private bool _requireSmallBulbActiveInHierarchy = true;

    [Header("Visuals")]
    [SerializeField] private bool _autoFindVisualsWhenEmpty = true;
    [SerializeField] private GameObject[] _enableWhenPowered;
    [SerializeField] private GameObject[] _disableWhenPowered;
    [SerializeField] private Light[] _lights;
    [SerializeField] private ParticleSystem[] _particles;

    [Header("Events")]
    [SerializeField] private UnityEvent _onCentralPowered;
    [SerializeField] private UnityEvent _onCentralUnpowered;

    [Header("Debug")]
    [SerializeField] private bool _lastPowered;
    [SerializeField] private bool _hasAppliedState;
    [SerializeField] private int _activeLinkedSmallBulbCount;

    private ElectricNode _node;
    private float _firstAllowedApplyTime;

    public bool LastPowered => _lastPowered;
    public int ActiveLinkedSmallBulbCount => _activeLinkedSmallBulbCount;

    private void Awake()
    {
        _node = GetComponent<ElectricNode>();
        RefreshAutoVisuals(false);
        _firstAllowedApplyTime = Time.time + Mathf.Max(0f, Mathf.Max(_firstUpdateDelay, _startupDelayBeforeFirstApply));
    }

    private void Start()
    {
        AutoFindReferencesIfNeeded();
    }

    private void Update()
    {
        if (_node == null)
            _node = GetComponent<ElectricNode>();

        AutoFindReferencesIfNeeded();

        if (!_hasAppliedState && _doNotForceBarrierOffBeforeFirstNetworkUpdate && Time.time < _firstAllowedApplyTime)
            return;

        ApplyState(EvaluatePoweredState(), false);
    }

    public void SetBarrier(ElectricBarrier barrier)
    {
        _barrier = barrier;
    }

    public void SetBarrierControlMode(BarrierControlMode mode)
    {
        _barrierControlMode = mode;
    }

    public void SetLinkedSmallBulbs(ElectricSmallBulb[] bulbs)
    {
        _linkedSmallBulbs = bulbs;
    }

    [ContextMenu("Refresh Auto Visuals")]
    public void RefreshAutoVisuals()
    {
        RefreshAutoVisuals(true);
    }

    public void RefreshAutoVisuals(bool force)
    {
        if (!_autoFindVisualsWhenEmpty && !force)
            return;

        if (force || _lights == null || _lights.Length == 0)
            _lights = GetComponentsInChildren<Light>(true);

        if (force || _particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    [ContextMenu("Find Barrier In Scene")]
    public void FindAndAssignBarrier()
    {
        _barrier = FindBarrierInScene();
    }

    [ContextMenu("Find Small Bulbs In Scene")]
    public void FindAndAssignSmallBulbs()
    {
        _linkedSmallBulbs = FindSmallBulbsInScene();
    }

    private bool EvaluatePoweredState()
    {
        if (_barrierControlMode == BarrierControlMode.NetworkPowered)
            return _node != null && _node.IsPowered;

        _activeLinkedSmallBulbCount = 0;

        ElectricSmallBulb[] bulbs = _linkedSmallBulbs;
        if (_autoFindSmallBulbs && (bulbs == null || bulbs.Length == 0))
        {
            _linkedSmallBulbs = FindSmallBulbsInScene();
            bulbs = _linkedSmallBulbs;
        }

        if (bulbs == null || bulbs.Length == 0)
            return _node != null && _node.IsPowered;

        for (int i = 0; i < bulbs.Length; i++)
        {
            ElectricSmallBulb bulb = bulbs[i];
            if (bulb == null)
                continue;

            if (_requireSmallBulbActiveInHierarchy && !bulb.gameObject.activeInHierarchy)
                continue;

            if (!bulb.IsDisabledByShock)
            {
                _activeLinkedSmallBulbCount++;
                return true;
            }
        }

        return false;
    }

    private void ApplyState(bool powered, bool force)
    {
        if (!force && _hasAppliedState && _lastPowered == powered)
            return;

        bool previousPowered = _lastPowered;
        _lastPowered = powered;
        _hasAppliedState = true;

        if (_activateBarrierWhenPowered && _barrier != null)
            _barrier.SetBarrierActive(powered);
        else if (!_activateBarrierWhenPowered && _barrier != null)
            _barrier.SetBarrierActive(!powered);

        SetObjectsActive(_enableWhenPowered, powered);
        SetObjectsActive(_disableWhenPowered, !powered);
        SetLights(powered);
        SetParticles(powered);

        if (!force && previousPowered != powered)
        {
            if (powered)
                _onCentralPowered?.Invoke();
            else
                _onCentralUnpowered?.Invoke();
        }
    }

    private void AutoFindReferencesIfNeeded()
    {
        if (_autoFindBarrier && _barrier == null)
            _barrier = FindBarrierInScene();

        if (_autoFindSmallBulbs && (_linkedSmallBulbs == null || _linkedSmallBulbs.Length == 0))
            _linkedSmallBulbs = FindSmallBulbsInScene();
    }

    private ElectricBarrier FindBarrierInScene()
    {
#if UNITY_2023_1_OR_NEWER
        ElectricBarrier existing = FindFirstObjectByType<ElectricBarrier>(FindObjectsInactive.Include);
#else
        ElectricBarrier existing = FindObjectOfType<ElectricBarrier>(true);
#endif
        if (existing != null)
            return existing;

        if (_barrierNames != null)
        {
            for (int i = 0; i < _barrierNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_barrierNames[i]))
                    continue;

                GameObject found = GameObject.Find(_barrierNames[i]);
                if (found == null)
                    continue;

                ElectricBarrier barrier = found.GetComponent<ElectricBarrier>();
                if (barrier == null)
                    barrier = found.AddComponent<ElectricBarrier>();

                return barrier;
            }
        }

        return null;
    }

    private ElectricSmallBulb[] FindSmallBulbsInScene()
    {
#if UNITY_2023_1_OR_NEWER
        ElectricSmallBulb[] bulbs = FindObjectsByType<ElectricSmallBulb>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        ElectricSmallBulb[] bulbs = FindObjectsOfType<ElectricSmallBulb>(true);
#endif
        if (bulbs != null && bulbs.Length > 0)
            return bulbs;

        return new ElectricSmallBulb[0];
    }

    private void SetLights(bool active)
    {
        if (_lights == null)
            return;

        for (int i = 0; i < _lights.Length; i++)
        {
            if (_lights[i] != null)
                _lights[i].enabled = active;
        }
    }

    private void SetParticles(bool active)
    {
        if (_particles == null)
            return;

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem particle = _particles[i];
            if (particle == null)
                continue;

            if (active)
                particle.Play(true);
            else
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }
}
