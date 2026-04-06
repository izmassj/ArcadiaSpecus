using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RobotController : MonoBehaviour
{
    [Header("External Lock")]
    [SerializeField] private bool _movementLocked;

    [Header("References")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _firstPersonPitchPivot;
    [SerializeField] private Transform _firstPersonCameraTarget;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera _thirdPersonCamera;
    [SerializeField] private CinemachineCamera _firstPersonCamera;
    [SerializeField] private int _activeCameraPriority = 20;
    [SerializeField] private int _inactiveCameraPriority = 10;

    [Header("Input")]
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private string _gameplayMapName = "Gameplay";
    [SerializeField] private string _navigateActionName = "Navigate";
    [SerializeField] private string _jumpActionName = "Jump";
    [SerializeField] private string _lookActionName = "Look";
    [SerializeField] private string _changePerspectiveActionName = "ChangePerspective";

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _acceleration = 18f;
    [SerializeField] private float _deceleration = 24f;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private bool _invertForwardInput = true;

    [Header("First Person Look")]
    [SerializeField] private float _mouseYawSpeed = 0.18f;
    [SerializeField] private float _mousePitchSpeed = 0.18f;
    [SerializeField] private bool _invertLookY = false;
    [SerializeField] private float _minPitch = -75f;
    [SerializeField] private float _maxPitch = 75f;
    [SerializeField] private bool _lockCursorInFirstPerson = true;

    [Header("Jump")]
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _gravity = -25f;
    [SerializeField] private float _groundedVerticalVelocity = -2f;

    [Header("Mode")]
    [SerializeField] private bool _isFirstPerson = false;

    [Header("Debug")]
    [SerializeField] private bool _isGrounded;
    [SerializeField] private Vector3 _worldVelocity;
    [SerializeField] private Vector3 _localPlanarVelocity;

    private InputActionMap _gameplayMap;
    private InputAction _navigateAction;
    private InputAction _jumpAction;
    private InputAction _lookAction;
    private InputAction _changePerspectiveAction;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private float _forwardSpeed;
    private float _verticalVelocity;
    private bool _wasGrounded;
    private float _mostNegativeFallVelocity;
    private Vector3 _previousPosition;
    private float _pitch;

    public bool IsGrounded => _isGrounded;
    public Vector3 WorldVelocity => _worldVelocity;
    public Vector3 LocalPlanarVelocity => _localPlanarVelocity;
    public float MaxMoveSpeed => _moveSpeed;
    public float Speed01 => Mathf.Clamp01(Mathf.Abs(_forwardSpeed) / Mathf.Max(0.01f, _moveSpeed));
    public float JumpImpulse01 { get; private set; }
    public float LandingImpulse01 { get; private set; }

    private void Awake()
    {
        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        StartInputActions();
    }

    private void Start()
    {
        _previousPosition = transform.position;

        if (_firstPersonPitchPivot != null)
            _pitch = NormalizePitch(_firstPersonPitchPivot.localEulerAngles.x);

        ApplyPerspectiveState(true);
    }

    private void OnDisable()
    {
        if (_gameplayMap != null && _gameplayMap.enabled)
            _gameplayMap.Disable();

        if (!_lockCursorInFirstPerson)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (_movementLocked)
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _forwardSpeed = Mathf.MoveTowards(_forwardSpeed, 0f, _deceleration * Time.deltaTime);
            UpdateRuntimeState();
            FadeImpulses();
            return;
        }

        ReadInput();
        HandlePerspectiveToggle();
        HandleLook();
        HandleMovement();
        UpdateRuntimeState();
        FadeImpulses();
    }


    private void StartInputActions()
    {
        if (_inputActions == null)
            return;

        _gameplayMap = _inputActions.FindActionMap(_gameplayMapName, true);
        _navigateAction = _gameplayMap.FindAction(_navigateActionName, true);
        _jumpAction = _gameplayMap.FindAction(_jumpActionName, true);
        _lookAction = _gameplayMap.FindAction(_lookActionName, true);
        _changePerspectiveAction = _gameplayMap.FindAction(_changePerspectiveActionName, true);

        if (!_gameplayMap.enabled)
            _gameplayMap.Enable();
    }

    private void ReadInput()
    {
        _moveInput = _navigateAction != null ? _navigateAction.ReadValue<Vector2>() : Vector2.zero;
        _lookInput = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private void HandlePerspectiveToggle()
    {
        if (_changePerspectiveAction == null || !_changePerspectiveAction.WasPressedThisFrame())
            return;

        _isFirstPerson = !_isFirstPerson;
        ApplyPerspectiveState(false);
    }

    private void ApplyPerspectiveState(bool instant)
    {
        if (_firstPersonCamera != null)
            _firstPersonCamera.Priority = _isFirstPerson ? _activeCameraPriority : _inactiveCameraPriority;

        if (_thirdPersonCamera != null)
            _thirdPersonCamera.Priority = _isFirstPerson ? _inactiveCameraPriority : _activeCameraPriority;

        if (_lockCursorInFirstPerson)
        {
            Cursor.lockState = _isFirstPerson ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_isFirstPerson;
        }

        if (!_isFirstPerson && _firstPersonPitchPivot != null)
            _firstPersonPitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleLook()
    {
        if (!_isFirstPerson)
            return;

        transform.Rotate(Vector3.up, _lookInput.x * _mouseYawSpeed, Space.Self);

        float pitchSign = _invertLookY ? 1f : -1f;
        _pitch += _lookInput.y * _mousePitchSpeed * pitchSign;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        if (_firstPersonPitchPivot != null)
            _firstPersonPitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    public void SetMovementLocked(bool locked, bool snapVelocity)
    {
        _movementLocked = locked;

        if (!snapVelocity)
            return;

        _moveInput = Vector2.zero;
        _lookInput = Vector2.zero;
        _forwardSpeed = 0f;
        _verticalVelocity = _characterController != null && _characterController.isGrounded ? _groundedVerticalVelocity : 0f;
        _worldVelocity = Vector3.zero;
        _localPlanarVelocity = Vector3.zero;
    }

    private void HandleMovement()
    {
        bool groundedBeforeMove = _characterController.isGrounded;

        if (groundedBeforeMove && _verticalVelocity < 0f)
            _verticalVelocity = _groundedVerticalVelocity;

        if (_jumpAction != null && _jumpAction.WasPressedThisFrame() && groundedBeforeMove)
        {
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            JumpImpulse01 = 1f;
        }

        float forwardSign = _invertForwardInput ? -1f : 1f;
        float targetForwardSpeed = _moveInput.y * forwardSign * _moveSpeed;
        float speedRate = Mathf.Abs(_moveInput.y) > 0.001f ? _acceleration : _deceleration;

        _forwardSpeed = Mathf.MoveTowards(
            _forwardSpeed,
            targetForwardSpeed,
            speedRate * Time.deltaTime
        );

        transform.Rotate(Vector3.up, _moveInput.x * _rotationSpeed * Time.deltaTime, Space.Self);

        if (!groundedBeforeMove)
            _mostNegativeFallVelocity = Mathf.Min(_mostNegativeFallVelocity, _verticalVelocity);

        _verticalVelocity += _gravity * Time.deltaTime;

        Vector3 motion = transform.forward * _forwardSpeed;
        motion.y = _verticalVelocity;

        _characterController.Move(motion * Time.deltaTime);

        _isGrounded = _characterController.isGrounded;

        if (_isGrounded && !_wasGrounded)
        {
            float landing01 = Mathf.InverseLerp(2f, 14f, Mathf.Abs(_mostNegativeFallVelocity));
            LandingImpulse01 = Mathf.Max(LandingImpulse01, landing01);
            _mostNegativeFallVelocity = 0f;
        }

        if (!_isGrounded && _wasGrounded)
            _mostNegativeFallVelocity = 0f;

        _wasGrounded = _isGrounded;
    }

    private void UpdateRuntimeState()
    {
        _worldVelocity = (transform.position - _previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        _previousPosition = transform.position;

        Vector3 planar = _worldVelocity;
        planar.y = 0f;
        _localPlanarVelocity = transform.InverseTransformDirection(planar);
    }

    private void FadeImpulses()
    {
        JumpImpulse01 = Mathf.MoveTowards(JumpImpulse01, 0f, 6f * Time.deltaTime);
        LandingImpulse01 = Mathf.MoveTowards(LandingImpulse01, 0f, 6f * Time.deltaTime);
    }

    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}