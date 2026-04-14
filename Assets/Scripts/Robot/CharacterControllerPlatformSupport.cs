using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerPlatformSupport : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController _characterController;

    [Header("Ground Probe")]
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private float _probeExtraDistance = 0.3f;
    [SerializeField] [Range(0.1f, 1f)] private float _probeRadiusScale = 0.9f;
    [SerializeField] [Range(0.01f, 1f)] private float _minGroundNormalY = 0.35f;

    [Header("Debug")]
    [SerializeField] private MovingPlatformSurface _currentPlatform;

    private Vector3 _localPlatformPoint;
    private Quaternion _lastPlatformRotation;

    private void Awake()
    {
        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        MovingPlatformSurface support = FindCurrentSupport();

        if (support == null)
        {
            _currentPlatform = null;
            return;
        }

        if (support != _currentPlatform)
        {
            AttachToPlatform(support);
            return;
        }

        ApplyPlatformDelta();
        RefreshPlatformCache();
    }

    private MovingPlatformSurface FindCurrentSupport()
    {
        if (_characterController == null || !_characterController.isGrounded)
            return null;

        Vector3 worldCenter = transform.position + _characterController.center;
        float probeRadius = Mathf.Max(0.02f, _characterController.radius * _probeRadiusScale);
        float innerHeight = Mathf.Max(0f, (_characterController.height * 0.5f) - probeRadius);

        Vector3 castOrigin = worldCenter + Vector3.up * 0.05f;
        float castDistance = innerHeight + _probeExtraDistance;

        if (Physics.SphereCast(
                castOrigin,
                probeRadius,
                Vector3.down,
                out RaycastHit hit,
                castDistance,
                _groundMask,
                QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < _minGroundNormalY)
                return null;

            return hit.collider.GetComponentInParent<MovingPlatformSurface>();
        }

        return null;
    }

    private void AttachToPlatform(MovingPlatformSurface platform)
    {
        _currentPlatform = platform;
        RefreshPlatformCache();
    }

    private void ApplyPlatformDelta()
    {
        if (_currentPlatform == null)
            return;

        Transform platformTransform = _currentPlatform.transform;

        Vector3 targetWorldPoint = platformTransform.TransformPoint(_localPlatformPoint);
        Vector3 platformDelta = targetWorldPoint - transform.position;

        if (platformDelta.sqrMagnitude > 0.0000001f)
            _characterController.Move(platformDelta);

        if (_currentPlatform.CarryRotation)
        {
            Quaternion rotationDelta = platformTransform.rotation * Quaternion.Inverse(_lastPlatformRotation);

            if (_currentPlatform.YawOnly)
            {
                float yawDelta = Mathf.DeltaAngle(0f, rotationDelta.eulerAngles.y);
                transform.Rotate(0f, yawDelta, 0f, Space.World);
            }
            else
            {
                transform.rotation = rotationDelta * transform.rotation;
            }
        }
    }

    private void RefreshPlatformCache()
    {
        if (_currentPlatform == null)
            return;

        Transform platformTransform = _currentPlatform.transform;
        _localPlatformPoint = platformTransform.InverseTransformPoint(transform.position);
        _lastPlatformRotation = platformTransform.rotation;
    }
}