using UnityEngine;

public class MovingPlatform : BaseCarryingPlatform
{
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