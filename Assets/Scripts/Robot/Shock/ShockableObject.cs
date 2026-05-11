using UnityEngine;
using UnityEngine.Events;

public class ShockableObject : MonoBehaviour, IShockable
{
    [Header("Shock")]
    [SerializeField] private bool _onlyOnce = true;
    [SerializeField] private UnityEvent _onShocked;

    [Header("Optional Disable Targets")]
    [SerializeField] private bool _disableWholeObject;
    [SerializeField] private GameObject[] _objectsToDisable;
    [SerializeField] private Behaviour[] _behavioursToDisable;
    [SerializeField] private Renderer[] _renderersToDisable;
    [SerializeField] private Light[] _lightsToDisable;
    [SerializeField] private Collider[] _collidersToDisable;
    [SerializeField] private ParticleSystem[] _particlesToStop;
    [SerializeField] private bool _clearParticles = true;
    [SerializeField] private bool _deactivateParticleObjects;

    [Header("Optional VFX")]
    [SerializeField] private GameObject _shockedVfxPrefab;
    [SerializeField] private Transform _vfxSpawnPoint;
    [SerializeField] private float _vfxAutoDestroyDelay = 3f;

    [Header("Debug")]
    [SerializeField] private bool _hasBeenShocked;

    public bool HasBeenShocked => _hasBeenShocked;

    public void OnShock(ShockInfo shockInfo)
    {
        if (_onlyOnce && _hasBeenShocked)
            return;

        _hasBeenShocked = true;
        SpawnVfx();
        _onShocked?.Invoke();
        ApplyDisableTargets();
    }

    public void ResetShockState()
    {
        _hasBeenShocked = false;
    }

    private void SpawnVfx()
    {
        if (_shockedVfxPrefab == null)
            return;

        Vector3 position = _vfxSpawnPoint != null ? _vfxSpawnPoint.position : transform.position;
        Quaternion rotation = _vfxSpawnPoint != null ? _vfxSpawnPoint.rotation : Quaternion.identity;
        GameObject instance = Instantiate(_shockedVfxPrefab, position, rotation);

        if (_vfxAutoDestroyDelay > 0f)
            Destroy(instance, _vfxAutoDestroyDelay);
    }

    private void ApplyDisableTargets()
    {
        SetObjectsActive(_objectsToDisable, false);
        SetBehavioursEnabled(_behavioursToDisable, false);
        SetRenderersEnabled(_renderersToDisable, false);
        SetLightsEnabled(_lightsToDisable, false);
        SetCollidersEnabled(_collidersToDisable, false);
        StopParticles();

        if (_disableWholeObject)
            gameObject.SetActive(false);
    }

    private void StopParticles()
    {
        if (_particlesToStop == null)
            return;

        ParticleSystemStopBehavior stopBehavior = _clearParticles
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        for (int i = 0; i < _particlesToStop.Length; i++)
        {
            ParticleSystem particle = _particlesToStop[i];
            if (particle == null)
                continue;

            particle.Stop(true, stopBehavior);

            if (_deactivateParticleObjects)
                particle.gameObject.SetActive(false);
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

    private static void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = enabled;
        }
    }

    private static void SetRenderersEnabled(Renderer[] renderers, bool enabled)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = enabled;
        }
    }

    private static void SetLightsEnabled(Light[] lights, bool enabled)
    {
        if (lights == null)
            return;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = enabled;
        }
    }

    private static void SetCollidersEnabled(Collider[] colliders, bool enabled)
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled;
        }
    }
}
