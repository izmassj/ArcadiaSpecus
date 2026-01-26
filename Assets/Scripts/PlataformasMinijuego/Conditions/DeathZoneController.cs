using UnityEngine;
using UnityEngine.UIElements;

public class DeathZoneController : BaseMovingPlatform
{
    private void Update()
    {
        transform.position = CalculateMovement();
    }

    protected override void InitializePoints()
    {
        base.InitializePoints();
        settings.oneWayWithReset = true;
        settings.pingPong = false;
    }
}