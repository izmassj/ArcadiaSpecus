using UnityEngine;

public class ElectricBarrier : MonoBehaviour
{
    [SerializeField] private bool _activeOnStart;
    [SerializeField] private GameObject _barrierRoot;
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private Behaviour[] _behaviours;

    private void Awake()
    {
        SetBarrierActive(_activeOnStart);
    }

    public void SetBarrierActive(bool active)
    {
        if (_barrierRoot != null)
            _barrierRoot.SetActive(active);

        SetColliders(active);
        SetRenderers(active);
        SetBehaviours(active);
        SetParticles(active);
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
