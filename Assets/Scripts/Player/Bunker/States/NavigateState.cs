using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class NavigateState : PlayerBunkerState
{
    private Vector3 _desiredPos;
    private Vector3 _inertiaVel;
    private Tween _zoomTween;
    private float _lastZoomTargetZ = float.NaN;

    public NavigateState(PlayerBunkerManager manager) : base(manager) { }

    public override void Enter()
    {
        if (playerManager.navigationVirtualCamera != null)
        {
            _desiredPos = playerManager.navigationVirtualCamera.transform.position;
            _lastZoomTargetZ = playerManager.navigationVirtualCamera.transform.position.z;
        }

        _inertiaVel = Vector3.zero;
    }

    public override void Exit()
    {
        _zoomTween?.Kill();
        _zoomTween = null;
    }

    public override void HandleInput()
    {
    }

    public override void Update()
    {
        UpdateZoomInput();
        UpdateCameraMovement();
    }


    private void UpdateZoomInput()
    {
        if (playerManager.zoomInputAction == null || playerManager.navigationVirtualCamera == null)
            return;

        Vector2 scroll = playerManager.zoomInputAction.ReadValue<Vector2>();

        if (scroll.y > 0.01f)
        {
            MoveZoomTo(playerManager.cameraMaxZ);
        }
        else if (scroll.y < -0.01f)
        {
            MoveZoomTo(playerManager.cameraMinZ);
        }
    }

    private void MoveZoomTo(float targetZ)
    {
        CinemachineCamera virtualCamera = playerManager.navigationVirtualCamera;
        if (virtualCamera == null)
            return;

        targetZ = Mathf.Clamp(targetZ, playerManager.cameraMinZ, playerManager.cameraMaxZ);

        if (!float.IsNaN(_lastZoomTargetZ) && Mathf.Approximately(_lastZoomTargetZ, targetZ) && _zoomTween != null && _zoomTween.IsActive())
            return;

        _lastZoomTargetZ = targetZ;

        _zoomTween?.Kill();
        _zoomTween = virtualCamera.transform
            .DOMoveZ(targetZ, playerManager.cameraZoomTweenDuration)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() =>
            {
                if (virtualCamera != null)
                {
                    Vector3 currentPos = virtualCamera.transform.position;
                    _desiredPos.z = currentPos.z;
                }
            })
            .OnKill(() =>
            {
                _zoomTween = null;
            });
    }

    private void UpdateCameraMovement()
    {
        CinemachineCamera virtualCamera = playerManager.navigationVirtualCamera;
        Camera mainCamera = playerManager.mainCamera;

        if (virtualCamera == null || mainCamera == null)
            return;

        if ((_desiredPos - virtualCamera.transform.position).sqrMagnitude > 10000f)
        {
            _desiredPos = virtualCamera.transform.position;
            _inertiaVel = Vector3.zero;
        }

        bool dragging = playerManager.dragInputAction != null && playerManager.dragInputAction.IsPressed();
        Vector2 stick = GetStickInput();

        if (dragging)
        {
            if (stick == Vector2.zero && Mouse.current != null)
            {
                Vector2 deltaPx = Mouse.current.delta.ReadValue();

                float wppY = (2f * mainCamera.orthographicSize) / Screen.height;
                float wppX = wppY * mainCamera.aspect;

                float sign = playerManager.cameraInvert ? 1f : -1f;

                Vector3 deltaWorld = new Vector3(deltaPx.x * wppX, deltaPx.y * wppY, 0f)
                                   * (sign * playerManager.cameraMouseSensitivity);

                _desiredPos += deltaWorld;
                CacheInertia(deltaWorld);
            }
            else if (stick != Vector2.zero)
            {
                float sign = playerManager.cameraInvert ? 1f : -1f;

                Vector3 deltaWorld = new Vector3(stick.x, stick.y, 0f)
                                   * (sign * playerManager.cameraStickSpeed)
                                   * Time.deltaTime;

                _desiredPos += deltaWorld;
                CacheInertia(deltaWorld);
            }
        }
        else if (playerManager.cameraEnableInertia && _inertiaVel.sqrMagnitude > 0f)
        {
            _desiredPos += _inertiaVel * Time.deltaTime;
            _inertiaVel = Vector3.Lerp(_inertiaVel, Vector3.zero, playerManager.cameraInertiaDecay * Time.deltaTime);

            if (_inertiaVel.magnitude < playerManager.cameraInertiaStopSpeed)
                _inertiaVel = Vector3.zero;
        }

        if (playerManager.navigationConfiner2D != null &&
            playerManager.navigationConfiner2D.BoundingShape2D != null)
        {
            Vector3 before = _desiredPos;
            _desiredPos = ClampToBounds(_desiredPos, playerManager.navigationConfiner2D.BoundingShape2D.bounds);

            if (!Mathf.Approximately(before.x, _desiredPos.x)) _inertiaVel.x = 0f;
            if (!Mathf.Approximately(before.y, _desiredPos.y)) _inertiaVel.y = 0f;
        }

        Vector3 current = virtualCamera.transform.position;
        _desiredPos.z = current.z;

        float t = 1f - Mathf.Exp(-playerManager.cameraSharpness * Time.deltaTime);
        Vector3 next = Vector3.Lerp(current, _desiredPos, t);

        if (playerManager.navigationConfiner2D != null &&
            playerManager.navigationConfiner2D.BoundingShape2D != null)
        {
            next = ClampToBounds(next, playerManager.navigationConfiner2D.BoundingShape2D.bounds);
        }

        next.z = current.z;
        virtualCamera.transform.position = next;
    }

    private void CacheInertia(Vector3 deltaWorld)
    {
        if (!playerManager.cameraEnableInertia)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0.00001f)
            return;

        Vector3 velocity = deltaWorld / dt;

        if (velocity.magnitude > playerManager.cameraInertiaMaxSpeed)
            velocity = velocity.normalized * playerManager.cameraInertiaMaxSpeed;

        _inertiaVel = Vector3.Lerp(_inertiaVel, velocity, 0.6f);
    }

    private Vector2 GetStickInput()
    {
        if (Gamepad.current == null)
            return Vector2.zero;

        Vector2 stick = Gamepad.current.rightStick.ReadValue();

        if (stick.magnitude < playerManager.cameraStickDeadzone)
            return Vector2.zero;

        return stick;
    }

    private Vector3 ClampToBounds(Vector3 p, Bounds b)
    {
        Camera mainCamera = playerManager.mainCamera;

        if (mainCamera != null && mainCamera.orthographic)
        {
            float halfH = mainCamera.orthographicSize;
            float halfW = halfH * mainCamera.aspect;

            p.x = Mathf.Clamp(p.x, b.min.x + halfW, b.max.x - halfW);
            p.y = Mathf.Clamp(p.y, b.min.y + halfH, b.max.y - halfH);
            return p;
        }

        p.x = Mathf.Clamp(p.x, b.min.x, b.max.x);
        p.y = Mathf.Clamp(p.y, b.min.y, b.max.y);
        return p;
    }

}