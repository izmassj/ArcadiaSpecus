using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraDragCinemachine : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float dragSpeed;

    private CinemachineFramingTransposer framingTransposer;
    private Vector3 lastMousePosition;

    private void Start()
    {
        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
    }

    private void Update()
    {
        HandleDrag();
    }

    private void HandleDrag()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            Vector3 currentMousePos = GetMousePosition();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                lastMousePosition = currentMousePos;
                return;
            }

            Vector3 difference = lastMousePosition - currentMousePos;
            difference.z = 0;

            // Move the virtual camera
            virtualCamera.transform.position += difference * dragSpeed * Time.deltaTime;

            lastMousePosition = currentMousePos;
        }
    }

    private Vector3 GetMousePosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Camera mainCamera = Camera.main;
        return mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 773f));
    }
}