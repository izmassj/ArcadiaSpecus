using UnityEngine;

public abstract class BaseCarryingPlatform : BaseMovingPlatform
{
    protected Transform playerOnPlatform;
    protected Vector3 lastPlatformPosition;

    protected void Start()
    {
        lastPlatformPosition = transform.position;
    }

    protected void Update()
    {
        UpdatePlayerMovement();
    }

    protected virtual void UpdatePlayerMovement()
    {
        if (playerOnPlatform != null)
        {
            Vector3 platformMovement = transform.position - lastPlatformPosition;
            playerOnPlatform.position += platformMovement;
        }
        lastPlatformPosition = transform.position;
    }

    protected virtual void HandlePlayerCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = collision.transform;
        }
    }

    protected virtual void HandlePlayerCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = null;
        }
    }
}