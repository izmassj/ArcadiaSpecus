using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RobotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController _characterController;

    [Header("Input")]
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private string _gameplayMapName = "Gameplay";
    [SerializeField] private string _navigateActionName = "Navigate";
    [SerializeField] private string _jumpActionName = "Jump";

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _acceleration = 18f;
    [SerializeField] private float _deceleration = 24f;
    [SerializeField] private float _rotationSpeed = 180f;

    [Header("Jump")]
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _gravity = -25f;
    [SerializeField] private float _groundedVerticalVelocity = -2f;

    [Header("Debug")]
    [SerializeField] private bool _isGrounded;
    [SerializeField] private Vector3 _worldVelocity;
    [SerializeField] private Vector3 _localPlanarVelocity;

    private InputActionMap _gameplayMap;
    private InputAction _navigateAction;
    private InputAction _jumpAction;

    private Vector2 _moveInput;
    private float _forwardSpeed;
    private float _verticalVelocity;
    private bool _wasGrounded;
    private float _mostNegativeFallVelocity;
    private Vector3 _previousPosition;

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
    }

    private void OnDisable()
    {
        if (_gameplayMap != null && _gameplayMap.enabled)
            _gameplayMap.Disable();
    }

    private void Update()
    {
        ReadInput();
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

        if (!_gameplayMap.enabled)
            _gameplayMap.Enable();
    }

    private void ReadInput()
    {
        _moveInput = _navigateAction != null
            ? _navigateAction.ReadValue<Vector2>()
            : Vector2.zero;
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

        float targetForwardSpeed = -_moveInput.y * _moveSpeed;
        float speedRate = Mathf.Abs(_moveInput.y) > 0.001f ? _acceleration : _deceleration;

        _forwardSpeed = Mathf.MoveTowards(
            _forwardSpeed,
            targetForwardSpeed,
            speedRate * Time.deltaTime
        );

        float yawInput = _moveInput.x;
        transform.Rotate(Vector3.up, yawInput * _rotationSpeed * Time.deltaTime, Space.Self);

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
}