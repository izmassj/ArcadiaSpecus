using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Hall Points")]
    [SerializeField] private List<Transform> hallPoints = new List<Transform>();

    [Header("Action Points")]
    [SerializeField] private List<Transform> actionPoints = new List<Transform>();

    [Header("Behaviour")]
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 4f;
    [SerializeField] private float arriveDistance = 0.2f;

    [Header("Debug")]
    [SerializeField] private NPCState currentState;
    [SerializeField] private Transform currentTarget;

    private bool isWaiting;

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError($"[{name}] No hay NavMeshAgent asignado.");
            enabled = false;
            return;
        }

        if (hallPoints.Count == 0 && actionPoints.Count == 0)
        {
            Debug.LogWarning($"[{name}] No hay puntos asignados.");
            enabled = false;
            return;
        }

        GoToRandomHallPoint();
    }

    private void Update()
    {
        if (currentState == NPCState.MovingToHall || currentState == NPCState.MovingToActionPoint)
        {
            CheckArrival();
        }
    }

    private void CheckArrival()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= arriveDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f)
            {
                StartWaiting();
            }
        }
    }

    private void StartWaiting()
    {
        if (isWaiting)
            return;

        isWaiting = true;
        currentState = NPCState.Waiting;
        agent.ResetPath();

        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        StartCoroutine(WaitRoutine(waitTime));
    }

    private IEnumerator WaitRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        isWaiting = false;

        if (currentTarget != null && hallPoints.Contains(currentTarget))
        {
            GoToRandomActionPoint();
        }
        else
        {
            GoToRandomHallPoint();
        }
    }

    private void GoToRandomHallPoint()
    {
        Transform point = GetRandomPoint(hallPoints);

        if (point == null)
        {
            currentState = NPCState.Idle;
            return;
        }

        currentTarget = point;
        currentState = NPCState.MovingToHall;
        agent.SetDestination(point.position);
    }

    private void GoToRandomActionPoint()
    {
        Transform point = GetRandomPoint(actionPoints);

        if (point == null)
        {
            GoToRandomHallPoint();
            return;
        }

        currentTarget = point;
        currentState = NPCState.MovingToActionPoint;
        agent.SetDestination(point.position);
    }

    private Transform GetRandomPoint(List<Transform> points)
    {
        if (points == null || points.Count == 0)
            return null;

        return points[Random.Range(0, points.Count)];
    }
}