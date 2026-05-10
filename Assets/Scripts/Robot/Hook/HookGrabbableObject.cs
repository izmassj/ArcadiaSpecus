using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HookGrabbableObject : MonoBehaviour
{
    [Header("Grab")]
    [SerializeField] private bool _canBeGrabbed = true;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private Vector3 _targetOffset = Vector3.zero;
    [SerializeField] private Transform _connectPoint;
    [SerializeField] private bool _useConnectPointAsHookTarget = true;
    [SerializeField] private bool _alignConnectPointToCarryPoint = true;
    [SerializeField] private Vector3 _carriedLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 _carriedLocalEuler = Vector3.zero;

    [Header("Selection")]
    [SerializeField] private float _indicatorHeight = 1.25f;

    private Transform _initialParent;
    private bool _initialKinematic;
    private bool _initialUseGravity;
    private bool _initialDetectCollisions;
    private bool _isHooked;
    private bool _isCarried;
    private Transform _carryTarget;
    private bool _carriedWithParenting;
    private Vector3 _carriedWorldScale = Vector3.one;
    private Vector3 _connectLocalPosition;
    private Quaternion _connectLocalRotation = Quaternion.identity;
    private bool _hasConnectPose;

    public bool CanBeGrabbed => _canBeGrabbed && !_isHooked && !_isCarried;
    public bool IsHooked => _isHooked;
    public bool IsCarried => _isCarried;
    public Rigidbody Rigidbody => _rigidbody;
    public float IndicatorHeight => _indicatorHeight;
    public Vector3 CarriedLocalPosition => _carriedLocalPosition;
    public Quaternion CarriedLocalRotation => Quaternion.Euler(_carriedLocalEuler);
    public Transform ConnectPoint => _connectPoint;

    private void Reset()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
    }

    private void Awake()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        if (_colliders == null || _colliders.Length == 0)
            _colliders = GetComponentsInChildren<Collider>(true);

        _initialParent = transform.parent;

        if (_rigidbody != null)
        {
            _initialKinematic = _rigidbody.isKinematic;
            _initialUseGravity = _rigidbody.useGravity;
            _initialDetectCollisions = _rigidbody.detectCollisions;
        }

        CacheConnectPose();
    }

    public Vector3 GetTargetPoint()
    {
        if (_useConnectPointAsHookTarget && _connectPoint != null)
            return _connectPoint.position + transform.TransformVector(_targetOffset);

        if (_rigidbody != null)
            return _rigidbody.worldCenterOfMass + transform.TransformVector(_targetOffset);

        return transform.position + transform.TransformVector(_targetOffset);
    }

    public Vector3 GetIndicatorPoint()
    {
        return GetTargetPoint() + Vector3.up * _indicatorHeight;
    }

    public void BeginHooked()
    {
        _isHooked = true;
        _isCarried = false;

        if (_rigidbody == null)
            return;

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.detectCollisions = true;
    }

    public void BeginCarried(Transform carryTarget, bool detectCollisionsWhileCarried, bool parentToTarget)
    {
        _isHooked = false;
        _isCarried = true;
        _carryTarget = carryTarget;
        _carriedWithParenting = parentToTarget;
        _carriedWorldScale = transform.lossyScale;
        CacheConnectPose();

        bool useConnectAlignment = _alignConnectPointToCarryPoint && _connectPoint != null && _hasConnectPose;
        if (useConnectAlignment)
            _carriedWithParenting = false;

        if (_carriedWithParenting && _carryTarget != null)
        {
            transform.SetParent(_carryTarget, true);
            transform.localPosition = _carriedLocalPosition;
            transform.localRotation = CarriedLocalRotation;
        }
        else
        {
            transform.SetParent(null, true);
            transform.localScale = _carriedWorldScale;
            UpdateCarriedPose();
        }

        if (_rigidbody == null)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.detectCollisions = detectCollisionsWhileCarried;
    }

    public void UpdateCarriedPose()
    {
        if (!_isCarried || _carryTarget == null)
            return;

        if (_alignConnectPointToCarryPoint && _connectPoint != null && _hasConnectPose)
        {
            ApplyConnectPointPose();
            return;
        }

        if (_carriedWithParenting)
        {
            transform.localPosition = _carriedLocalPosition;
            transform.localRotation = CarriedLocalRotation;
            return;
        }

        transform.position = _carryTarget.TransformPoint(_carriedLocalPosition);
        transform.rotation = _carryTarget.rotation * CarriedLocalRotation;
        transform.localScale = _carriedWorldScale;
    }

    private void CacheConnectPose()
    {
        if (_connectPoint == null)
        {
            _hasConnectPose = false;
            return;
        }

        _connectLocalPosition = transform.InverseTransformPoint(_connectPoint.position);
        _connectLocalRotation = Quaternion.Inverse(transform.rotation) * _connectPoint.rotation;
        _hasConnectPose = true;
    }

    private void ApplyConnectPointPose()
    {
        Quaternion desiredConnectRotation = _carryTarget.rotation * CarriedLocalRotation;
        Vector3 desiredConnectPosition = _carryTarget.TransformPoint(_carriedLocalPosition);

        Quaternion desiredRootRotation = desiredConnectRotation * Quaternion.Inverse(_connectLocalRotation);
        Vector3 scaledConnectOffset = Vector3.Scale(_carriedWorldScale, _connectLocalPosition);
        Vector3 desiredRootPosition = desiredConnectPosition - desiredRootRotation * scaledConnectOffset;

        transform.SetPositionAndRotation(desiredRootPosition, desiredRootRotation);
        transform.localScale = _carriedWorldScale;
    }

    public void ReleaseCarried(bool restoreOriginalParent)
    {
        _isHooked = false;
        _isCarried = false;

        if (restoreOriginalParent)
            transform.SetParent(_initialParent, true);
        else
        {
            transform.SetParent(null, true);
            transform.localScale = _carriedWorldScale;
        }

        _carryTarget = null;
        _carriedWithParenting = false;

        if (_rigidbody == null)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = _initialKinematic;
        _rigidbody.useGravity = _initialUseGravity;
        _rigidbody.detectCollisions = _initialDetectCollisions;
    }

    public void CancelHook()
    {
        _isHooked = false;
        _carryTarget = null;
        _carriedWithParenting = false;

        if (_rigidbody == null)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = _initialKinematic;
        _rigidbody.useGravity = _initialUseGravity;
        _rigidbody.detectCollisions = _initialDetectCollisions;
    }
}
