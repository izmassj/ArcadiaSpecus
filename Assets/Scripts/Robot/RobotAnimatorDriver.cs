using UnityEngine;

public class RobotAnimatorDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private bool _findAnimatorInChildren = true;

    [Header("Parameters")]
    [SerializeField] private string _jumpingBoolName = "Jumping";
    [SerializeField] private string _startJumpTriggerName = "StartJump";
    [SerializeField] private string _middleJumpTriggerName = "MiddleJump";
    [SerializeField] private string _airJumpTriggerName = "AirJump";
    [SerializeField] private string _fallJumpTriggerName = "FallJump";

    [Header("Timing")]
    [SerializeField] private float _middleMinAirTime = 0.04f;
    [SerializeField] private float _airMinAirTime = 0.08f;

    [Header("Velocity Thresholds")]
    [SerializeField] private float _middleVelocityThreshold = 0.15f;
    [SerializeField] private float _apexVelocityWindow = 0.35f;
    [SerializeField] private float _fallVelocityThreshold = -0.6f;

    private int _jumpingBoolHash;
    private int _startJumpTriggerHash;
    private int _middleJumpTriggerHash;
    private int _airJumpTriggerHash;
    private int _fallJumpTriggerHash;

    private bool _hasJumpingBool;
    private bool _hasStartJumpTrigger;
    private bool _hasMiddleJumpTrigger;
    private bool _hasAirJumpTrigger;
    private bool _hasFallJumpTrigger;

    private bool _jumpSequenceActive;
    private bool _leftGround;
    private bool _sentMiddle;
    private bool _sentAir;
    private bool _sentFall;
    private bool _lastGrounded;
    private float _airborneTime;

    private void Awake()
    {
        if (_controller == null)
            _controller = GetComponent<RobotController>();

        if (_animator == null && _findAnimatorInChildren)
            _animator = GetComponentInChildren<Animator>(true);

        CacheHashes();
        CacheParameterAvailability();
    }

    private void Start()
    {
        if (_controller == null)
            return;

        _lastGrounded = _controller.IsGrounded;
        SetJumping(false);
    }

    private void Update()
    {
        if (_controller == null || _animator == null)
            return;

        bool grounded = _controller.IsGrounded;
        float verticalSpeed = _controller.WorldVelocity.y;
        bool jumpPressedNow = _controller.JumpImpulse01 > 0.01f;

        if (!_jumpSequenceActive)
        {
            if (jumpPressedNow)
            {
                BeginJumpSequence(true);
            }
            else if (_lastGrounded && !grounded)
            {
                BeginJumpSequence(false);
                TriggerAirIfPossible();
            }
        }

        if (_jumpSequenceActive)
            UpdateJumpSequence(grounded, verticalSpeed);

        _lastGrounded = grounded;
    }

    private void BeginJumpSequence(bool fromJumpInput)
    {
        _jumpSequenceActive = true;
        _leftGround = false;
        _sentMiddle = false;
        _sentAir = false;
        _sentFall = false;
        _airborneTime = 0f;

        SetJumping(true);
        ResetJumpTriggers();

        if (fromJumpInput)
            TryTrigger(_startJumpTriggerHash, _hasStartJumpTrigger);
    }

    private void UpdateJumpSequence(bool grounded, float verticalSpeed)
    {
        if (grounded)
        {
            if (_leftGround)
                EndJumpSequence();

            return;
        }

        _leftGround = true;
        _airborneTime += Time.deltaTime;

        if (!_sentMiddle && _airborneTime >= _middleMinAirTime && verticalSpeed > _middleVelocityThreshold)
        {
            _sentMiddle = true;
            TryTrigger(_middleJumpTriggerHash, _hasMiddleJumpTrigger);
        }

        if (!_sentAir && _airborneTime >= _airMinAirTime && Mathf.Abs(verticalSpeed) <= _apexVelocityWindow)
        {
            _sentAir = true;
            TryTrigger(_airJumpTriggerHash, _hasAirJumpTrigger);
        }

        if (!_sentFall && verticalSpeed <= _fallVelocityThreshold)
        {
            if (!_sentAir)
                TriggerAirIfPossible();

            _sentFall = true;
            TryTrigger(_fallJumpTriggerHash, _hasFallJumpTrigger);
        }
    }

    private void EndJumpSequence()
    {
        _jumpSequenceActive = false;
        _leftGround = false;
        _sentMiddle = false;
        _sentAir = false;
        _sentFall = false;
        _airborneTime = 0f;

        SetJumping(false);
        ResetJumpTriggers();
    }

    private void TriggerAirIfPossible()
    {
        _sentAir = true;
        TryTrigger(_airJumpTriggerHash, _hasAirJumpTrigger);
    }

    private void SetJumping(bool value)
    {
        if (_animator == null || !_hasJumpingBool)
            return;

        _animator.SetBool(_jumpingBoolHash, value);
    }

    private void ResetJumpTriggers()
    {
        if (_animator == null)
            return;

        if (_hasStartJumpTrigger)
            _animator.ResetTrigger(_startJumpTriggerHash);

        if (_hasMiddleJumpTrigger)
            _animator.ResetTrigger(_middleJumpTriggerHash);

        if (_hasAirJumpTrigger)
            _animator.ResetTrigger(_airJumpTriggerHash);

        if (_hasFallJumpTrigger)
            _animator.ResetTrigger(_fallJumpTriggerHash);
    }

    private void TryTrigger(int hash, bool hasParameter)
    {
        if (_animator == null || !hasParameter)
            return;

        _animator.SetTrigger(hash);
    }

    private void CacheHashes()
    {
        _jumpingBoolHash = Animator.StringToHash(_jumpingBoolName);
        _startJumpTriggerHash = Animator.StringToHash(_startJumpTriggerName);
        _middleJumpTriggerHash = Animator.StringToHash(_middleJumpTriggerName);
        _airJumpTriggerHash = Animator.StringToHash(_airJumpTriggerName);
        _fallJumpTriggerHash = Animator.StringToHash(_fallJumpTriggerName);
    }

    private void CacheParameterAvailability()
    {
        _hasJumpingBool = HasParameter(_jumpingBoolName, AnimatorControllerParameterType.Bool);
        _hasStartJumpTrigger = HasParameter(_startJumpTriggerName, AnimatorControllerParameterType.Trigger);
        _hasMiddleJumpTrigger = HasParameter(_middleJumpTriggerName, AnimatorControllerParameterType.Trigger);
        _hasAirJumpTrigger = HasParameter(_airJumpTriggerName, AnimatorControllerParameterType.Trigger);
        _hasFallJumpTrigger = HasParameter(_fallJumpTriggerName, AnimatorControllerParameterType.Trigger);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (_animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        var parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == type && parameters[i].name == parameterName)
                return true;
        }

        return false;
    }
}
