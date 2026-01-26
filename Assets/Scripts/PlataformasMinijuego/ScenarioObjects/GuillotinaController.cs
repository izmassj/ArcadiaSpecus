using UnityEngine;
using UnityEngine.UIElements;

public class GuillotinaController : BaseMovingPlatform
{
    private void Update()
    {
        transform.position = CalculateMovement();
    }

    protected override void InitializePoints()
    {
        base.InitializePoints();
        settings.movementDirection = Vector3.right;
    }
}