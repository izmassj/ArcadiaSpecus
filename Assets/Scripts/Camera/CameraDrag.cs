using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CameraDrag : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera _virtualCamera;
    [SerializeField] private CinemachineConfiner2D _confiner2D;

    [Header("Camera")]
    [SerializeField] private Camera _mainCamera;

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerBunkerInputAction;

    private InputActionMap _gameplayInputActionMap;
    private InputAction _dragInputAction;
    private InputAction _navigateInputAction;

    [Header("Mouse Drag")]
    [SerializeField] private float _mouseSensitivity;
    [SerializeField] private bool _invert;

    [Header("Stick")]
    [SerializeField] private float _stickSpeed;
    [SerializeField] private float _stickDeadzone;

    [Header("Smooth")]
    [SerializeField] private float _sharpness;

    [Header("Slide")]
    [SerializeField] private bool _enableInertia;
    [SerializeField] private float _inertiaDecay;
    [SerializeField] private float _inertiaMaxSpeed;
    [SerializeField] private float _inertiaStopSpeed;

    private Vector3 _desiredPos;

    private bool _wasDragging;
    private Vector3 _inertiaVel;

    private void Awake()
    {
        if (_virtualCamera == null) _virtualCamera = GetComponent<CinemachineCamera>();
        if (_mainCamera == null) _mainCamera = Camera.main;

        _gameplayInputActionMap = _playerBunkerInputAction.FindActionMap("Gameplay", true);
        _dragInputAction = _gameplayInputActionMap.FindAction("Drag", true);
        _navigateInputAction = _gameplayInputActionMap.FindAction("Navigate", true);

        _desiredPos = _virtualCamera.transform.position;
    }

    private void OnEnable()
    {
        _gameplayInputActionMap.Enable();
    }

    private void OnDisable()
    {
        _gameplayInputActionMap.Disable();
    }

    private void Update()
    {
        if (_virtualCamera == null || _mainCamera == null) return;

        if ((_desiredPos - _virtualCamera.transform.position).sqrMagnitude > 10000f)
        {
            _desiredPos = _virtualCamera.transform.position;
            _inertiaVel = Vector3.zero;
        }

        bool dragging = _dragInputAction != null && _dragInputAction.IsPressed();

        if (dragging && Mouse.current != null)
        {
            if (GetStickIfGamepadActive() == Vector2.zero)
            {
                Vector2 deltaPx = Mouse.current.delta.ReadValue();

                float wppY = (2f * _mainCamera.orthographicSize) / Screen.height;
                float wppX = wppY * _mainCamera.aspect;

                float sign = _invert ? 1f : -1f;
                Vector3 deltaWorld = new Vector3(deltaPx.x * wppX, deltaPx.y * wppY, 0f) * (sign * _mouseSensitivity);

                _desiredPos += deltaWorld;

                float dt = Time.deltaTime;
                if (_enableInertia && dt > 0.00001f)
                {
                    Vector3 v = deltaWorld / dt;
                    if (v.magnitude > _inertiaMaxSpeed) v = v.normalized * _inertiaMaxSpeed;

                    _inertiaVel = Vector3.Lerp(_inertiaVel, v, 0.6f);
                }
            }
            else
            {
                Vector2 stick = GetStickIfGamepadActive();

                float wppY = (2f * _mainCamera.orthographicSize) / Screen.height;
                float wppX = wppY * _mainCamera.aspect;

                float sign = _invert ? 1f : -1f;
                Vector3 deltaWorld = new Vector3(stick.x * wppX, stick.y * wppY, 0f) * (sign * _stickSpeed) * 8;

                _desiredPos += deltaWorld;

                float dt = Time.deltaTime;
                if (_enableInertia && dt > 0.00001f)
                {
                    Vector3 v = deltaWorld / dt;
                    if (v.magnitude > _inertiaMaxSpeed) v = v.normalized * _inertiaMaxSpeed;

                    _inertiaVel = Vector3.Lerp(_inertiaVel, v, 0.6f);
                }
            }
        }

        _wasDragging = dragging;

        if (_confiner2D != null && _confiner2D.BoundingShape2D != null)
        {
            Vector3 before = _desiredPos;
            _desiredPos = ClampToBounds(_desiredPos, _confiner2D.BoundingShape2D.bounds);

            if (!Mathf.Approximately(before.x, _desiredPos.x)) _inertiaVel.x = 0f;
            if (!Mathf.Approximately(before.y, _desiredPos.y)) _inertiaVel.y = 0f;
        }

        Vector3 current = _virtualCamera.transform.position;
        _desiredPos.z = current.z;

        float t = 1f - Mathf.Exp(-_sharpness * Time.deltaTime);
        Vector3 next = Vector3.Lerp(current, _desiredPos, t);

        if (_confiner2D != null && _confiner2D.BoundingShape2D != null)
            next = ClampToBounds(next, _confiner2D.BoundingShape2D.bounds);

        next.z = current.z;
        _virtualCamera.transform.position = next;
    }

    private Vector2 GetStickIfGamepadActive()
    {
        if (_navigateInputAction == null) return Vector2.zero;

        for (int i = 0; i < _navigateInputAction.controls.Count; i++)
        {
            var control = _navigateInputAction.controls[i];

            if (control.device is Gamepad && control is StickControl stick)
                return stick.ReadValue();
        }

        return Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
    }

    private Vector3 ClampToBounds(Vector3 p, Bounds b)
    {
        if (_mainCamera.orthographic)
        {
            float halfH = _mainCamera.orthographicSize;
            float halfW = halfH * _mainCamera.aspect;

            p.x = Mathf.Clamp(p.x, b.min.x + halfW, b.max.x - halfW);
            p.y = Mathf.Clamp(p.y, b.min.y + halfH, b.max.y - halfH);
            return p;
        }

        p.x = Mathf.Clamp(p.x, b.min.x, b.max.x);
        p.y = Mathf.Clamp(p.y, b.min.y, b.max.y);
        return p;
    }
}