using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class ElectricityNetworkManager : MonoBehaviour
{
    private static readonly List<ElectricityNetworkManager> Instances = new List<ElectricityNetworkManager>();

    [Header("Auto")]
    [SerializeField] private bool _autoFindNodesOnStart = true;
    [SerializeField] private bool _autoRebuildWhenRequested = true;
    [SerializeField] private float _recalculateInterval = 0.05f;

    [Header("Connection")]
    [SerializeField] private float _extraConnectionTolerance = 0.02f;

    [Header("Debug")]
    [SerializeField] private bool _drawConnections;
    [SerializeField] private Color _connectionColor = new Color(0.1f, 0.9f, 1f, 0.75f);
    [SerializeField] private List<ElectricNode> _nodes = new List<ElectricNode>();

    private bool _needsRebuild;
    private bool _needsRecalculate;
    private float _nextRecalculateTime;

    private void OnEnable()
    {
        if (!Instances.Contains(this))
            Instances.Add(this);
    }

    private void OnDisable()
    {
        Instances.Remove(this);
    }

    private void Start()
    {
        if (_autoFindNodesOnStart || _nodes.Count == 0)
            FindNodesInScene();

        RebuildNetwork();
    }

    private void Update()
    {
        if (!_autoRebuildWhenRequested)
            return;

        if (_needsRebuild)
        {
            RebuildNetwork();
            return;
        }

        if (_needsRecalculate && Time.time >= _nextRecalculateTime)
            RecalculatePower();
    }

    public static void RequestRebuildAll()
    {
        for (int i = 0; i < Instances.Count; i++)
        {
            if (Instances[i] != null)
                Instances[i].RequestRebuild();
        }
    }

    public static void RequestRecalculateAll()
    {
        for (int i = 0; i < Instances.Count; i++)
        {
            if (Instances[i] != null)
                Instances[i].RequestRecalculate();
        }
    }

    public void RequestRebuild()
    {
        _needsRebuild = true;
    }

    public void RequestRecalculate()
    {
        _needsRecalculate = true;
        _nextRecalculateTime = Time.time + Mathf.Max(0f, _recalculateInterval);
    }

    [ContextMenu("Find Nodes In Scene")]
    public void FindNodesInScene()
    {
        _nodes.Clear();
#if UNITY_2023_1_OR_NEWER
        ElectricNode[] nodes = FindObjectsByType<ElectricNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        ElectricNode[] nodes = FindObjectsOfType<ElectricNode>();
#endif
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].isActiveAndEnabled && !_nodes.Contains(nodes[i]))
                _nodes.Add(nodes[i]);
        }
    }

    [ContextMenu("Rebuild Network")]
    public void RebuildNetwork()
    {
        _needsRebuild = false;
        _needsRecalculate = false;

        if (_autoFindNodesOnStart || _nodes.Count == 0)
            FindNodesInScene();

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i] != null)
                _nodes[i].ClearRuntimeConnections();
        }

        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode a = _nodes[i];
            if (a == null || !a.isActiveAndEnabled)
                continue;

            for (int j = i + 1; j < _nodes.Count; j++)
            {
                ElectricNode b = _nodes[j];
                if (b == null || !b.isActiveAndEnabled)
                    continue;

                if (AreNodesConnected(a, b))
                {
                    a.AddRuntimeConnection(b);
                    b.AddRuntimeConnection(a);
                }
            }
        }

        RecalculatePower();
    }

    [ContextMenu("Recalculate Power")]
    public void RecalculatePower()
    {
        _needsRecalculate = false;

        HashSet<ElectricNode> powered = new HashSet<ElectricNode>();
        Queue<ElectricNode> queue = new Queue<ElectricNode>();

        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode node = _nodes[i];
            if (node == null || !node.isActiveAndEnabled)
                continue;

            if (node.IsActivePowerSource)
            {
                powered.Add(node);
                queue.Enqueue(node);
            }
        }

        while (queue.Count > 0)
        {
            ElectricNode current = queue.Dequeue();
            if (!current.CanOutputPower && !current.IsActivePowerSource)
                continue;

            IReadOnlyList<ElectricNode> connected = current.ConnectedNodes;
            for (int i = 0; i < connected.Count; i++)
            {
                ElectricNode next = connected[i];
                if (next == null || powered.Contains(next) || !next.CanReceivePower)
                    continue;

                powered.Add(next);

                if (next.CanOutputPower)
                    queue.Enqueue(next);
            }
        }

        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode node = _nodes[i];
            if (node != null)
                node.SetPoweredFromNetwork(powered.Contains(node));
        }
    }

    private bool AreNodesConnected(ElectricNode a, ElectricNode b)
    {
        int aCount = a.ConnectionPointCount;
        int bCount = b.ConnectionPointCount;

        if (aCount <= 0 || bCount <= 0)
            return false;

        float radius = a.ConnectionRadius + b.ConnectionRadius + _extraConnectionTolerance;
        float sqrRadius = radius * radius;

        for (int i = 0; i < aCount; i++)
        {
            Vector3 aPoint = a.GetConnectionPointWorldPosition(i);
            for (int j = 0; j < bCount; j++)
            {
                Vector3 bPoint = b.GetConnectionPointWorldPosition(j);
                if ((aPoint - bPoint).sqrMagnitude <= sqrRadius)
                    return true;
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (!_drawConnections || _nodes == null)
            return;

        Gizmos.color = _connectionColor;
        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode node = _nodes[i];
            if (node == null)
                continue;

            IReadOnlyList<ElectricNode> connected = node.ConnectedNodes;
            for (int j = 0; j < connected.Count; j++)
            {
                ElectricNode other = connected[j];
                if (other == null || other.GetInstanceID() < node.GetInstanceID())
                    continue;

                Gizmos.DrawLine(node.GetBestWorldConnectionPoint(other.transform.position), other.GetBestWorldConnectionPoint(node.transform.position));
            }
        }
    }
}
