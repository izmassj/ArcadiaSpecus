using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraDrag : MonoBehaviour
{
    [Header("Cinemachine Components")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private CinemachineConfiner2D confiner2D;

    [Header("Camera Reference")]
    [SerializeField] private Camera _mainCamera;

    [Header("Controller Drag Settings")]
    [SerializeField] private float _controllerSensitivity = 10f;
    [SerializeField] private float _stickDeadzone = 0.08f;

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerBunkerInputAction;

    private Vector3 _dragStartPosition;
    private Vector3 _cameraStartPosition;
    private bool _isDragging;

    private InputActionMap _gameplayInputActionMap;
    private InputAction _dragInputAction;
    private InputAction _navigateInputAction;

    private Transform _vcamTransform;

    private void Awake()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineCamera>();

        if (virtualCamera == null)
        {
            enabled = false;
            return;
        }

        virtualCamera.Follow = null;
        virtualCamera.LookAt = null;

        _vcamTransform = virtualCamera.transform;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_playerBunkerInputAction != null)
        {
            _gameplayInputActionMap = _playerBunkerInputAction.FindActionMap("Gameplay", true);
            _dragInputAction = _gameplayInputActionMap.FindAction("Drag", true);
            _navigateInputAction = _gameplayInputActionMap.FindAction("Navigate", true);
        }
    }

    private void OnEnable()
    {
        if (_playerBunkerInputAction != null)
            _playerBunkerInputAction.Enable();

        if (_dragInputAction != null)
        {
            _dragInputAction.started += DragStarted;
            _dragInputAction.canceled += DragCanceled;
        }
    }

    private void OnDisable()
    {
        if (_dragInputAction != null)
        {
            _dragInputAction.started -= DragStarted;
            _dragInputAction.canceled -= DragCanceled;
        }

        if (_playerBunkerInputAction != null)
            _playerBunkerInputAction.Disable();
    }

    private void DragStarted(InputAction.CallbackContext ctx) => StartDrag();
    private void DragCanceled(InputAction.CallbackContext ctx) => EndDrag();

    public void OnDrag(InputAction.CallbackContext ctx)
    {
        if (ctx.started) StartDrag();
        if (ctx.canceled) EndDrag();
    }

    private void StartDrag()
    {
        if (_mainCamera == null || _vcamTransform == null)
            return;

        _isDragging = true;
        _dragStartPosition = GetPointerWorldPosition();
        _cameraStartPosition = _vcamTransform.position;
    }

    private void EndDrag()
    {
        _isDragging = false;
    }

    private void LateUpdate()
    {
        if (_vcamTransform == null)
            return;

        if (_isDragging)
            HandlePointerDrag();

        HandleNavigatePan();
    }

    private void HandlePointerDrag()
    {
        if (_mainCamera == null)
            return;

        Vector3 currentPointerPos = GetPointerWorldPosition();
        Vector3 difference = _dragStartPosition - currentPointerPos;
        Vector3 targetPosition = _cameraStartPosition + difference;

        targetPosition = ApplyConfinement(targetPosition);
        _vcamTransform.position = targetPosition;
    }

    private void HandleNavigatePan()
    {
        if (_navigateInputAction == null)
            return;

        Vector2 nav = _navigateInputAction.ReadValue<Vector2>();

        if (nav.sqrMagnitude < _stickDeadzone * _stickDeadzone)
            return;

        Vector3 move = new Vector3(nav.x, -nav.y, 0f) * _controllerSensitivity * Time.deltaTime;
        Vector3 targetPosition = _vcamTransform.position + move;

        targetPosition = ApplyConfinement(targetPosition);
        _vcamTransform.position = targetPosition;
    }

    private Vector3 GetPointerWorldPosition()
    {
        if (_mainCamera == null || Pointer.current == null)
            return _vcamTransform != null ? _vcamTransform.position : Vector3.zero;

        Vector2 pos = Pointer.current.position.ReadValue();
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(pos.x, pos.y, 730f));
        return new Vector3(worldPos.x, worldPos.y, _vcamTransform.position.z);
    }

    private Vector3 ApplyConfinement(Vector3 desiredPosition)
    {
        if (confiner2D == null || confiner2D.BoundingShape2D == null)
            return desiredPosition;

        Collider2D boundingShape = confiner2D.BoundingShape2D;
        Vector2 desiredPos2D = new Vector2(desiredPosition.x, desiredPosition.y);

        if (!boundingShape.bounds.Contains(desiredPos2D))
        {
            Vector2 clampedPos = boundingShape.bounds.ClosestPoint(desiredPos2D);
            return new Vector3(clampedPos.x, clampedPos.y, desiredPosition.z);
        }

        return desiredPosition;
    }
}