using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class TankWheelSplineRepeater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int splineIndex = 0;
    [SerializeField] private Transform piecePrefab;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private Transform wheelCenter;

    [Header("Distribution")]
    [SerializeField, Min(0.001f)] private float spacing = 0.15f;
    [SerializeField, Min(1)] private int maxPieces = 128;

    [Header("Movement")]
    [Range(0f, 1f)]
    [SerializeField] private float normalizedOffset = 0f;
    [SerializeField] private float speed = 0f;
    [SerializeField] private bool reverseDirection = false;

    [Header("Rotation")]
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;
    [SerializeField] private bool faceTowardCenter = false;

    [Header("Spline Sampling")]
    [SerializeField, Min(16)] private int minSampleCount = 64;
    [SerializeField, Range(1, 8)] private int samplesPerPiece = 3;

    private readonly List<Transform> _pieces = new();

    private Vector3[] _sampleLocalPositions;
    private Vector3[] _sampleLocalTangents;

    private float _splineLength;
    private bool _closed;
    private int _pieceCount;

    private float _lastOffset = float.MinValue;
    private bool _isBuilt;

    private void OnEnable()
    {
        if (piecesRoot == null)
            piecesRoot = transform;

        Rebuild();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!_isBuilt || !CanBuild())
            return;

        bool changed = false;

        if (Mathf.Abs(speed) > 0.0001f)
        {
            float dir = reverseDirection ? -1f : 1f;
            normalizedOffset = Mathf.Repeat(
                normalizedOffset + (speed / Mathf.Max(_splineLength, 0.0001f)) * dir * Time.deltaTime,
                1f
            );
            changed = true;
        }

        if (!Mathf.Approximately(_lastOffset, normalizedOffset))
            changed = true;

        if (!changed)
            return;

        UpdatePiecesFast();
        _lastOffset = normalizedOffset;
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (!CanBuild())
            return;

        if (piecesRoot == null)
            piecesRoot = transform;

        CacheSplineData();
        EnsurePieceCount(_pieceCount);
        BuildSamples();
        UpdatePiecesFast();

        _lastOffset = normalizedOffset;
        _isBuilt = true;
    }

    [ContextMenu("Refresh Positions")]
    public void RefreshPositions()
    {
        if (!_isBuilt || !CanBuild())
            return;

        UpdatePiecesFast();
        _lastOffset = normalizedOffset;
    }

    [ContextMenu("Clear Generated Pieces")]
    public void ClearGeneratedPieces()
    {
        if (piecesRoot == null)
            return;

        for (int i = piecesRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = piecesRoot.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        _pieces.Clear();
        _sampleLocalPositions = null;
        _sampleLocalTangents = null;
        _isBuilt = false;
    }

    private void CacheSplineData()
    {
        Spline spline = GetSpline();
        if (spline == null)
        {
            _closed = false;
            _splineLength = 0f;
            _pieceCount = 0;
            return;
        }

        _closed = spline.Closed;
        _splineLength = splineContainer.CalculateLength(splineIndex);
        _pieceCount = GetTargetPieceCount(_splineLength, _closed);
    }

    private void BuildSamples()
    {
        int sampleCount = Mathf.Max(minSampleCount, _pieceCount * samplesPerPiece);

        _sampleLocalPositions = new Vector3[sampleCount];
        _sampleLocalTangents = new Vector3[sampleCount];

        Transform splineTransform = splineContainer.transform;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;

            Vector3 worldPos = (Vector3)splineContainer.EvaluatePosition(splineIndex, t);
            Vector3 worldTangent = (Vector3)splineContainer.EvaluateTangent(splineIndex, t);

            if (worldTangent.sqrMagnitude < 0.000001f)
                worldTangent = Vector3.forward;

            _sampleLocalPositions[i] = splineTransform.InverseTransformPoint(worldPos);
            _sampleLocalTangents[i] = splineTransform.InverseTransformDirection(worldTangent).normalized;
        }
    }

    private void UpdatePiecesFast()
    {
        if (_pieces.Count == 0 || _sampleLocalPositions == null || _sampleLocalPositions.Length == 0)
            return;

        int sampleCount = _sampleLocalPositions.Length;
        Transform splineTransform = splineContainer.transform;

        for (int i = 0; i < _pieces.Count; i++)
        {
            Transform piece = _pieces[i];
            if (piece == null)
                continue;

            float baseT;

            if (_closed)
                baseT = (float)i / _pieces.Count;
            else
                baseT = (_pieces.Count == 1) ? 0f : (float)i / (_pieces.Count - 1);

            float signedOffset = reverseDirection ? -normalizedOffset : normalizedOffset;

            float t = _closed
                ? Mathf.Repeat(baseT + signedOffset, 1f)
                : Mathf.Clamp01(baseT + signedOffset);

            float rawIndex = t * sampleCount;
            int a = Mathf.FloorToInt(rawIndex) % sampleCount;
            int b = (a + 1) % sampleCount;
            float lerp = rawIndex - Mathf.Floor(rawIndex);

            Vector3 localPosition = Vector3.LerpUnclamped(_sampleLocalPositions[a], _sampleLocalPositions[b], lerp);
            Vector3 localTangent = Vector3.SlerpUnclamped(_sampleLocalTangents[a], _sampleLocalTangents[b], lerp).normalized;

            if (!_closed && t >= 0.9999f)
            {
                localPosition = _sampleLocalPositions[sampleCount - 1];
                localTangent = _sampleLocalTangents[sampleCount - 1];
            }

            Vector3 worldPosition = splineTransform.TransformPoint(localPosition);
            Vector3 worldTangent = splineTransform.TransformDirection(localTangent).normalized;

            Quaternion rotation = GetRotationForPiece(worldPosition, worldTangent);
            piece.SetPositionAndRotation(worldPosition, rotation);
        }
    }

    private Quaternion GetRotationForPiece(Vector3 worldPosition, Vector3 tangent)
    {
        Vector3 radial;

        if (wheelCenter != null)
        {
            radial = worldPosition - wheelCenter.position;
            radial = Vector3.ProjectOnPlane(radial, tangent);
        }
        else
        {
            radial = Vector3.Cross(tangent, Vector3.right);
            if (radial.sqrMagnitude < 0.000001f)
                radial = Vector3.Cross(tangent, Vector3.up);
        }

        if (faceTowardCenter)
            radial = -radial;

        if (radial.sqrMagnitude < 0.000001f)
            radial = Vector3.up;

        radial.Normalize();

        Quaternion baseRotation = Quaternion.LookRotation(tangent, radial);
        return baseRotation * Quaternion.Euler(localEulerOffset);
    }

    private int GetTargetPieceCount(float splineLength, bool closed)
    {
        int count = Mathf.FloorToInt(splineLength / spacing);

        if (!closed)
            count += 1;

        count = Mathf.Max(1, count);
        count = Mathf.Min(count, maxPieces);

        return count;
    }

    private void EnsurePieceCount(int targetCount)
    {
        if (piecesRoot == null)
            piecesRoot = transform;

        while (_pieces.Count < targetCount)
        {
            Transform instance = Instantiate(piecePrefab, piecesRoot);
            instance.name = piecePrefab.name + "_" + _pieces.Count.ToString("000");
            _pieces.Add(instance);
        }

        while (_pieces.Count > targetCount)
        {
            int last = _pieces.Count - 1;
            Transform piece = _pieces[last];
            _pieces.RemoveAt(last);

            if (piece != null)
            {
                if (Application.isPlaying)
                    Destroy(piece.gameObject);
                else
                    DestroyImmediate(piece.gameObject);
            }
        }
    }

    private bool CanBuild()
    {
        if (splineContainer == null)
            return false;

        if (piecePrefab == null)
            return false;

        if (splineIndex < 0 || splineIndex >= splineContainer.Splines.Count)
            return false;

        return true;
    }

    private Spline GetSpline()
    {
        if (splineContainer == null)
            return null;

        if (splineIndex < 0 || splineIndex >= splineContainer.Splines.Count)
            return null;

        return splineContainer.Splines[splineIndex];
    }
}