using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(ElectricNode))]
public class ElectricCentralBulb : MonoBehaviour
{
    [SerializeField] private ElectricBarrier _barrier;
    [SerializeField] private bool _activateBarrierWhenPowered = true;
    [SerializeField] private GameObject[] _enableWhenPowered;
    [SerializeField] private GameObject[] _disableWhenPowered;
    [SerializeField] private Light[] _lights;
    [SerializeField] private UnityEvent _onCentralPowered;
    [SerializeField] private UnityEvent _onCentralUnpowered;

    private ElectricNode _node;
    private bool _lastPowered;

    private void Awake()
    {
        _node = GetComponent<ElectricNode>();
    }

    private void OnEnable()
    {
        _node = GetComponent<ElectricNode>();
        ApplyState(_node != null && _node.IsPowered, true);
    }

    private void Update()
    {
        if (_node == null)
            return;

        ApplyState(_node.IsPowered, false);
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

        if (!force)
        {
            if (powered)
                _onCentralPowered?.Invoke();
            else
                _onCentralUnpowered?.Invoke();
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
