using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(ElectricNode))]
public class ElectricCentralBulb : MonoBehaviour
{
    [SerializeField] private ElectricBarrier _barrier;
    [SerializeField] private bool _activateBarrierWhenPowered = true;
    [SerializeField] private bool _autoFindBarrierWhenMissing = true;
    [SerializeField] private bool _autoFindVisualsWhenEmpty = true;
    [SerializeField] private GameObject[] _enableWhenPowered;
    [SerializeField] private GameObject[] _disableWhenPowered;
    [SerializeField] private Light[] _lights;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private Material _poweredMaterial;
    [SerializeField] private Material _unpoweredMaterial;
    [SerializeField] private UnityEvent _onCentralPowered;
    [SerializeField] private UnityEvent _onCentralUnpowered;

    private ElectricNode _node;
    private bool _lastPowered;
    private Material[] _initialMaterials;

    private void Awake()
    {
        _node = GetComponent<ElectricNode>();
        CacheAutoVisualsIfNeeded(false);
        CacheInitialMaterials();
        TryAutoFindBarrier();
    }

    private void OnEnable()
    {
        _node = GetComponent<ElectricNode>();
        TryAutoFindBarrier();
        ApplyState(_node != null && _node.IsPowered, true);
    }

    private void Update()
    {
        if (_node == null)
            return;

        ApplyState(_node.IsPowered, false);
    }

    public void SetBarrier(ElectricBarrier barrier)
    {
        _barrier = barrier;
        ApplyState(_node != null && _node.IsPowered, true);
    }

    public void RefreshAutoVisuals()
    {
        CacheAutoVisualsIfNeeded(true);
        CacheInitialMaterials();
        ApplyState(_node != null && _node.IsPowered, true);
    }

    private void TryAutoFindBarrier()
    {
        if (!_autoFindBarrierWhenMissing || _barrier != null)
            return;

#if UNITY_2023_1_OR_NEWER
        _barrier = FindFirstObjectByType<ElectricBarrier>(FindObjectsInactive.Include);
#else
        _barrier = FindObjectOfType<ElectricBarrier>(true);
#endif
    }

    private void ApplyState(bool powered, bool force)
    {
        if (!force && _lastPowered == powered)
            return;

        _lastPowered = powered;

        if (_activateBarrierWhenPowered && _barrier != null)
            _barrier.SetBarrierActive(powered);

        SetObjectsActive(_enableWhenPowered, powered);
        SetObjectsActive(_disableWhenPowered, !powered);

        if (_lights != null)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                    _lights[i].enabled = powered;
            }
        }

        if (_renderers != null && (_poweredMaterial != null || _unpoweredMaterial != null))
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer targetRenderer = _renderers[i];
                if (targetRenderer == null)
                    continue;

                if (powered && _poweredMaterial != null)
                    targetRenderer.sharedMaterial = _poweredMaterial;
                else if (!powered && _unpoweredMaterial != null)
                    targetRenderer.sharedMaterial = _unpoweredMaterial;
                else if (!powered && _unpoweredMaterial == null && _initialMaterials != null && i < _initialMaterials.Length)
                    targetRenderer.sharedMaterial = _initialMaterials[i];
            }
        }

        if (!force)
        {
            if (powered)
                _onCentralPowered?.Invoke();
            else
                _onCentralUnpowered?.Invoke();
        }
    }

    private void CacheAutoVisualsIfNeeded(bool force)
    {
        if (!_autoFindVisualsWhenEmpty && !force)
            return;

        if (force || _renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>(true);

        if (force || _lights == null || _lights.Length == 0)
            _lights = GetComponentsInChildren<Light>(true);
    }

    private void CacheInitialMaterials()
    {
        if (_renderers == null || _renderers.Length == 0)
            return;

        _initialMaterials = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _initialMaterials[i] = _renderers[i].sharedMaterial;
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
