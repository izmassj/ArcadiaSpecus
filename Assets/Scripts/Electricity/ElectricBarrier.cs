using System.Collections.Generic;
using UnityEngine;

public class ElectricBarrier : MonoBehaviour
{
    [SerializeField] private bool _activeOnStart;
    [SerializeField] private bool _autoFindTargetsWhenEmpty = true;
    [SerializeField] private GameObject _barrierRoot;
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private Behaviour[] _behaviours;

    private void Awake()
    {
        CacheTargetsIfNeeded();
        SetBarrierActive(_activeOnStart);
    }

    [ContextMenu("Cache Barrier Targets From Children")]
    public void CacheTargetsFromChildren()
    {
        _barrierRoot = gameObject;
        _colliders = GetComponentsInChildren<Collider>(true);
        _renderers = GetComponentsInChildren<Renderer>(true);
        _particles = GetComponentsInChildren<ParticleSystem>(true);

        Behaviour[] behaviours = GetComponentsInChildren<Behaviour>(true);
        List<Behaviour> filteredBehaviours = new List<Behaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this || behaviour is ElectricBarrier)
                continue;

            filteredBehaviours.Add(behaviour);
        }

        _behaviours = filteredBehaviours.ToArray();
    }

    public void SetBarrierActive(bool active)
    {
        CacheTargetsIfNeeded();

        if (_barrierRoot != null && _barrierRoot != gameObject)
            _barrierRoot.SetActive(active);

        SetColliders(active);
        SetRenderers(active);
        SetBehaviours(active);
        SetParticles(active);
    }

    private void CacheTargetsIfNeeded()
    {
        if (!_autoFindTargetsWhenEmpty)
            return;

        bool empty = (_barrierRoot == null) &&
                     (_colliders == null || _colliders.Length == 0) &&
                     (_renderers == null || _renderers.Length == 0) &&
                     (_particles == null || _particles.Length == 0) &&
                     (_behaviours == null || _behaviours.Length == 0);

        if (empty)
            CacheTargetsFromChildren();
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

    private void SetBehaviours(bool active)
    {
        if (_behaviours == null)
            return;

        for (int i = 0; i < _behaviours.Length; i++)
        {
            if (_behaviours[i] != null)
                _behaviours[i].enabled = active;
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
}
