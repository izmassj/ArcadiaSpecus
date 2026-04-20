using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RobotController : MonoBehaviour
{
    private enum JumpAnimationPhase
    {
        None,
        Start,
        Middle,
        Air,
        Fall
    }

    [Header("References")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _firstPersonPitchPivot;
    [SerializeField] private Transform _firstPersonCameraTarget;
    [SerializeField] private CharacterControllerPlatformMotor _platformMotor;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _groundCheckOrigin;

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

    [Header("Jump Animation")]
    [SerializeField] private bool _useAnimatedJumpFlow = true;
    [SerializeField] private LayerMask _nearGroundMask = ~0;
    [SerializeField] private float _nearGroundDistance = 0.9f;
    [SerializeField] private float _groundCheckOffset = 0.05f;
    [SerializeField] private float _stateCrossFade = 0.05f;
    [SerializeField] private float _jumpStartFallbackLength = 0.625f;
    [SerializeField] private float _jumpMiddleFallbackLength = 0.5833333f;
    [SerializeField] private float _jumpFallReturnDelay = 0.12f;
    [SerializeField] private string _idleStateName = "Idle";
    [SerializeField] private string _jumpStartStateName = "JumpStart";
    [SerializeField] private string _jumpMiddleStateName = "JumpMiddle";
    [SerializeField] private string _jumpAirStateName = "JumpAir";
    [SerializeField] private string _jumpFallStateName = "JumpFall";
    [SerializeField] private string _startJumpTriggerName = "StartJump";
    [SerializeField] private string _middleJumpTriggerName = "MiddleJump";
    [SerializeField] private string _airJumpTriggerName = "AirJump";
    [SerializeField] private string _fallJumpTriggerName = "FallJump";
    [SerializeField] private string _jumpingBoolName = "Jumping";

    [Header("Mode")]
    [SerializeField] private bool _isFirstPerson = false;

    [Header("External Lock")]
    [SerializeField] private bool _inputLocked;
    [SerializeField] private bool _hardMovementLocked;

    [Header("Debug")]
    [SerializeField] private bool _isGrounded;
    [SerializeField] private bool _isNearGround;
    [SerializeField] private Vector3 _worldVelocity;
    [SerializeField] private Vector3 _localPlanarVelocity;
    [SerializeField] private string _currentJumpPhase;

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

    private JumpAnimationPhase _jumpAnimationPhase;
    private float _jumpPhaseTimer;
    private float _jumpStartLength;
    private float _jumpMiddleLength;
    private bool _hasJumpingBool;
    private bool _hasStartJumpTrigger;
    private bool _hasMiddleJumpTrigger;
    private bool _hasAirJumpTrigger;
    private bool _hasFallJumpTrigger;

    public bool IsGrounded => _isGrounded;
    public bool IsFirstPerson => _isFirstPerson;
    public Vector3 WorldVelocity => _worldVelocity;
    public Vector3 LocalPlanarVelocity => _localPlanarVelocity;
    public float MaxMoveSpeed => _moveSpeed;
    public float Speed01 => Mathf.Clamp01(Mathf.Abs(_forwardSpeed) / Mathf.Max(0.01f, _moveSpeed));
    public float JumpImpulse01 { get; private set; }
    public float LandingImpulse01 { get; private set; }
    public CinemachineCamera ThirdPersonCamera => _thirdPersonCamera;
    public CinemachineCamera FirstPersonCamera => _firstPersonCamera;

    private void Awake()
    {
        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();

        if (_platformMotor == null)
            _platformMotor = GetComponent<CharacterControllerPlatformMotor>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);

        CacheAnimatorData();
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
        SyncAnimatorToIdle();
    }

    private void OnDisable()
    {
        if (_gameplayMap != null && _gameplayMap.enabled)
            _gameplayMap.Disable();

        if (_animator != null && _hasJumpingBool)
            _animator.SetBool(_jumpingBoolName, false);

        if (!_lockCursorInFirstPerson)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (_hardMovementLocked)
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _forwardSpeed = Mathf.MoveTowards(_forwardSpeed, 0f, _deceleration * Time.deltaTime);
            UpdateRuntimeState();
            FadeImpulses();
            return;
        }

        ReadInput();

        if (_inputLocked)
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
        }
        else
        {
            HandlePerspectiveToggle();
            HandleLook();
        }

        HandleMovement();
        UpdateRuntimeState();
        FadeImpulses();
        _currentJumpPhase = _jumpAnimationPhase.ToString();
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

    private void HandleMovement()
    {
        if (_platformMotor != null)
            _platformMotor.PreCharacterMove();

        bool groundedBeforeMove = _characterController.isGrounded;

        if (groundedBeforeMove && _verticalVelocity < 0f)
            _verticalVelocity = _groundedVerticalVelocity;

        if (_useAnimatedJumpFlow)
            UpdateJumpAnimationBeforeMove(groundedBeforeMove);
        else
            HandleImmediateJump(groundedBeforeMove);

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

        Vector3 frameDisplacement = motion * Time.deltaTime;

        if (_platformMotor != null)
            frameDisplacement += _platformMotor.FrameDisplacement;

        _characterController.Move(frameDisplacement);

        if (_platformMotor != null)
            _platformMotor.PostCharacterMove();

        _isGrounded = _characterController.isGrounded;
        _isNearGround = CheckNearGround();

        if (_isGrounded && !_wasGrounded)
        {
            float landing01 = Mathf.InverseLerp(2f, 14f, Mathf.Abs(_mostNegativeFallVelocity));
            LandingImpulse01 = Mathf.Max(LandingImpulse01, landing01);
            _mostNegativeFallVelocity = 0f;
        }

        if (!_isGrounded && _wasGrounded)
            _mostNegativeFallVelocity = 0f;

        if (_useAnimatedJumpFlow)
            UpdateJumpAnimationAfterMove();

        _wasGrounded = _isGrounded;
    }

    private void HandleImmediateJump(bool groundedBeforeMove)
    {
        if (_jumpAction == null || !_jumpAction.WasPressedThisFrame() || !groundedBeforeMove)
            return;

        DoTakeoff();
    }

    private void UpdateJumpAnimationBeforeMove(bool groundedBeforeMove)
    {
        if (_jumpAnimationPhase != JumpAnimationPhase.None)
            _jumpPhaseTimer += Time.deltaTime;

        if (_jumpAction != null && _jumpAction.WasPressedThisFrame() && groundedBeforeMove && _jumpAnimationPhase == JumpAnimationPhase.None)
        {
            EnterJumpPhase(JumpAnimationPhase.Start);
            return;
        }

        if (_jumpAnimationPhase != JumpAnimationPhase.Start)
            return;

        if (!HasStateFinished(_jumpStartStateName, _jumpStartFallbackLength))
            return;

        DoTakeoff();
        EnterJumpPhase(JumpAnimationPhase.Middle);
    }

    private void UpdateJumpAnimationAfterMove()
    {
        switch (_jumpAnimationPhase)
        {
            case JumpAnimationPhase.Middle:
                if (HasStateFinished(_jumpMiddleStateName, _jumpMiddleFallbackLength))
                    EnterJumpPhase(JumpAnimationPhase.Air);
                break;

            case JumpAnimationPhase.Air:
                if (!_isGrounded && _verticalVelocity <= 0f && _isNearGround)
                    EnterJumpPhase(JumpAnimationPhase.Fall);
                else if (_isGrounded)
                    FinishJumpAnimation();
                break;

            case JumpAnimationPhase.Fall:
                if (_isGrounded && _jumpPhaseTimer >= _jumpFallReturnDelay)
                    FinishJumpAnimation();
                break;
        }
    }

    private void EnterJumpPhase(JumpAnimationPhase phase)
    {
        _jumpAnimationPhase = phase;
        _jumpPhaseTimer = 0f;

        switch (phase)
        {
            case JumpAnimationPhase.Start:
                SetJumpingAnimator(true);
                PlayAnimatorJumpState(_jumpStartStateName, _startJumpTriggerName);
                break;

            case JumpAnimationPhase.Middle:
                SetJumpingAnimator(true);
                PlayAnimatorJumpState(_jumpMiddleStateName, _middleJumpTriggerName);
                break;

            case JumpAnimationPhase.Air:
                SetJumpingAnimator(true);
                PlayAnimatorJumpState(_jumpAirStateName, _airJumpTriggerName);
                break;

            case JumpAnimationPhase.Fall:
                SetJumpingAnimator(true);
                PlayAnimatorJumpState(_jumpFallStateName, _fallJumpTriggerName);
                break;
        }
    }

    private bool HasStateFinished(string stateName, float fallbackLength)
    {
        if (_animator == null || string.IsNullOrEmpty(stateName))
            return _jumpPhaseTimer >= fallbackLength;

        if (_animator.IsInTransition(0))
            return false;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        int stateHash = Animator.StringToHash(stateName);

        if (stateInfo.shortNameHash != stateHash && stateInfo.fullPathHash != stateHash)
            return _jumpPhaseTimer >= fallbackLength;

        return stateInfo.normalizedTime >= 1f;
    }

    private void FinishJumpAnimation()
    {
        _jumpAnimationPhase = JumpAnimationPhase.None;
        _jumpPhaseTimer = 0f;
        SetJumpingAnimator(false);
        ResetJumpTriggers();

        if (_animator != null && !string.IsNullOrEmpty(_idleStateName))
        {
            int idleHash = Animator.StringToHash(_idleStateName);
            if (_animator.HasState(0, idleHash))
                _animator.CrossFadeInFixedTime(idleHash, _stateCrossFade);
        }
    }

    private void DoTakeoff()
    {
        _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        JumpImpulse01 = 1f;

        if (_platformMotor != null)
            _platformMotor.ClearPlatform();
    }

    private void CacheAnimatorData()
    {
        if (_animator == null)
            return;

        _hasJumpingBool = HasAnimatorParameter(_jumpingBoolName, AnimatorControllerParameterType.Bool);
        _hasStartJumpTrigger = HasAnimatorParameter(_startJumpTriggerName, AnimatorControllerParameterType.Trigger);
        _hasMiddleJumpTrigger = HasAnimatorParameter(_middleJumpTriggerName, AnimatorControllerParameterType.Trigger);
        _hasAirJumpTrigger = HasAnimatorParameter(_airJumpTriggerName, AnimatorControllerParameterType.Trigger);
        _hasFallJumpTrigger = HasAnimatorParameter(_fallJumpTriggerName, AnimatorControllerParameterType.Trigger);

        _jumpStartLength = GetClipLength(_jumpStartStateName, _jumpStartFallbackLength);
        _jumpMiddleLength = GetClipLength(_jumpMiddleStateName, _jumpMiddleFallbackLength);
    }

    private void SyncAnimatorToIdle()
    {
        _jumpAnimationPhase = JumpAnimationPhase.None;
        _jumpPhaseTimer = 0f;
        SetJumpingAnimator(false);
        ResetJumpTriggers();

        if (_animator == null || string.IsNullOrEmpty(_idleStateName))
            return;

        int idleHash = Animator.StringToHash(_idleStateName);
        if (_animator.HasState(0, idleHash))
            _animator.Play(idleHash, 0, 0f);
    }

    private void PlayAnimatorJumpState(string stateName, string triggerName)
    {
        if (_animator == null)
            return;

        ResetJumpTriggers();

        if (!string.IsNullOrEmpty(triggerName))
        {
            if (triggerName == _startJumpTriggerName && _hasStartJumpTrigger)
                _animator.SetTrigger(triggerName);
            else if (triggerName == _middleJumpTriggerName && _hasMiddleJumpTrigger)
                _animator.SetTrigger(triggerName);
            else if (triggerName == _airJumpTriggerName && _hasAirJumpTrigger)
                _animator.SetTrigger(triggerName);
            else if (triggerName == _fallJumpTriggerName && _hasFallJumpTrigger)
                _animator.SetTrigger(triggerName);
        }

        if (string.IsNullOrEmpty(stateName))
            return;

        int stateHash = Animator.StringToHash(stateName);
        if (_animator.HasState(0, stateHash))
            _animator.CrossFadeInFixedTime(stateHash, _stateCrossFade);
    }

    private void ResetJumpTriggers()
    {
        if (_animator == null)
            return;

        if (_hasStartJumpTrigger)
            _animator.ResetTrigger(_startJumpTriggerName);
        if (_hasMiddleJumpTrigger)
            _animator.ResetTrigger(_middleJumpTriggerName);
        if (_hasAirJumpTrigger)
            _animator.ResetTrigger(_airJumpTriggerName);
        if (_hasFallJumpTrigger)
            _animator.ResetTrigger(_fallJumpTriggerName);
    }

    private void SetJumpingAnimator(bool value)
    {
        if (_animator != null && _hasJumpingBool)
            _animator.SetBool(_jumpingBoolName, value);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
    {
        if (_animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        var parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == expectedType)
                return true;
        }

        return false;
    }

    private float GetClipLength(string clipName, float fallback)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(clipName))
            return fallback;

        var clips = _animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == clipName)
                return Mathf.Max(0.01f, clip.length);
        }

        return fallback;
    }

    private bool CheckNearGround()
    {
        Vector3 origin;

        if (_groundCheckOrigin != null)
        {
            origin = _groundCheckOrigin.position;
        }
        else if (_characterController != null)
        {
            Bounds bounds = _characterController.bounds;
            origin = new Vector3(bounds.center.x, bounds.min.y + _groundCheckOffset, bounds.center.z);
        }
        else
        {
            origin = transform.position + Vector3.up * _groundCheckOffset;
        }

        return Physics.Raycast(
            origin,
            Vector3.down,
            _nearGroundDistance,
            _nearGroundMask,
            QueryTriggerInteraction.Ignore
        );
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

    public void SetInputLocked(bool locked, bool clearMotion)
    {
        _inputLocked = locked;

        if (!clearMotion)
            return;

        _moveInput = Vector2.zero;
        _lookInput = Vector2.zero;
        _forwardSpeed = 0f;
    }

    public void SetMovementLocked(bool locked, bool snapVelocity)
    {
        _hardMovementLocked = locked;

        if (!snapVelocity)
            return;

        _moveInput = Vector2.zero;
        _lookInput = Vector2.zero;
        _forwardSpeed = 0f;
        _verticalVelocity = _characterController != null && _characterController.isGrounded ? _groundedVerticalVelocity : 0f;
        _worldVelocity = Vector3.zero;
        _localPlanarVelocity = Vector3.zero;
    }

    public void ForceThirdPerson(bool instant)
    {
        if (!_isFirstPerson)
            return;

        _isFirstPerson = false;
        ApplyPerspectiveState(instant);
    }

    public void ForcePerspective(bool firstPerson, bool instant)
    {
        _isFirstPerson = firstPerson;
        ApplyPerspectiveState(instant);
    }
}
