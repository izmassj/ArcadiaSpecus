using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class IntersectionSplineExpander : MonoBehaviour
{
    [SerializeField] private SplineContainer _splineContainer;
    [SerializeField] private float _duplicatePointDistance = 0.01f;

    public SplineContainer SplineContainer => _splineContainer;

    public void AppendRoom(RoomManager room, bool skipFirstPoint = true)
    {
        if (_splineContainer == null || room == null || !room.HasRailPoints())
            return;

        Spline spline = _splineContainer.Spline;
        var railPoints = room.RailPoints;
        int startIndex = skipFirstPoint ? 1 : 0;

        for (int i = startIndex; i < railPoints.Count; i++)
        {
            Transform point = railPoints[i];

            if (point == null)
                continue;

            float3 localPosition = (float3)_splineContainer.transform.InverseTransformPoint(point.position);

            if (IsDuplicateOfLastKnot(spline, localPosition))
                continue;

            spline.Add(new BezierKnot(localPosition), TangentMode.Linear);
        }

        spline.Closed = false;
        room.SetRailOwner(this);
    }

    [ContextMenu("Clear Spline")]
    public void ClearSpline()
    {
        if (_splineContainer == null)
            return;

        _splineContainer.Spline.Clear();
    }

    private bool IsDuplicateOfLastKnot(Spline spline, float3 localPosition)
    {
        if (spline == null || spline.Count == 0)
            return false;

        float3 lastPosition = spline[spline.Count - 1].Position;
        return math.distance(lastPosition, localPosition) <= _duplicatePointDistance;
    }
}
