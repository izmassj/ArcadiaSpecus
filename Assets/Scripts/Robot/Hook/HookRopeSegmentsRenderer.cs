using System.Collections.Generic;
using UnityEngine;

public class HookRopeSegmentsRenderer : MonoBehaviour
{
    private enum SegmentAxis
    {
        X,
        Y,
        Z
    }

    [Header("Segment Prefab")]
    [SerializeField] private GameObject _segmentPrefab;
    [SerializeField] private Transform _segmentParent;
    [SerializeField] private SegmentAxis _segmentForwardAxis = SegmentAxis.Y;
    [SerializeField] private float _segmentLength = 0.35f;
    [SerializeField] private int _maxSegments = 64;
    [SerializeField] private bool _scaleSegmentsToFit = true;

    [Header("Curve")]
    [SerializeField] private float _attachedSagAmount = 0.35f;
    [SerializeField] private AnimationCurve _sagCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

    [Header("Ground Clamp")]
    [SerializeField] private bool _avoidGroundClip = true;
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private float _groundCheckHeight = 1.5f;
    [SerializeField] private float _minimumHeightAboveGround = 0.06f;

    [Header("Outline Integration")]
    [SerializeField] private ChildRenderersOutlineOverride _outlineOverride;
    [SerializeField] private bool _autoFindOutlineOverride = true;
    [SerializeField] private bool _registerSegmentsInOutlineOverride = true;
    [SerializeField] private bool _forceSegmentLayer = true;
    [SerializeField] private string _entityLayerName = "Entity";
    [SerializeField] private int _fallbackEntityLayer = 10;
    [SerializeField] private bool _useParentLayerIfEntityLayerMissing = true;

    private readonly List<Transform> _segments = new List<Transform>();
    private readonly List<Vector3> _baseScales = new List<Vector3>();
    private readonly RaycastHit[] _groundHits = new RaycastHit[4];
    private bool _visible;
    private int _cachedRuntimeLayer = -1;

    private void Awake()
    {
        if (_segmentParent == null)
            _segmentParent = transform;

        ResolveOutlineOverride();
        Hide();
    }

    public void SetRope(Vector3 start, Vector3 end, bool useSag)
    {
        if (_segmentPrefab == null)
            return;

        float distance = Vector3.Distance(start, end);
        if (distance <= 0.01f)
        {
            Hide();
            return;
        }

        int count = Mathf.Clamp(Mathf.CeilToInt(distance / Mathf.Max(0.01f, _segmentLength)), 1, Mathf.Max(1, _maxSegments));
        EnsureSegments(count);

        _visible = true;

        Vector3 previous = GetPoint(start, end, 0f, useSag);
        for (int i = 0; i < count; i++)
        {
            float t0 = (float)i / count;
            float t1 = (float)(i + 1) / count;
            float tm = (t0 + t1) * 0.5f;

            Vector3 a = i == 0 ? previous : GetPoint(start, end, t0, useSag);
            Vector3 b = GetPoint(start, end, t1, useSag);
            Vector3 mid = GetPoint(start, end, tm, useSag);
            Vector3 direction = b - a;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = (end - start).normalized;
            else
                direction.Normalize();

            Transform segment = _segments[i];
            if (!segment.gameObject.activeSelf)
                segment.gameObject.SetActive(true);

            segment.position = mid;
            segment.rotation = GetRotationForAxis(direction);

            if (_scaleSegmentsToFit)
                ApplyLengthScale(segment, Vector3.Distance(a, b));
        }

        for (int i = count; i < _segments.Count; i++)
        {
            if (_segments[i].gameObject.activeSelf)
                _segments[i].gameObject.SetActive(false);
        }
    }

