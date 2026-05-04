using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerPlatformMotor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController _characterController;

    [Header("Probe")]
    [SerializeField] private LayerMask _supportMask = ~0;
    [SerializeField] private float _probeStartOffset = 0.08f;
    [SerializeField] private float _probeDistance = 0.35f;
    [SerializeField][Range(0.1f, 1f)] private float _probeRadiusScale = 0.9f;
    [SerializeField] private float _groundGraceTime = 0.12f;
    [SerializeField][Range(0.01f, 1f)] private float _minGroundNormalY = 0.35f;

    [Header("Debug")]
    [SerializeField] private MovingPlatformSurface _currentPlatform;
    [SerializeField] private Vector3 _frameDisplacement;

    private Vector3 _localPlatformPoint;
    private Quaternion _lastPlatformRotation = Quaternion.identity;
    private float _lastSupportedTime = -999f;

    public Vector3 FrameDisplacement => _frameDisplacement;
    public MovingPlatformSurface CurrentPlatform => _currentPlatform;
    public bool HasCurrentPlatform => _currentPlatform != null;

    private void Awake()
    {
        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();
    }

    public void PreCharacterMove()
    {
        _frameDisplacement = Vector3.zero;

        if (_currentPlatform == null)
            return;

        bool keepByGrace = Time.time - _lastSupportedTime <= _groundGraceTime;

        if (!_characterController.isGrounded && !keepByGrace)
        {
            ClearPlatform();
            return;
        }

        Transform platformTransform = _currentPlatform.transform;

        Vector3 targetWorldPoint = platformTransform.TransformPoint(_localPlatformPoint);
        _frameDisplacement = targetWorldPoint - transform.position;

        if (_currentPlatform.CarryRotation)
        {
            Quaternion deltaRotation = platformTransform.rotation * Quaternion.Inverse(_lastPlatformRotation);

            if (_currentPlatform.YawOnly)
            {
                float yawDelta = Mathf.DeltaAngle(0f, deltaRotation.eulerAngles.y);
                if (Mathf.Abs(yawDelta) > 0.0001f)
                    transform.Rotate(0f, yawDelta, 0f, Space.World);
            }
            else
            {
                transform.rotation = deltaRotation * transform.rotation;
            }
        }
    }

    public void PostCharacterMove()
    {
        MovingPlatformSurface support = ProbeCurrentSupport();

        if (support != null)
        {
            SetPlatform(support);
            return;
        }

        bool keepByGrace = Time.time - _lastSupportedTime <= _groundGraceTime;

        if (_characterController.isGrounded && !keepByGrace)
            ClearPlatform();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < _minGroundNormalY)
            return;

        if (hit.moveDirection.y > 0.3f)
            return;

        MovingPlatformSurface platform = hit.collider.GetComponentInParent<MovingPlatformSurface>();

        if (platform == null)
            return;

        SetPlatform(platform);
    }

    private MovingPlatformSurface ProbeCurrentSupport()
    {
        if (_characterController == null)
            return null;

        Bounds bounds = _characterController.bounds;

        float radius = Mathf.Max(0.02f, Mathf.Min(bounds.extents.x, bounds.extents.z) * _probeRadiusScale);

        Vector3 origin = new Vector3(
            bounds.center.x,
            bounds.min.y + radius + _probeStartOffset,
            bounds.center.z
        );

        float distance = _probeDistance + _probeStartOffset;

        if (Physics.SphereCast(
                origin,
                radius,
                Vector3.down,
                out RaycastHit hit,
                distance,
                _supportMask,
                QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < _minGroundNormalY)
                return null;

            return hit.collider.GetComponentInParent<MovingPlatformSurface>();
        }

        return null;
    }

    private void SetPlatform(MovingPlatformSurface platform)
    {
        _currentPlatform = platform;
        _localPlatformPoint = platform.transform.InverseTransformPoint(transform.position);
        _lastPlatformRotation = platform.transform.rotation;
        _lastSupportedTime = Time.time;
    }

    public void RefreshPlatformAnchorAfterTeleport()
    {
        if (_currentPlatform == null)
            return;

        _localPlatformPoint = _currentPlatform.transform.InverseTransformPoint(transform.position);
        _lastPlatformRotation = _currentPlatform.transform.rotation;
        _frameDisplacement = Vector3.zero;
    }

    public void ClearPlatform()
    {
        _currentPlatform = null;
        _frameDisplacement = Vector3.zero;
    }
}