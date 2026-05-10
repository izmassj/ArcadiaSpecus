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
    [SerializeField] private GameObject[] _enabledWhenAlive;
    [SerializeField] private GameObject[] _disabledWhenAlive;
    [SerializeField] private Light[] _lights;
    [SerializeField] private Renderer[] _renderers;
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
        ApplyAliveVisuals(!_isDisabledByShock);
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

    public void DisableBulb()
    {
        if (_isDisabledByShock)
            return;

        _isDisabledByShock = true;

        if (_node == null)
            _node = GetComponent<ElectricNode>();

        _node.SetDisabled(true);
        ApplyAliveVisuals(false);
        _onDisabledByShock?.Invoke();
    }

    public void ResetBulb()
    {
        _isDisabledByShock = false;

        if (_node == null)
            _node = GetComponent<ElectricNode>();

        _node.SetDisabled(false);
        ApplyAliveVisuals(true);
        _onResetBulb?.Invoke();
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
