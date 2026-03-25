using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

public class IntersectionSplineExpander : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer _splineContainer;

    [Header("Branch Start Points")]
    [SerializeField] private Transform _leftStartPoint;
    [SerializeField] private Transform _rightStartPoint;

    [Header("Fixed Intersection Splines")]
    [SerializeField] private int _horizontalSplineIndex = 0;
    [SerializeField] private int _rightElevatorSplineIndex = 1;
    [SerializeField] private int _leftElevatorSplineIndex = 2;

    [Header("Dynamic Room Branches")]
    [SerializeField] private int _leftSplineIndex = -1;
    [SerializeField] private int _rightSplineIndex = -1;

    [Header("Parameters")]
    [SerializeField] private float _duplicateDistance = 0.01f;
    [SerializeField] private int _closestPointSamples = 40;

    public SplineContainer GetSplineContainer()
    {
        return _splineContainer;
    }

    public int GetLeftBranchIndex()
    {
        return _leftSplineIndex;
    }

    public int GetRightBranchIndex()
    {
        return _rightSplineIndex;
    }

    public int GetPreferredElevatorSplineIndex()
    {
        if (HasUsableSpline(_rightElevatorSplineIndex))
            return _rightElevatorSplineIndex;

        if (HasUsableSpline(_leftElevatorSplineIndex))
            return _leftElevatorSplineIndex;

        return -1;
    }

    public int GetOrCreateBranchIndex(Vector3 roomWorldPosition)
    {
        if (_splineContainer == null)
            return -1;

        Vector3 localPoint = transform.InverseTransformPoint(roomWorldPosition);
        bool isLeftBranch = localPoint.x < 0f;

        return isLeftBranch ? GetOrCreateLeftBranch() : GetOrCreateRightBranch();
    }

    public void AppendRoomToBranch(RoomManager room, int splineIndex)
    {
        if (_splineContainer == null || room == null)
            return;

        if (!IsValidSplineIndex(splineIndex))
            return;

        List<Transform> railPoints = room.GetRailPoints();

        if (railPoints == null || railPoints.Count == 0)
            return;

        Spline spline = _splineContainer[splineIndex];
        bool reverse = ShouldReversePoints(spline, railPoints);

        if (!reverse)
        {
            for (int i = 0; i < railPoints.Count; i++)
            {
                TryAddPoint(spline, railPoints[i]);
            }
        }
        else
        {
            for (int i = railPoints.Count - 1; i >= 0; i--)
            {
                TryAddPoint(spline, railPoints[i]);
            }
        }
    }

    public bool HasUsableSpline(int splineIndex)
    {
        return IsValidSplineIndex(splineIndex) && _splineContainer[splineIndex].Count >= 2;
    }

    public int GetFirstUsableSplineIndex()
    {
        if (HasUsableSpline(_leftSplineIndex))
            return _leftSplineIndex;

        if (HasUsableSpline(_rightSplineIndex))
            return _rightSplineIndex;

        for (int i = 0; i < _splineContainer.Splines.Count; i++)
        {
            if (HasUsableSpline(i))
                return i;
        }

        return -1;
    }

    public int GetRandomUsableSplineIndex()
    {
        if (_splineContainer == null)
            return -1;

        List<int> validIndices = new();

        for (int i = 0; i < _splineContainer.Splines.Count; i++)
        {
            if (HasUsableSpline(i))
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
            return -1;

        return validIndices[Random.Range(0, validIndices.Count)];
    }

    public float GetSplineLength(int splineIndex)
    {
        if (!HasUsableSpline(splineIndex))
            return 0f;

        return _splineContainer[splineIndex].GetLength();
    }

    public Vector3 EvaluatePositionWorld(int splineIndex, float normalizedT)
    {
        if (!HasUsableSpline(splineIndex))
            return transform.position;

        float clampedT = Mathf.Clamp01(normalizedT);
        float3 localPosition = SplineUtility.EvaluatePosition(_splineContainer[splineIndex], clampedT);
        return _splineContainer.transform.TransformPoint((Vector3)localPosition);
    }

    public Vector3 EvaluateTangentWorld(int splineIndex, float normalizedT)
    {
        if (!HasUsableSpline(splineIndex))
            return Vector3.right;

        float clampedT = Mathf.Clamp01(normalizedT);
        float3 localTangent = SplineUtility.EvaluateTangent(_splineContainer[splineIndex], clampedT);
        return _splineContainer.transform.TransformDirection((Vector3)localTangent);
    }

    public float GetClosestNormalizedT(int splineIndex, Vector3 worldPosition)
    {
        if (!HasUsableSpline(splineIndex))
            return 0f;

        int samples = Mathf.Max(4, _closestPointSamples);
        float bestT = 0f;
        float bestDistance = float.MaxValue;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float distance = (EvaluatePositionWorld(splineIndex, t) - worldPosition).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestT = t;
            }
        }

        return bestT;
    }

    public float GetRoomNormalizedT(RoomManager room)
    {
        if (room == null)
            return 0f;

        int branchIndex = room.GetBranchIndex();

        if (!HasUsableSpline(branchIndex))
            return 0f;

        return GetClosestNormalizedT(branchIndex, room.GetRailCenterWorldPosition());
    }

    public IntersectionRailPort GetRoomPort(RoomManager room)
    {
        if (room == null)
            return IntersectionRailPort.None;

        if (room.GetBranchIndex() == _leftSplineIndex)
            return IntersectionRailPort.LeftRooms;

        if (room.GetBranchIndex() == _rightSplineIndex)
            return IntersectionRailPort.RightRooms;

        Vector3 localPoint = transform.InverseTransformPoint(room.transform.position);
        return localPoint.x < 0f ? IntersectionRailPort.LeftRooms : IntersectionRailPort.RightRooms;
    }

    public Vector3 GetElevatorWorldPosition()
    {
        if (HasUsableSpline(_rightElevatorSplineIndex))
            return EvaluatePositionWorld(_rightElevatorSplineIndex, 1f);

        if (HasUsableSpline(_leftElevatorSplineIndex))
            return EvaluatePositionWorld(_leftElevatorSplineIndex, 1f);

        return transform.position;
    }

    public bool TryGetTransition(IntersectionRailPort from, IntersectionRailPort to, out int splineIndex, out float targetNormalizedT)
    {
        splineIndex = -1;
        targetNormalizedT = 0f;

        if (from == to || from == IntersectionRailPort.None || to == IntersectionRailPort.None)
            return false;

        switch (from)
        {
            case IntersectionRailPort.RightRooms:
                if (to == IntersectionRailPort.LeftRooms)
                {
                    splineIndex = _horizontalSplineIndex;
                    targetNormalizedT = 1f;
                    return HasUsableSpline(splineIndex);
                }

                if (to == IntersectionRailPort.Elevator)
                {
                    splineIndex = _rightElevatorSplineIndex;
                    targetNormalizedT = 1f;
                    return HasUsableSpline(splineIndex);
                }
                break;

            case IntersectionRailPort.LeftRooms:
                if (to == IntersectionRailPort.RightRooms)
                {
                    splineIndex = _horizontalSplineIndex;
                    targetNormalizedT = 0f;
                    return HasUsableSpline(splineIndex);
                }

                if (to == IntersectionRailPort.Elevator)
                {
                    splineIndex = _leftElevatorSplineIndex;
                    targetNormalizedT = 1f;
                    return HasUsableSpline(splineIndex);
                }
                break;

            case IntersectionRailPort.Elevator:
                if (to == IntersectionRailPort.RightRooms)
                {
                    splineIndex = _rightElevatorSplineIndex;
                    targetNormalizedT = 0f;
                    return HasUsableSpline(splineIndex);
                }

                if (to == IntersectionRailPort.LeftRooms)
                {
                    splineIndex = _leftElevatorSplineIndex;
                    targetNormalizedT = 0f;
                    return HasUsableSpline(splineIndex);
                }
                break;
        }

        return false;
    }

    private int GetOrCreateLeftBranch()
    {
        if (IsValidSplineIndex(_leftSplineIndex))
            return _leftSplineIndex;

        _leftSplineIndex = CreateBranch(_leftStartPoint);
        return _leftSplineIndex;
    }

    private int GetOrCreateRightBranch()
    {
        if (IsValidSplineIndex(_rightSplineIndex))
            return _rightSplineIndex;

        _rightSplineIndex = CreateBranch(_rightStartPoint);
        return _rightSplineIndex;
    }

    private int CreateBranch(Transform startPoint)
    {
        if (_splineContainer == null || startPoint == null)
            return -1;

        int reusableIndex = GetReusableEmptySplineIndex();
        Spline spline;
        int splineIndex;

        if (reusableIndex >= 0)
        {
            splineIndex = reusableIndex;
            spline = _splineContainer[splineIndex];
        }
        else
        {
            spline = SplineUtility.AddSpline(_splineContainer);
            splineIndex = _splineContainer.Splines.Count - 1;
        }

        float3 localStartPoint = ToLocalPoint(startPoint.position);

        if (spline.Count == 0)
        {
            spline.Add(new BezierKnot(localStartPoint), TangentMode.Linear);
        }

        return splineIndex;
    }

    private int GetReusableEmptySplineIndex()
    {
        if (_splineContainer == null)
            return -1;

        for (int i = 0; i < _splineContainer.Splines.Count; i++)
        {
            if (i == _horizontalSplineIndex || i == _rightElevatorSplineIndex || i == _leftElevatorSplineIndex)
                continue;

            if (i == _leftSplineIndex || i == _rightSplineIndex)
                continue;

            if (_splineContainer[i].Count == 0)
                return i;
        }

        return -1;
    }

    private bool ShouldReversePoints(Spline spline, List<Transform> railPoints)
    {
        if (spline == null || spline.Count == 0 || railPoints == null || railPoints.Count < 2)
            return false;

        Vector3 splineEndWorld = _splineContainer.transform.TransformPoint((Vector3)spline[spline.Count - 1].Position);
        float firstDistance = Vector3.Distance(splineEndWorld, railPoints[0].position);
        float lastDistance = Vector3.Distance(splineEndWorld, railPoints[railPoints.Count - 1].position);

        return lastDistance < firstDistance;
    }

    private void TryAddPoint(Spline spline, Transform point)
    {
        if (spline == null || point == null)
            return;

        float3 localPoint = ToLocalPoint(point.position);

        if (IsDuplicateOfLastKnot(spline, localPoint))
            return;

        spline.Add(new BezierKnot(localPoint), TangentMode.Linear);
    }

    private bool IsDuplicateOfLastKnot(Spline spline, float3 localPoint)
    {
        if (spline == null || spline.Count == 0)
            return false;

        float3 lastPoint = spline[spline.Count - 1].Position;
        return math.distance(lastPoint, localPoint) <= _duplicateDistance;
    }

    private float3 ToLocalPoint(Vector3 worldPoint)
    {
        return (float3)_splineContainer.transform.InverseTransformPoint(worldPoint);
    }

    private bool IsValidSplineIndex(int index)
    {
        return _splineContainer != null && index >= 0 && index < _splineContainer.Splines.Count;
    }
}
