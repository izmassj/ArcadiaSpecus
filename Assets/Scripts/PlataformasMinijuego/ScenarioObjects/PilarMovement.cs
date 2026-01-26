using UnityEngine;
using UnityEngine.UIElements;

public class PilarMovement : BaseMovingPlatform
{
    private void Update()
    {
        transform.position = CalculateMovement();
    }

    protected override void InitializePoints()
    {
        base.InitializePoints();
        settings.movementDirection = Vector3.down;
        settings.pingPong = false;
        settings.startOnContact = true;
    }
}