using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ElectricNode))]
public class ElectricPowerSource : MonoBehaviour
{
    [SerializeField] private bool _activeOnStart = true;
    [SerializeField] private bool _forceNodeAsSource = true;

    private ElectricNode _node;

    public bool IsActive => _node != null && _node.IsActivePowerSource;

    private void Awake()
    {
        _node = GetComponent<ElectricNode>();

        if (_forceNodeAsSource)
            _node.SetStartsAsPowerSource(true);

        _node.SetPowerSourceActive(_activeOnStart);
    }

    public void SetActiveSource(bool active)
    {
        if (_node == null)
            _node = GetComponent<ElectricNode>();

        if (_forceNodeAsSource)
            _node.SetStartsAsPowerSource(true);

        _node.SetPowerSourceActive(active);
    }

    public void TurnOn() => SetActiveSource(true);
    public void TurnOff() => SetActiveSource(false);
    public void Toggle() => SetActiveSource(!IsActive);
}
