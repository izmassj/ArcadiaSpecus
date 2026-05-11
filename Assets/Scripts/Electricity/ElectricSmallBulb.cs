using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(ElectricNode))]
public class ElectricSmallBulb : MonoBehaviour, IShockable
{
    [Header("Shock")]
    [SerializeField] private bool _shockDisablesBulb = true;
    [SerializeField] private bool _onlyShockOnce = true;

    [Header("Visuals")]
    [SerializeField] private bool _autoFindVisualsWhenEmpty = true;
    [SerializeField] private GameObject[] _enabledWhenAlive;
    [SerializeField] private GameObject[] _disabledWhenAlive;
    [SerializeField] private Light[] _lights;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private bool _stopParticlesWhenDisabled = true;
    [SerializeField] private bool _clearParticlesWhenDisabled = true;
    [SerializeField] private bool _deactivateParticleObjectsWhenDisabled = true;
    [SerializeField] private bool _playParticlesWhenReset = true;
    [SerializeField] private Material _aliveMaterial;
    [SerializeField] private Material _disabledMaterial;

    [Header("Events")]
    [SerializeField] private UnityEvent _onDisabledByShock;
    [SerializeField] private UnityEvent _onResetBulb;

    [Header("Debug")]
    [SerializeField] private bool _isDisabledByShock;

    private ElectricNode _node;

    public bool IsDisabledByShock => _isDisabledByShock;

    private void Awake()
    {
        _node = GetComponent<ElectricNode>();
        CacheAutoVisualsIfNeeded(false);
        ApplyAliveVisuals(!_isDisabledByShock);

        if (_node != null)
            _node.SetDisabled(_isDisabledByShock);
    }

    public void OnShock(ShockInfo shockInfo)
    {
        if (!_shockDisablesBulb)
            return;

        if (_onlyShockOnce && _isDisabledByShock)
            return;

        DisableBulb();
    }

    [ContextMenu("Disable Bulb")]
    public void DisableBulb()
    {
        if (_isDisabledByShock)
            return;

        _isDisabledByShock = true;

        if (_node == null)
            _node = GetComponent<ElectricNode>();

        if (_node != null)
        {
            _node.SetDisabled(true);
            _node.SetPowerSourceActive(false);
        }

        ApplyAliveVisuals(false);
        _onDisabledByShock?.Invoke();
        ElectricityNetworkManager.RequestRecalculateAll();
    }

    [ContextMenu("Reset Bulb")]
    public void ResetBulb()
    {
        _isDisabledByShock = false;

        if (_node == null)
            _node = GetComponent<ElectricNode>();

        if (_node != null)
        {
            _node.SetDisabled(false);
            _node.SetStartsAsPowerSource(true);
            _node.SetPowerSourceActive(true);
        }

        ApplyAliveVisuals(true);
        _onResetBulb?.Invoke();
        ElectricityNetworkManager.RequestRecalculateAll();
    }

    public void RefreshAutoVisuals()
    {
        CacheAutoVisualsIfNeeded(true);
        ApplyAliveVisuals(!_isDisabledByShock);
    }

    private void CacheAutoVisualsIfNeeded(bool force)
    {
        if (!_autoFindVisualsWhenEmpty && !force)
            return;

        if (force || _renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>(true);

        if (force || _lights == null || _lights.Length == 0)
            _lights = GetComponentsInChildren<Light>(true);

        if (force || _particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void ApplyAliveVisuals(bool alive)
    {
        SetObjectsActive(_enabledWhenAlive, alive);
        SetObjectsActive(_disabledWhenAlive, !alive);

        if (_lights != null)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                    _lights[i].enabled = alive;
            }
        }

        if (_renderers != null)
        {
            Material material = alive ? _aliveMaterial : _disabledMaterial;
            if (material != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null)
                        _renderers[i].sharedMaterial = material;
                }
            }
        }

        ApplyParticleState(alive);
    }

    private void ApplyParticleState(bool alive)
    {
        if (_particles == null)
            return;

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem particle = _particles[i];
            if (particle == null)
                continue;

            if (alive)
            {
                if (_deactivateParticleObjectsWhenDisabled && !particle.gameObject.activeSelf)
                    particle.gameObject.SetActive(true);

                if (_playParticlesWhenReset && !particle.isPlaying)
                    particle.Play(true);
            }
            else
            {
                if (_stopParticlesWhenDisabled)
                {
                    ParticleSystemStopBehavior stopBehavior = _clearParticlesWhenDisabled
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting;

                    particle.Stop(true, stopBehavior);
                    particle.Clear(true);
                }

                if (_deactivateParticleObjectsWhenDisabled)
                    particle.gameObject.SetActive(false);
            }
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
