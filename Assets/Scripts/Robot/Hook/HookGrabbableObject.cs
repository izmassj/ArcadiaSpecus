using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HookGrabbableObject : MonoBehaviour
{
    [Header("Grab")]
    [SerializeField] private bool _canBeGrabbed = true;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private Vector3 _targetOffset = Vector3.zero;
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

    public bool CanBeGrabbed => _canBeGrabbed && !_isHooked && !_isCarried;
    public bool IsHooked => _isHooked;
    public bool IsCarried => _isCarried;
    public Rigidbody Rigidbody => _rigidbody;
    public float IndicatorHeight => _indicatorHeight;
    public Vector3 CarriedLocalPosition => _carriedLocalPosition;
    public Quaternion CarriedLocalRotation => Quaternion.Euler(_carriedLocalEuler);

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
    }

    public Vector3 GetTargetPoint()
    {
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

    public void BeginCarried(Transform parent, bool detectCollisionsWhileCarried)
    {
        _isHooked = false;
        _isCarried = true;

        transform.SetParent(parent, true);
        transform.localPosition = _carriedLocalPosition;
        transform.localRotation = CarriedLocalRotation;

        if (_rigidbody == null)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.detectCollisions = detectCollisionsWhileCarried;
    }

    public void ReleaseCarried(bool restoreOriginalParent)
    {
        _isHooked = false;
        _isCarried = false;

        if (restoreOriginalParent)
            transform.SetParent(_initialParent, true);
        else
            transform.SetParent(null, true);

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

        if (_rigidbody == null)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = _initialKinematic;
        _rigidbody.useGravity = _initialUseGravity;
        _rigidbody.detectCollisions = _initialDetectCollisions;
    }
}
