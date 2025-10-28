using UnityEngine;
using UnityEngine.InputSystem;

public class CameraDrag : MonoBehaviour
{
    private Vector3 _origin;
    private Vector3 _difference;

    private Camera _mainCamera;

    private bool _isDragging;

    private void Awake() => _mainCamera = Camera.main;

    public void OnDrag(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            _origin = GetMousePosition();
        }
        _isDragging = ctx.started || ctx.performed;
    }

    private void LateUpdate()
    {
        if (!_isDragging) return;

        Debug.Log(_mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()));

        _difference = GetMousePosition() - transform.position;
        transform.position = _origin - _difference;
    }

    private Vector3 GetMousePosition() 
    {
        return _mainCamera.ScreenToWorldPoint(new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 730f));
    }
}
