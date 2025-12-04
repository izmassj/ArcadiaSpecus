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

    public void OnDrag(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            StartDrag();
        }
        else if (ctx.canceled)
        {
            EndDrag();
        }
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
        if (!_isDragging) return;

        Vector3 currentMousePos = GetMouseWorldPosition();
        Vector3 difference = _dragStartPosition - currentMousePos;
        Vector3 targetPosition = _cameraStartPosition + difference;

        // Apply confinement
        if (confiner2D != null && confiner2D.m_BoundingShape2D != null)
        {
            targetPosition = ApplyConfinement(targetPosition);
        }

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