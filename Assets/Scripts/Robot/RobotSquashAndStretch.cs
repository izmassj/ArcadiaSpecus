using UnityEngine;

public class RobotSquashAndStretch : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotController _controller;
    [SerializeField] private Transform _visualRoot;

    [Header("Toggles")]
    [SerializeField] private bool _affectMove = true;
    [SerializeField] private bool _affectLean = true;
    [SerializeField] private bool _affectJump = true;
    [SerializeField] private bool _affectLanding = true;

    [Header("Move Response")]
    [SerializeField] private float _moveSquashY = 0.04f;
    [SerializeField] private float _moveStretchZ = 0.06f;

    [Header("Jump / Land Response")]
    [SerializeField] private float _jumpStretchY = 0.08f;
    [SerializeField] private float _jumpSquashXZ = 0.03f;
    [SerializeField] private float _landingSquashY = 0.12f;
    [SerializeField] private float _landingStretchXZ = 0.05f;
    [SerializeField] private float _landingDrop = 0.06f;

    [Header("Lean")]
    [SerializeField] private float _maxPitch = 8f;
    [SerializeField] private float _maxRoll = 6f;

    [Header("Smoothing")]
    [SerializeField] private float _scaleSharpness = 12f;
    [SerializeField] private float _rotationSharpness = 10f;
    [SerializeField] private float _positionSharpness = 10f;

    private Vector3 _baseLocalScale;
    private Quaternion _baseLocalRotation;
    private Vector3 _baseLocalPosition;

    private Vector3 _currentScale;
    private Quaternion _currentRotation;
    private Vector3 _currentPosition;

    private void Awake()
    {
        if (_visualRoot == null)
            _visualRoot = transform;

        _baseLocalScale = _visualRoot.localScale;
        _baseLocalRotation = _visualRoot.localRotation;
        _baseLocalPosition = _visualRoot.localPosition;

        _currentScale = _baseLocalScale;
        _currentRotation = _baseLocalRotation;
        _currentPosition = _baseLocalPosition;
    }

    private void LateUpdate()
    {
        if (_controller == null || _visualRoot == null)
            return;

        float speed01 = _affectMove ? _controller.Speed01 : 0f;
        float jump01 = _affectJump ? _controller.JumpImpulse01 : 0f;
        float land01 = _affectLanding ? _controller.LandingImpulse01 : 0f;

        Vector3 targetScale = _baseLocalScale;
        Vector3 targetPosition = _baseLocalPosition;
        Quaternion targetRotation = _baseLocalRotation;

        if (_affectMove)
        {
            targetScale += new Vector3(
                -speed01 * _moveStretchZ * 0.15f,
                -speed01 * _moveSquashY,
                speed01 * _moveStretchZ
            );
        }

        if (_affectJump)
        {
            targetScale += new Vector3(
                -jump01 * _jumpSquashXZ,
                jump01 * _jumpStretchY,
                -jump01 * _jumpSquashXZ
            );
        }

        if (_affectLanding)
        {
            targetScale += new Vector3(
                land01 * _landingStretchXZ,
                -land01 * _landingSquashY,
                land01 * _landingStretchXZ
            );

            targetPosition += Vector3.down * (land01 * _landingDrop);
        }

        if (_affectLean)
        {
            Vector3 localVelocity = _controller.LocalPlanarVelocity;
            float maxSpeed = Mathf.Max(0.01f, _controller.MaxMoveSpeed);

            float forward = Mathf.Clamp(localVelocity.z / maxSpeed, -1f, 1f);
            float side = Mathf.Clamp(localVelocity.x / maxSpeed, -1f, 1f);

            Quaternion leanRotation = Quaternion.Euler(
                -forward * _maxPitch,
                0f,
                -side * _maxRoll
            );

            targetRotation = _baseLocalRotation * leanRotation;
        }

        _currentScale = DampVector(_currentScale, targetScale, _scaleSharpness);
        _currentPosition = DampVector(_currentPosition, targetPosition, _positionSharpness);
        _currentRotation = DampQuaternion(_currentRotation, targetRotation, _rotationSharpness);

        _visualRoot.localScale = _currentScale;
        _visualRoot.localPosition = _currentPosition;
        _visualRoot.localRotation = _currentRotation;
    }

    private Vector3 DampVector(Vector3 current, Vector3 target, float sharpness)
    {
        float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        return Vector3.Lerp(current, target, t);
    }

    private Quaternion DampQuaternion(Quaternion current, Quaternion target, float sharpness)
    {
        float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        return Quaternion.Slerp(current, target, t);
    }
}