    public void Hide()
    {
        _visible = false;

        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] != null)
                _segments[i].gameObject.SetActive(false);
        }
    }

    private void EnsureSegments(int count)
    {
        while (_segments.Count < count)
        {
            GameObject instance = Instantiate(_segmentPrefab, _segmentParent);
            instance.SetActive(false);

            if (_forceSegmentLayer)
                SetLayerRecursively(instance, GetRuntimeLayer());

            _segments.Add(instance.transform);
            _baseScales.Add(instance.transform.localScale);

            RegisterSegmentForOutline(instance.transform);
        }
    }

    private Vector3 GetPoint(Vector3 start, Vector3 end, float t, bool useSag)
    {
        Vector3 point = Vector3.Lerp(start, end, t);

        if (useSag)
        {
            float sag01 = _sagCurve != null ? _sagCurve.Evaluate(t) : Mathf.Sin(t * Mathf.PI);
            point += Vector3.down * (_attachedSagAmount * sag01);
        }

        if (_avoidGroundClip)
            point = ClampAboveGround(point);

        return point;
    }

    private Vector3 ClampAboveGround(Vector3 point)
    {
        Vector3 origin = point + Vector3.up * Mathf.Max(0.01f, _groundCheckHeight);
        float distance = Mathf.Max(0.01f, _groundCheckHeight * 2f);

        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _groundHits, distance, _groundMask, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
            return point;

        float bestY = float.NegativeInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _groundHits[i].collider;
            if (hitCollider == null)
                continue;

            bestY = Mathf.Max(bestY, _groundHits[i].point.y);
        }

        if (bestY == float.NegativeInfinity)
            return point;

        float minY = bestY + _minimumHeightAboveGround;
        if (point.y < minY)
            point.y = minY;

        return point;
    }

    private Quaternion GetRotationForAxis(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        Quaternion look = Quaternion.LookRotation(direction, Vector3.up);

        switch (_segmentForwardAxis)
        {
            case SegmentAxis.X:
                return look * Quaternion.Euler(0f, 90f, 0f);
            case SegmentAxis.Y:
                return look * Quaternion.Euler(90f, 0f, 0f);
            default:
                return look;
        }
    }

    private void ApplyLengthScale(Transform segment, float length)
    {
        int index = _segments.IndexOf(segment);
        Vector3 scale = index >= 0 && index < _baseScales.Count ? _baseScales[index] : Vector3.one;
        float multiplier = length / Mathf.Max(0.01f, _segmentLength);

        switch (_segmentForwardAxis)
        {
            case SegmentAxis.X:
                scale.x = multiplier;
                break;
            case SegmentAxis.Y:
                scale.y = multiplier;
                break;
            default:
                scale.z = multiplier;
                break;
        }

        segment.localScale = scale;
    }

    private void ResolveOutlineOverride()
    {
        if (_outlineOverride != null || !_autoFindOutlineOverride)
            return;

        Transform searchRoot = _segmentParent != null ? _segmentParent : transform;
        _outlineOverride = searchRoot.GetComponentInParent<ChildRenderersOutlineOverride>();

        if (_outlineOverride == null && transform != searchRoot)
            _outlineOverride = GetComponentInParent<ChildRenderersOutlineOverride>();
    }

    private void RegisterSegmentForOutline(Transform segmentRoot)
    {
        if (!_registerSegmentsInOutlineOverride || segmentRoot == null)
            return;

        ResolveOutlineOverride();

        if (_outlineOverride == null)
            return;

        _outlineOverride.RegisterChildRenderers(segmentRoot, true, true);
    }

    private int GetRuntimeLayer()
    {
        if (_cachedRuntimeLayer >= 0)
            return _cachedRuntimeLayer;

        int entityLayer = string.IsNullOrWhiteSpace(_entityLayerName) ? -1 : LayerMask.NameToLayer(_entityLayerName);
        if (entityLayer >= 0)
        {
            _cachedRuntimeLayer = entityLayer;
            return _cachedRuntimeLayer;
        }

        if (_useParentLayerIfEntityLayerMissing && _segmentParent != null)
        {
            _cachedRuntimeLayer = _segmentParent.gameObject.layer;
            return _cachedRuntimeLayer;
        }

        _cachedRuntimeLayer = Mathf.Clamp(_fallbackEntityLayer, 0, 31);
        return _cachedRuntimeLayer;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
    }

    private void OnDisable()
    {
        if (_visible)
            Hide();
    }
}
