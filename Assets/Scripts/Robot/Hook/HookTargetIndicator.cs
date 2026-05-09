using UnityEngine;

public class HookTargetIndicator : MonoBehaviour
{
    [SerializeField] private bool _billboardToCamera = true;
    [SerializeField] private Camera _camera;
    [SerializeField] private Vector3 _extraOffset = Vector3.zero;

    private HookGrabbableObject _target;

    public void SetCamera(Camera cam)
    {
        _camera = cam;
    }

    public void SetTarget(HookGrabbableObject target)
    {
        _target = target;
        gameObject.SetActive(_target != null);
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);

            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        transform.position = _target.GetIndicatorPoint() + _extraOffset;

        if (!_billboardToCamera)
            return;

        if (_camera == null)
            _camera = Camera.main;

        if (_camera != null)
            transform.forward = _camera.transform.forward;
    }
}
