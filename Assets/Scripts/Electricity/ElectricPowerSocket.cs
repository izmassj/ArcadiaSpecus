using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ElectricNode))]
public class ElectricPowerSocket : MonoBehaviour
{
    [Header("Socket")]
    [SerializeField] private Transform _snapPoint;
    [SerializeField] private bool _snapProvider;
    [SerializeField] private bool _makeProviderKinematicWhileSocketed = true;
    [SerializeField] private bool _releaseWhenProviderExits = true;

    [Header("Debug")]
    [SerializeField] private ElectricPowerProvider _currentProvider;

    private ElectricNode _node;
    private Rigidbody _currentRigidbody;
    private bool _previousKinematic;

    private void Awake()
    {
        _node = GetComponent<ElectricNode>();
        _node.SetStartsAsPowerSource(true);
        _node.SetPowerSourceActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        ElectricPowerProvider provider = other.GetComponentInParent<ElectricPowerProvider>();
        if (provider == null || !provider.CanPowerSockets || _currentProvider != null)
            return;

        SetProvider(provider);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_releaseWhenProviderExits || _currentProvider == null)
            return;

        ElectricPowerProvider provider = other.GetComponentInParent<ElectricPowerProvider>();
        if (provider == _currentProvider)
            ClearProvider();
    }

    public void SetProvider(ElectricPowerProvider provider)
    {
        if (provider == null || !provider.CanPowerSockets)
            return;

        _currentProvider = provider;
        _currentRigidbody = provider.GetComponentInParent<Rigidbody>();

        if (_currentRigidbody != null)
        {
            _previousKinematic = _currentRigidbody.isKinematic;

            if (_makeProviderKinematicWhileSocketed)
                _currentRigidbody.isKinematic = true;
        }

        if (_snapProvider)
            SnapProvider(provider.transform);

        _node.SetPowerSourceActive(true);
    }

    public void ClearProvider()
    {
        if (_currentRigidbody != null && _makeProviderKinematicWhileSocketed)
            _currentRigidbody.isKinematic = _previousKinematic;

        _currentProvider = null;
        _currentRigidbody = null;
        _node.SetPowerSourceActive(false);
    }

    private void SnapProvider(Transform providerTransform)
    {
        if (_snapPoint == null || providerTransform == null)
            return;

        providerTransform.SetPositionAndRotation(_snapPoint.position, _snapPoint.rotation);
    }
}
