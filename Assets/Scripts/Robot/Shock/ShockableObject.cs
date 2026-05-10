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
        if (_objectsToDisable != null)
        {
            for (int i = 0; i < _objectsToDisable.Length; i++)
            {
                if (_objectsToDisable[i] != null)
                    _objectsToDisable[i].SetActive(false);
            }
        }

        if (_behavioursToDisable != null)
        {
            for (int i = 0; i < _behavioursToDisable.Length; i++)
            {
                if (_behavioursToDisable[i] != null)
                    _behavioursToDisable[i].enabled = false;
            }
        }

        if (_renderersToDisable != null)
        {
            for (int i = 0; i < _renderersToDisable.Length; i++)
            {
                if (_renderersToDisable[i] != null)
                    _renderersToDisable[i].enabled = false;
            }
        }

        if (_lightsToDisable != null)
        {
            for (int i = 0; i < _lightsToDisable.Length; i++)
            {
                if (_lightsToDisable[i] != null)
                    _lightsToDisable[i].enabled = false;
            }
        }

        if (_collidersToDisable != null)
        {
            for (int i = 0; i < _collidersToDisable.Length; i++)
            {
                if (_collidersToDisable[i] != null)
                    _collidersToDisable[i].enabled = false;
            }
        }

        if (_disableWholeObject)
            gameObject.SetActive(false);
    }
}
