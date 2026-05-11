using UnityEngine;

[DisallowMultipleComponent]
public class ElectricBarrier : MonoBehaviour
{
    [Header("Startup")]
    [SerializeField] private bool _applyInitialStateOnAwake = false;
    [SerializeField] private bool _applyStateOnAwake = false;
    [SerializeField] private bool _activeOnStart = true;

    [Header("Targets")]
    [SerializeField] private bool _autoCollectTargets = true;
    [SerializeField] private bool _autoFindTargetsWhenEmpty = true;
    [SerializeField] private bool _controlBarrierRootActive = false;
    [SerializeField] private bool _controlBarrierRoot = false;
    [SerializeField] private bool _controlBehaviours = false;
    [SerializeField] private GameObject _barrierRoot;
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private Behaviour[] _behaviours;

    [Header("Debug")]
    [SerializeField] private bool _lastAppliedState = true;

    public bool LastAppliedState => _lastAppliedState;

    private void Awake()
    {
        CacheTargetsIfNeeded();

        if (_applyInitialStateOnAwake || _applyStateOnAwake)
            SetBarrierActive(_activeOnStart);
        else
            _lastAppliedState = GuessCurrentActiveState();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && _barrierRoot == gameObject)
        {
            _controlBarrierRootActive = false;
            _controlBarrierRoot = false;
        }
    }

    [ContextMenu("Cache Targets From Children")]
    public void CacheTargetsFromChildren()
    {
        AutoCollectTargets();
    }

    [ContextMenu("Auto Collect Barrier Targets")]
    public void AutoCollectTargets()
    {
        if (_barrierRoot == null)
            _barrierRoot = gameObject;

        Transform searchRoot = _barrierRoot != null ? _barrierRoot.transform : transform;
        _colliders = searchRoot.GetComponentsInChildren<Collider>(true);
        _renderers = searchRoot.GetComponentsInChildren<Renderer>(true);
        _particles = searchRoot.GetComponentsInChildren<ParticleSystem>(true);

        if (_controlBehaviours)
            _behaviours = searchRoot.GetComponentsInChildren<Behaviour>(true);
        else
            _behaviours = new Behaviour[0];
    }

    public void SetBarrierActive(bool active)
    {
        CacheTargetsIfNeeded();
        _lastAppliedState = active;

        if ((_controlBarrierRootActive || _controlBarrierRoot) && _barrierRoot != null && _barrierRoot != gameObject)
            _barrierRoot.SetActive(active);

        SetColliders(active);
        SetRenderers(active);
        SetParticles(active);

        if (_controlBehaviours)
            SetBehaviours(active);
    }

    private void CacheTargetsIfNeeded()
    {
        bool shouldAutoCollect = _autoCollectTargets || _autoFindTargetsWhenEmpty;
        if (!shouldAutoCollect)
            return;

        bool needsColliders = _colliders == null || _colliders.Length == 0;
        bool needsRenderers = _renderers == null || _renderers.Length == 0;
        bool needsParticles = _particles == null || _particles.Length == 0;
        bool needsBehaviours = _controlBehaviours && (_behaviours == null || _behaviours.Length == 0);

        if (!needsColliders && !needsRenderers && !needsParticles && !needsBehaviours)
            return;

        if (_barrierRoot == null)
            _barrierRoot = gameObject;

        Transform searchRoot = _barrierRoot != null ? _barrierRoot.transform : transform;

        if (needsColliders)
            _colliders = searchRoot.GetComponentsInChildren<Collider>(true);

        if (needsRenderers)
            _renderers = searchRoot.GetComponentsInChildren<Renderer>(true);

        if (needsParticles)
            _particles = searchRoot.GetComponentsInChildren<ParticleSystem>(true);

        if (needsBehaviours)
            _behaviours = searchRoot.GetComponentsInChildren<Behaviour>(true);
        else if (_behaviours == null)
            _behaviours = new Behaviour[0];
    }

    private bool GuessCurrentActiveState()
    {
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    return _colliders[i].enabled;
            }
        }

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    return _renderers[i].enabled;
            }
        }

        return gameObject.activeInHierarchy;
    }

    private void SetColliders(bool active)
    {
        if (_colliders == null)
            return;

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = active;
        }
    }

    private void SetRenderers(bool active)
    {
        if (_renderers == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].enabled = active;
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

    private void SetBehaviours(bool active)
    {
        if (_behaviours == null)
            return;

        for (int i = 0; i < _behaviours.Length; i++)
        {
            Behaviour behaviour = _behaviours[i];
            if (behaviour != null && behaviour != this)
                behaviour.enabled = active;
        }
    }
}
