using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ElectricNode : MonoBehaviour
{
    [Header("Power")]
    [SerializeField] private bool _startsAsPowerSource;
    [SerializeField] private bool _powerSourceActive;
    [SerializeField] private bool _canReceivePower = true;
    [SerializeField] private bool _canOutputPower = true;
    [SerializeField] private bool _disabled;

    [Header("Connections")]
    [SerializeField] private Transform[] _connectionPoints;
    [SerializeField] private Vector3[] _localConnectionPoints = { new Vector3(0f, 0f, -0.5f), new Vector3(0f, 0f, 0.5f) };
    [SerializeField] private float _connectionRadius = 0.28f;

    [Header("Visuals")]
    [SerializeField] private bool _applyVisuals = true;
    [SerializeField] private bool _autoFindVisualsWhenEmpty;
    [SerializeField] private GameObject[] _enableWhenPowered;
    [SerializeField] private GameObject[] _disableWhenPowered;
    [SerializeField] private Light[] _lights;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private ParticleSystem[] _particles;
    [SerializeField] private Material _poweredMaterial;
    [SerializeField] private Material _unpoweredMaterial;

    [Header("Events")]
    [SerializeField] private UnityEvent _onPowered;
    [SerializeField] private UnityEvent _onUnpowered;
    [SerializeField] private BoolEvent _onPoweredChanged;

    [Header("Debug")]
    [SerializeField] private bool _isPowered;
    [SerializeField] private Color _unpoweredGizmoColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
    [SerializeField] private Color _poweredGizmoColor = new Color(0.1f, 0.9f, 1f, 0.95f);
    [SerializeField] private Color _disabledGizmoColor = new Color(1f, 0.1f, 0.1f, 0.85f);

    private readonly List<ElectricNode> _connectedNodes = new List<ElectricNode>();
    private Material[] _initialSharedMaterials;

    public bool IsPowered => _isPowered;
    public bool IsDisabled => _disabled;
    public bool CanReceivePower => !_disabled && _canReceivePower && isActiveAndEnabled;
    public bool CanOutputPower => !_disabled && _canOutputPower && isActiveAndEnabled;
    public bool IsActivePowerSource => !_disabled && _startsAsPowerSource && _powerSourceActive && isActiveAndEnabled;
    public bool StartsAsPowerSource => _startsAsPowerSource;
    public float ConnectionRadius => Mathf.Max(0.01f, _connectionRadius);
    public IReadOnlyList<ElectricNode> ConnectedNodes => _connectedNodes;

    protected virtual void Awake()
    {
        RefreshAutoVisuals(false);
        CacheInitialMaterials();
        ApplyPoweredState(false, true);
    }

    protected virtual void OnEnable()
    {
        ElectricityNetworkManager.RequestRebuildAll();
    }

    protected virtual void OnDisable()
    {
        ElectricityNetworkManager.RequestRebuildAll();
    }

    protected virtual void OnValidate()
    {
        _connectionRadius = Mathf.Max(0.01f, _connectionRadius);
    }

    public int ConnectionPointCount
    {
        get
        {
            if (_connectionPoints != null && _connectionPoints.Length > 0)
                return _connectionPoints.Length;

            return _localConnectionPoints != null ? _localConnectionPoints.Length : 0;
        }
    }

    public Vector3 GetConnectionPointWorldPosition(int index)
    {
        if (_connectionPoints != null && _connectionPoints.Length > 0)
        {
            if (index < 0 || index >= _connectionPoints.Length || _connectionPoints[index] == null)
                return transform.position;

            return _connectionPoints[index].position;
        }

        if (_localConnectionPoints == null || index < 0 || index >= _localConnectionPoints.Length)
            return transform.position;

        return transform.TransformPoint(_localConnectionPoints[index]);
    }

    public Vector3 GetBestWorldConnectionPoint(Vector3 fromPosition)
    {
        int count = ConnectionPointCount;
        if (count <= 0)
            return transform.position;

        float bestDistance = float.MaxValue;
        Vector3 bestPoint = GetConnectionPointWorldPosition(0);

        for (int i = 0; i < count; i++)
        {
            Vector3 point = GetConnectionPointWorldPosition(i);
            float distance = (point - fromPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    public Vector3 GetApproximateCenter()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds.center;

        Collider collider = GetComponentInChildren<Collider>();
        if (collider != null)
            return collider.bounds.center;

        return transform.position;
    }

    public void SetPowerSourceActive(bool active)
    {
        if (_powerSourceActive == active)
            return;

        _powerSourceActive = active;
        ElectricityNetworkManager.RequestRecalculateAll();
    }

    public void SetStartsAsPowerSource(bool isPowerSource)
    {
        if (_startsAsPowerSource == isPowerSource)
            return;

        _startsAsPowerSource = isPowerSource;
        ElectricityNetworkManager.RequestRecalculateAll();
    }

    public void SetDisabled(bool disabled)
    {
        if (_disabled == disabled)
            return;

        _disabled = disabled;
        ElectricityNetworkManager.RequestRecalculateAll();
    }

    public void SetCanReceivePower(bool canReceivePower)
    {
        if (_canReceivePower == canReceivePower)
            return;

        _canReceivePower = canReceivePower;
        ElectricityNetworkManager.RequestRecalculateAll();
    }

    public void SetCanOutputPower(bool canOutputPower)
    {
        if (_canOutputPower == canOutputPower)
            return;

        _canOutputPower = canOutputPower;
        ElectricityNetworkManager.RequestRecalculateAll();
    }

    public void SetConnectionRadius(float radius)
    {
        _connectionRadius = Mathf.Max(0.01f, radius);
        ElectricityNetworkManager.RequestRebuildAll();
    }

    public void SetLocalConnectionPoints(Vector3[] points)
    {
        _localConnectionPoints = points;
        ElectricityNetworkManager.RequestRebuildAll();
    }

    public void SetApplyVisuals(bool applyVisuals)
    {
        _applyVisuals = applyVisuals;
        ApplyVisualState(_isPowered);
    }

    public void RefreshAutoVisuals()
    {
        RefreshAutoVisuals(true);
    }

    public void RefreshAutoVisuals(bool force)
    {
        if (!_autoFindVisualsWhenEmpty && !force)
            return;

        if (force || _renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>(true);

        if (force || _lights == null || _lights.Length == 0)
            _lights = GetComponentsInChildren<Light>(true);

        if (force || _particles == null || _particles.Length == 0)
            _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    internal void ClearRuntimeConnections()
    {
        _connectedNodes.Clear();
    }

    internal void AddRuntimeConnection(ElectricNode node)
    {
        if (node == null || node == this || _connectedNodes.Contains(node))
            return;

        _connectedNodes.Add(node);
    }

    internal void SetPoweredFromNetwork(bool powered)
    {
        ApplyPoweredState(powered, false);
    }

    private void ApplyPoweredState(bool powered, bool force)
    {
        if (!force && _isPowered == powered)
            return;

        _isPowered = powered;
        ApplyVisualState(powered);

        if (!force)
        {
            if (powered)
                _onPowered?.Invoke();
            else
                _onUnpowered?.Invoke();

            _onPoweredChanged?.Invoke(powered);
        }
    }

    private void ApplyVisualState(bool powered)
    {
        if (!_applyVisuals)
            return;

        SetObjectsActive(_enableWhenPowered, powered);
        SetObjectsActive(_disableWhenPowered, !powered);

        if (_lights != null)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                    _lights[i].enabled = powered;
            }
        }

        if (_particles != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystem particle = _particles[i];
                if (particle == null)
                    continue;

                if (powered)
                    particle.Play(true);
                else
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (_renderers != null && (_poweredMaterial != null || _unpoweredMaterial != null))
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer targetRenderer = _renderers[i];
                if (targetRenderer == null)
                    continue;

                if (powered && _poweredMaterial != null)
                    targetRenderer.sharedMaterial = _poweredMaterial;
                else if (!powered && _unpoweredMaterial != null)
                    targetRenderer.sharedMaterial = _unpoweredMaterial;
                else if (!powered && _unpoweredMaterial == null && _initialSharedMaterials != null && i < _initialSharedMaterials.Length)
                    targetRenderer.sharedMaterial = _initialSharedMaterials[i];
            }
        }
    }

    private void CacheInitialMaterials()
    {
        if (_renderers == null || _renderers.Length == 0)
            return;

        _initialSharedMaterials = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _initialSharedMaterials[i] = _renderers[i].sharedMaterial;
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_disabled)
            Gizmos.color = _disabledGizmoColor;
        else if (_isPowered)
            Gizmos.color = _poweredGizmoColor;
        else
            Gizmos.color = _unpoweredGizmoColor;

        int count = ConnectionPointCount;
        for (int i = 0; i < count; i++)
        {
            Vector3 point = GetConnectionPointWorldPosition(i);
            Gizmos.DrawSphere(point, ConnectionRadius * 0.18f);
            Gizmos.DrawWireSphere(point, ConnectionRadius);
            Gizmos.DrawLine(transform.position, point);
        }
    }

    [Serializable]
    public class BoolEvent : UnityEvent<bool> { }
}
