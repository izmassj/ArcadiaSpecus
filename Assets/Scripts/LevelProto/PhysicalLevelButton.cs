using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicalLevelButton : MonoBehaviour, ILevelResettable
{
    [Header("Interaction")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _toggleEachPress;
    [SerializeField] private bool _startsPressed;

    [Header("Targets")]
    [SerializeField] private MovingWall _controlledWall;
    [SerializeField] private Animator _animator;
    [SerializeField] private string _animatorPressedBool = "Pressed";

    private bool _playerInside;
    private bool _isPressed;
    private Tween _buttonTween;

    private Vector3 _initialLocalPosition;

    private void Awake()
    {
        _isPressed = _startsPressed;
        _initialLocalPosition = transform.localPosition;
    }

    private void OnDisable()
    {
        _buttonTween?.Kill();
    }

    private void Update()
    {
        if (!_playerInside)
            return;

        PressButton();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_playerTag))
            _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_playerTag))
            _playerInside = false;
    }

    public void ResetLevelState()
    {
        transform.DOKill();
        transform.localPosition = _initialLocalPosition;
        _isPressed = false;
    }

    public void PressButton()
    {
        if (_toggleEachPress)
            _isPressed = !_isPressed;
        else
            _isPressed = true;

        if (_animator != null && !string.IsNullOrWhiteSpace(_animatorPressedBool))
            _animator.SetBool(_animatorPressedBool, _isPressed);

        if (_controlledWall != null)
            _controlledWall.SetLowered(_isPressed);
    }
}
