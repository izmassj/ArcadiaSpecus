using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class IntersectionSplineExpander : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer _splineContainer;

    [Header("Branch Start Points")]
    [SerializeField] private Transform _leftStartPoint;
    [SerializeField] private Transform _rightStartPoint;

    [Header("Debug")]
    [SerializeField] private int _leftSplineIndex = -1;
    [SerializeField] private int _rightSplineIndex = -1;

    [Header("Parameters")]
    [SerializeField] private float _duplicateDistance = 0.01f;

    public SplineContainer GetSplineContainer()
    {
        return _splineContainer;
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

        if (splineIndex < 0 || splineIndex >= _splineContainer.Splines.Count)
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
