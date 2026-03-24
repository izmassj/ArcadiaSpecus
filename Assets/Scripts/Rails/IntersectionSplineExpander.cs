using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class IntersectionSplineExpander : MonoBehaviour
{
    [SerializeField] private SplineContainer _splineContainer;
    [SerializeField] private float _duplicatePointDistance = 0.01f;

    public SplineContainer SplineContainer => _splineContainer;

    public bool HasValidSpline()
    {
        return _splineContainer != null && _splineContainer.Spline != null;
    }

    public bool HasAnyKnot()
    {
        return HasValidSpline() && _splineContainer.Spline.Count > 0;
    }

    public Vector3 GetLastWorldPosition()
    {
        if (!HasAnyKnot())
            return transform.position;

        Spline spline = _splineContainer.Spline;
        float3 localPosition = spline[spline.Count - 1].Position;
        return _splineContainer.transform.TransformPoint((Vector3)localPosition);
    }

    public void AppendRoom(RoomManager roomManager, bool reverseOrder)
    {
        if (!HasValidSpline() || roomManager == null || roomManager.RailPointCount == 0)
            return;

        Spline spline = _splineContainer.Spline;
        bool skipConnectedEndPoint = spline.Count > 0;

        if (reverseOrder)
        {
            int startIndex = skipConnectedEndPoint ? roomManager.RailPointCount - 2 : roomManager.RailPointCount - 1;

            for (int i = startIndex; i >= 0; i--)
            {
                Transform point = roomManager.GetRailPoint(i);

                if (point == null)
                    continue;

                TryAddPoint(spline, point.position);
            }
        }
        else
        {
            int startIndex = skipConnectedEndPoint ? 1 : 0;

            for (int i = startIndex; i < roomManager.RailPointCount; i++)
            {
                Transform point = roomManager.GetRailPoint(i);

                if (point == null)
                    continue;

                TryAddPoint(spline, point.position);
            }
        }

        spline.SetTangentMode(TangentMode.Linear);
        spline.Closed = false;
    }

    [ContextMenu("Clear Spline")]
    public void ClearSpline()
    {
        if (!HasValidSpline())
            return;

        _splineContainer.Spline.Clear();
    }

    private void TryAddPoint(Spline spline, Vector3 worldPosition)
    {
        float3 localPosition = (float3)_splineContainer.transform.InverseTransformPoint(worldPosition);

        if (IsDuplicateOfLastKnot(spline, localPosition))
            return;

        spline.Add(new BezierKnot(localPosition), TangentMode.Linear);
    }

    private bool IsDuplicateOfLastKnot(Spline spline, float3 localPosition)
    {
        if (spline == null || spline.Count == 0)
            return false;

        float3 lastPosition = spline[spline.Count - 1].Position;
        return math.distance(lastPosition, localPosition) <= _duplicatePointDistance;
    }
}
