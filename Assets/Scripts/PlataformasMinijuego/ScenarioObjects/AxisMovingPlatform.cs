using UnityEngine;

public class AxisMovingPlatform : BaseCarryingPlatform
{
    [SerializeField] private MovementAxisPlatforms movementAxis = MovementAxisPlatforms.Z;

    protected override void InitializePoints()
    {
        base.InitializePoints();
        settings.movementDirection = GetAxisVector();
    }

    private Vector3 GetAxisVector()
    {
        return movementAxis switch
        {
            MovementAxisPlatforms.X => Vector3.right,
            MovementAxisPlatforms.Y => Vector3.up,
            MovementAxisPlatforms.Z => Vector3.forward,
            _ => Vector3.forward
        };
    }

    private void Update()
    {
        transform.position = CalculateMovement();
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerCollisionEnter(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        HandlePlayerCollisionExit(collision);
    }
}