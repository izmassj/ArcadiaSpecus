using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseMovingPlatform : MonoBehaviour
{
    [SerializeField] protected PlatformMovementSettings settings;

    protected Vector3 startPoint;
    protected Vector3 endPoint;
    protected bool movingForward = true;
    protected bool isActive = false;

    protected virtual void Start()
    {
        InitializePoints();

        if (!settings.startOnContact)
        {
            isActive = true;
        }
    }

    protected virtual void InitializePoints()
    {
        startPoint = transform.position;
        endPoint = startPoint + settings.movementDirection.normalized * settings.distance;
    }

    protected virtual Vector3 CalculateMovement()
    {
        if (!isActive) return transform.position;

        Vector3 targetPosition = movingForward ? endPoint : startPoint;
        Vector3 newPosition = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            settings.speed * Time.deltaTime
        );

        if (settings.pingPong && Vector3.Distance(newPosition, targetPosition) < 0.01f)
        {
            movingForward = !movingForward;
        }
        else if (settings.oneWayWithReset && Vector3.Distance(newPosition, targetPosition) < 0.01f)
        {
            newPosition = startPoint;
        }

        return newPosition;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (settings.startOnContact && other.CompareTag("Player"))
        {
            isActive = true;
        }
    }
}
