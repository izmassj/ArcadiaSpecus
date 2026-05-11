using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class ElectricityNetworkManager : MonoBehaviour
{
    private static readonly List<ElectricityNetworkManager> Instances = new List<ElectricityNetworkManager>();

    [Header("Auto")]
    [SerializeField] private bool _autoFindNodesOnStart = true;
    [SerializeField] private bool _autoRebuildWhenRequested = true;
    [SerializeField] private float _recalculateInterval = 0.05f;
    [SerializeField] private bool _recalculateEveryFrame;

    [Header("Connection")]
    [SerializeField] private float _extraConnectionTolerance = 0.05f;
    [SerializeField] private bool _connectPaintedRouteTiles = true;
    [SerializeField] private bool _connectExternalNodesToPaintedRoutes = true;
    [SerializeField] private float _externalRouteConnectDistance = 3f;
    [SerializeField] private bool _fallbackConnectRoutesByDistance = true;
    [SerializeField] private float _routeRouteFallbackConnectDistance = 1.15f;

    [Header("Debug")]
    [SerializeField] private bool _drawConnections;
    [SerializeField] private Color _connectionColor = new Color(0.1f, 0.9f, 1f, 0.75f);
    [SerializeField] private List<ElectricNode> _nodes = new List<ElectricNode>();

    private readonly Dictionary<Vector2Int, ElectricRoutePaintedTile> _paintedTiles = new Dictionary<Vector2Int, ElectricRoutePaintedTile>();
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
        if (_autoFindNodesOnStart)
            RebuildNetwork();
        else
            RecalculatePower();
    }

    private void Update()
    {
        if (_needsRebuild && _autoRebuildWhenRequested)
            RebuildNetwork();

        if (_recalculateEveryFrame)
        {
            RecalculatePower();
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
        _needsRecalculate = true;
    }

    public void RequestRecalculate()
    {
        _needsRecalculate = true;
        _nextRecalculateTime = Mathf.Min(_nextRecalculateTime, Time.time + Mathf.Max(0f, _recalculateInterval));
    }

    [ContextMenu("Rebuild Network")]
    public void RebuildNetwork()
    {
        _needsRebuild = false;
        _needsRecalculate = false;

        if (_autoFindNodesOnStart || _nodes == null || _nodes.Count == 0)
            FindNodesInScene();

        ClearConnections();
        ConnectByConnectionPoints();

        if (_connectPaintedRouteTiles)
            ConnectPaintedRouteTiles();

        if (_fallbackConnectRoutesByDistance)
            ConnectRouteNodesByDistanceFallback();

        if (_connectExternalNodesToPaintedRoutes)
            ConnectExternalNodesToNearestRoutes();

        RecalculatePower();
    }

    [ContextMenu("Recalculate Power")]
    public void RecalculatePower()
    {
        _needsRecalculate = false;
        _nextRecalculateTime = Time.time + Mathf.Max(0f, _recalculateInterval);

        if (_nodes == null)
            return;

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i] != null)
                _nodes[i].SetPoweredFromNetwork(false);
        }

        Queue<ElectricNode> queue = new Queue<ElectricNode>();
        HashSet<ElectricNode> visited = new HashSet<ElectricNode>();

        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode node = _nodes[i];
            if (node == null || !node.IsActivePowerSource)
                continue;

            node.SetPoweredFromNetwork(true);
            queue.Enqueue(node);
            visited.Add(node);
        }

        while (queue.Count > 0)
        {
            ElectricNode current = queue.Dequeue();

            if (!current.CanOutputPower)
                continue;

            IReadOnlyList<ElectricNode> connectedNodes = current.ConnectedNodes;
            for (int i = 0; i < connectedNodes.Count; i++)
            {
                ElectricNode next = connectedNodes[i];
                if (next == null || visited.Contains(next) || !next.CanReceivePower)
                    continue;

                next.SetPoweredFromNetwork(true);
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
    }

    private void FindNodesInScene()
    {
#if UNITY_2023_1_OR_NEWER
        ElectricNode[] found = FindObjectsByType<ElectricNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        ElectricNode[] found = FindObjectsOfType<ElectricNode>();
#endif
        _nodes.Clear();
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && found[i].isActiveAndEnabled)
                _nodes.Add(found[i]);
        }
    }

    private void ClearConnections()
    {
        if (_nodes == null)
            return;

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i] != null)
                _nodes[i].ClearRuntimeConnections();
        }
    }

    private void ConnectByConnectionPoints()
    {
        if (_nodes == null)
            return;

        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode a = _nodes[i];
            if (a == null)
                continue;

            for (int j = i + 1; j < _nodes.Count; j++)
            {
                ElectricNode b = _nodes[j];
                if (b == null)
                    continue;

                if (NodesAreCloseEnough(a, b))
                    Connect(a, b);
            }
        }
    }

    private bool NodesAreCloseEnough(ElectricNode a, ElectricNode b)
    {
        int aCount = a.ConnectionPointCount;
        int bCount = b.ConnectionPointCount;

        if (aCount <= 0 || bCount <= 0)
            return false;

        for (int i = 0; i < aCount; i++)
        {
            Vector3 aPoint = a.GetConnectionPointWorldPosition(i);

            for (int j = 0; j < bCount; j++)
            {
                Vector3 bPoint = b.GetConnectionPointWorldPosition(j);
                float radius = a.ConnectionRadius + b.ConnectionRadius + _extraConnectionTolerance;
                if ((aPoint - bPoint).sqrMagnitude <= radius * radius)
                    return true;
            }
        }

        return false;
    }

    private void ConnectPaintedRouteTiles()
    {
#if UNITY_2023_1_OR_NEWER
        ElectricRoutePaintedTile[] tiles = FindObjectsByType<ElectricRoutePaintedTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        ElectricRoutePaintedTile[] tiles = FindObjectsOfType<ElectricRoutePaintedTile>();
#endif
        _paintedTiles.Clear();

        for (int i = 0; i < tiles.Length; i++)
        {
            ElectricRoutePaintedTile tile = tiles[i];
            if (tile == null)
                continue;

            _paintedTiles[tile.GridPosition] = tile;
        }

        foreach (KeyValuePair<Vector2Int, ElectricRoutePaintedTile> pair in _paintedTiles)
        {
            ElectricRoutePaintedTile tile = pair.Value;
            ElectricNode node = tile.GetComponent<ElectricNode>();
            if (node == null)
                continue;

            TryConnectPaintedDirection(tile, node, Vector2Int.up);
            TryConnectPaintedDirection(tile, node, Vector2Int.right);
            TryConnectPaintedDirection(tile, node, Vector2Int.down);
            TryConnectPaintedDirection(tile, node, Vector2Int.left);
        }
    }

    private void TryConnectPaintedDirection(ElectricRoutePaintedTile tile, ElectricNode node, Vector2Int direction)
    {
        if (!tile.HasConnection(direction))
            return;

        if (!_paintedTiles.TryGetValue(tile.GridPosition + direction, out ElectricRoutePaintedTile otherTile) || otherTile == null)
            return;

        if (!otherTile.HasConnection(-direction))
            return;

        ElectricNode otherNode = otherTile.GetComponent<ElectricNode>();
        if (otherNode != null)
            Connect(node, otherNode);
    }

    private void ConnectRouteNodesByDistanceFallback()
    {
        List<ElectricRouteNode> routes = GetRouteNodes();
        float maxDistanceSqr = _routeRouteFallbackConnectDistance * _routeRouteFallbackConnectDistance;

        for (int i = 0; i < routes.Count; i++)
        {
            ElectricRouteNode a = routes[i];
            if (a == null)
                continue;

            for (int j = i + 1; j < routes.Count; j++)
            {
                ElectricRouteNode b = routes[j];
                if (b == null)
                    continue;

                Vector3 delta = a.transform.position - b.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= maxDistanceSqr)
                    Connect(a, b);
            }
        }
    }

    private void ConnectExternalNodesToNearestRoutes()
    {
        List<ElectricRouteNode> routes = GetRouteNodes();
        if (routes.Count == 0 || _nodes == null)
            return;

        float maxDistanceSqr = _externalRouteConnectDistance * _externalRouteConnectDistance;

        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode node = _nodes[i];
            if (node == null || node is ElectricRouteNode)
                continue;

            Vector3 nodeCenter = node.GetApproximateCenter();
            ElectricRouteNode bestRoute = null;
            float bestDistance = float.MaxValue;

            for (int j = 0; j < routes.Count; j++)
            {
                ElectricRouteNode route = routes[j];
                if (route == null)
                    continue;

                Vector3 routeCenter = route.GetApproximateCenter();
                Vector3 delta = nodeCenter - routeCenter;
                delta.y = 0f;
                float distance = delta.sqrMagnitude;

                if (distance < bestDistance && distance <= maxDistanceSqr)
                {
                    bestDistance = distance;
                    bestRoute = route;
                }
            }

            if (bestRoute != null)
                Connect(node, bestRoute);
        }
    }

    private List<ElectricRouteNode> GetRouteNodes()
    {
        List<ElectricRouteNode> routes = new List<ElectricRouteNode>();
        if (_nodes == null)
            return routes;

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i] is ElectricRouteNode route)
                routes.Add(route);
        }

        return routes;
    }

    private static void Connect(ElectricNode a, ElectricNode b)
    {
        if (a == null || b == null || a == b)
            return;

        a.AddRuntimeConnection(b);
        b.AddRuntimeConnection(a);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawConnections || _nodes == null)
            return;

        Gizmos.color = _connectionColor;

        for (int i = 0; i < _nodes.Count; i++)
        {
            ElectricNode node = _nodes[i];
            if (node == null)
                continue;

            IReadOnlyList<ElectricNode> connectedNodes = node.ConnectedNodes;
            for (int j = 0; j < connectedNodes.Count; j++)
            {
                ElectricNode other = connectedNodes[j];
                if (other != null)
                    Gizmos.DrawLine(node.GetApproximateCenter(), other.GetApproximateCenter());
            }
        }
    }
}
