using UnityEngine;

public class ElectricRouteNode : ElectricNode
{
    public enum RouteShape
    {
        Custom,
        StraightZ,
        StraightX,
        CornerForwardRight,
        CornerForwardLeft,
        CornerBackRight,
        CornerBackLeft,
        ThreeWayForwardLeftRight,
        ThreeWayBackLeftRight,
        ThreeWayForwardBackRight,
        ThreeWayForwardBackLeft,
        FourWay
    }

    [Header("Route Preset")]
    [SerializeField] private RouteShape _routeShape = RouteShape.StraightZ;
    [SerializeField] private float _halfLength = 0.5f;
    [SerializeField] private float _connectionHeight;
    [SerializeField] private bool _applyPresetOnValidate = true;

    private void Reset()
    {
        ApplyPreset();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        _halfLength = Mathf.Max(0.01f, _halfLength);

        if (_applyPresetOnValidate)
            ApplyPreset();
    }

    [ContextMenu("Apply Route Connection Preset")]
    public void ApplyPreset()
    {
        if (_routeShape == RouteShape.Custom)
            return;

        Vector3 f = new Vector3(0f, _connectionHeight, _halfLength);
        Vector3 b = new Vector3(0f, _connectionHeight, -_halfLength);
        Vector3 r = new Vector3(_halfLength, _connectionHeight, 0f);
        Vector3 l = new Vector3(-_halfLength, _connectionHeight, 0f);

        switch (_routeShape)
        {
            case RouteShape.StraightZ:
                SetLocalConnectionPoints(new[] { b, f });
                break;
            case RouteShape.StraightX:
                SetLocalConnectionPoints(new[] { l, r });
                break;
            case RouteShape.CornerForwardRight:
                SetLocalConnectionPoints(new[] { f, r });
                break;
            case RouteShape.CornerForwardLeft:
                SetLocalConnectionPoints(new[] { f, l });
                break;
            case RouteShape.CornerBackRight:
                SetLocalConnectionPoints(new[] { b, r });
                break;
            case RouteShape.CornerBackLeft:
                SetLocalConnectionPoints(new[] { b, l });
                break;
            case RouteShape.ThreeWayForwardLeftRight:
                SetLocalConnectionPoints(new[] { f, l, r });
                break;
            case RouteShape.ThreeWayBackLeftRight:
                SetLocalConnectionPoints(new[] { b, l, r });
                break;
            case RouteShape.ThreeWayForwardBackRight:
                SetLocalConnectionPoints(new[] { f, b, r });
                break;
            case RouteShape.ThreeWayForwardBackLeft:
                SetLocalConnectionPoints(new[] { f, b, l });
                break;
            case RouteShape.FourWay:
                SetLocalConnectionPoints(new[] { f, b, l, r });
                break;
        }
    }
}
