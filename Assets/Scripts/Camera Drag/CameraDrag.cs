using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drag de cámara con ratón + desplazamiento con stick derecho (New Input System).
/// Mantiene OnDrag(InputAction.CallbackContext) para compatibilidad con PlayerInput.
/// </summary>
public class CameraDrag : MonoBehaviour
{
    [Header("Cinemachine Components")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private CinemachineConfiner2D confiner2D;

    [Header("Camera Reference")]
    [SerializeField] private Camera _mainCamera;

    [Header("Controller Drag Settings (New Input)")]
    [SerializeField] private float _controllerSensitivity = 10f;
    [SerializeField] private float _stickDeadzone = 0.08f;

    private Vector3 _dragStartPosition;
    private Vector3 _cameraStartPosition;
    private bool _isDragging;
    private Transform _cameraTarget;

    // Entrada opcional si queréis pasarla por callback desde PlayerInput (acción Look).
    private Vector2 _lookInputFromAction;

    private void Start()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineVirtualCamera>();

        if (virtualCamera == null)
        {
            Debug.LogError("CameraDrag: falta CinemachineVirtualCamera.");
            enabled = false;
            return;
        }

        if (virtualCamera.Follow == null)
        {
            GameObject targetObject = new GameObject("CameraDragTarget");
            targetObject.transform.position = virtualCamera.transform.position;
            virtualCamera.Follow = targetObject.transform;
            _cameraTarget = targetObject.transform;
        }
        else
        {
            _cameraTarget = virtualCamera.Follow;
        }

        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    /// <summary>
    /// Callback existente para drag con ratón/trigger (según vuestro Input Action).
    /// </summary>
    public void OnDrag(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            StartDrag();

        if (ctx.canceled)
            EndDrag();
    }

    /// <summary>
    /// Callback opcional para acción "Look" desde PlayerInput.
    /// Si no se conecta, se usa Gamepad.current.rightStick directamente.
    /// </summary>
    public void OnLook(InputAction.CallbackContext ctx)
    {
        _lookInputFromAction = ctx.ReadValue<Vector2>();
    }

    private void StartDrag()
    {
        if (_mainCamera == null || _cameraTarget == null)
            return;

        _isDragging = true;
        _dragStartPosition = GetMouseWorldPosition();
        _cameraStartPosition = _cameraTarget.position;
    }

    private void EndDrag()
    {
        _isDragging = false;
    }

    private void LateUpdate()
    {
        if (_cameraTarget == null)
            return;

        if (_isDragging)
            HandleMouseDrag();

        HandleControllerStickPan();
    }

    private void HandleMouseDrag()
    {
        if (_mainCamera == null)
            return;

        Vector3 currentMousePos = GetMouseWorldPosition();
        Vector3 difference = _dragStartPosition - currentMousePos;
        Vector3 targetPosition = _cameraStartPosition + difference;

        targetPosition = ApplyConfinement(targetPosition);
        _cameraTarget.position = targetPosition;
    }

    private void HandleControllerStickPan()
    {
        Vector2 stick = GetLookInput();

        if (stick.sqrMagnitude < _stickDeadzone * _stickDeadzone)
            return;

        // Mantengo el eje Y invertido como en vuestro comportamiento actual.
        Vector3 move = new Vector3(stick.x, -stick.y, 0f) * _controllerSensitivity * Time.deltaTime;
        Vector3 targetPosition = _cameraTarget.position + move;

        targetPosition = ApplyConfinement(targetPosition);
        _cameraTarget.position = targetPosition;
    }

    private Vector2 GetLookInput()
    {
        // Prioridad 1: acción pasada por callback (si está conectada).
        if (_lookInputFromAction.sqrMagnitude > 0f)
            return _lookInputFromAction;

        // Fallback New Input System directo (sin Input Manager antiguo).
        if (Gamepad.current != null)
            return Gamepad.current.rightStick.ReadValue();

        return Vector2.zero;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (_mainCamera == null || Mouse.current == null)
            return _cameraTarget != null ? _cameraTarget.position : Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Mantengo la profundidad fija original para conservar vuestro comportamiento.
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 730f));
        return new Vector3(worldPos.x, worldPos.y, _cameraTarget.position.z);
    }

    private Vector3 ApplyConfinement(Vector3 desiredPosition)
    {
        if (confiner2D == null || confiner2D.m_BoundingShape2D == null)
            return desiredPosition;

        Collider2D boundingShape = confiner2D.m_BoundingShape2D;
        Vector2 desiredPos2D = new Vector2(desiredPosition.x, desiredPosition.y);

        if (!boundingShape.bounds.Contains(desiredPos2D))
        {
            Vector2 clampedPos = boundingShape.bounds.ClosestPoint(desiredPos2D);
            return new Vector3(clampedPos.x, clampedPos.y, desiredPosition.z);
        }

        return desiredPosition;
    }
}