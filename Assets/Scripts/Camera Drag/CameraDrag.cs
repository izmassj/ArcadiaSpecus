using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CameraDrag : MonoBehaviour
{
    [Header("Cinemachine Components")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private CinemachineConfiner2D confiner2D;

    [Header("Camera Reference")]
    [SerializeField] private Camera _mainCamera;

    private Vector3 _dragStartPosition;
    private Vector3 _cameraStartPosition;
    private bool _isDragging;
    private Transform _cameraTarget;

    // 🔹 Configuración Input Manager (solo stick)
    [Header("Controller Drag Settings")]
    public string rightStickX = "RHorizontal";
    public string rightStickY = "RVertical";
    public float controllerSensitivity = 10f;

    private void Start()
    {
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
        {
            _mainCamera = Camera.main;
        }
    }

    // EXISTENTE — drag con mouse
    public void OnDrag(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            StartDrag();
        else if (ctx.canceled)
            EndDrag();
    }

    private void StartDrag()
    {
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
        // 🖱 drag por ratón existente
        if (_isDragging)
        {
            HandleMouseDrag();
        }

        // 🎮 drag con mando sin botón
        HandleControllerDragOnlyStick();
    }

    private void HandleMouseDrag()
    {
        Vector3 currentMousePos = GetMouseWorldPosition();
        Vector3 difference = _dragStartPosition - currentMousePos;
        Vector3 targetPosition = _cameraStartPosition + difference;

        if (confiner2D != null && confiner2D.m_BoundingShape2D != null)
            targetPosition = ApplyConfinement(targetPosition);

        _cameraTarget.position = targetPosition;
    }

    // 🔹 Nuevo — cámara se mueve solo con stick derecho
    private void HandleControllerDragOnlyStick()
    {
        float stickX = Input.GetAxis(rightStickX);
        float stickY = Input.GetAxis(rightStickY);

        // Si no se mueve el stick, no hacemos nada
        if (Mathf.Abs(stickX) < 0.05f && Mathf.Abs(stickY) < 0.05f)
            return;

        Vector3 move = new Vector3(-stickX, -stickY, 0f) * controllerSensitivity * Time.deltaTime;
        Vector3 targetPosition = _cameraTarget.position + move;

        if (confiner2D != null && confiner2D.m_BoundingShape2D != null)
            targetPosition = ApplyConfinement(targetPosition);

        _cameraTarget.position = targetPosition;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
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
