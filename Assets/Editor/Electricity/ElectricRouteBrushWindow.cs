using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElectricRouteBrushWindow : EditorWindow
{
    private enum BrushMode
    {
        Paint,
        Erase
    }

    private enum DesiredShape
    {
        Straight,
        Corner,
        ThreeWay,
        FourWay
    }

    private struct TileConfig
    {
        public DesiredShape Shape;
        public float YRotation;
        public ElectricRouteNode.RouteShape RouteShape;

        public TileConfig(DesiredShape shape, float yRotation, ElectricRouteNode.RouteShape routeShape)
        {
            Shape = shape;
            YRotation = yRotation;
            RouteShape = routeShape;
        }
    }

    [SerializeField] private Transform _routeParent;
    [SerializeField] private GameObject _straightPrefab;
    [SerializeField] private GameObject _cornerPrefab;
    [SerializeField] private GameObject _threeWayPrefab;
    [SerializeField] private GameObject _fourWayPrefab;

    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector3 _gridOrigin;
    [SerializeField] private float _placementY;
    [SerializeField] private BrushMode _mode = BrushMode.Paint;

    [SerializeField] private bool _paintOnDrag = true;
    [SerializeField] private bool _autoRebuildAfterEdit = true;
    [SerializeField] private bool _forceRouteNodePreset = true;
    [SerializeField] private bool _snapToGrid = true;

    [SerializeField] private bool _usePaintedConnections = true;
    [SerializeField] private bool _connectSingleClickToNeighbours = true;
    [SerializeField] private bool _drawConnectionDebug = true;

    [SerializeField] private Color _paintPreviewColor = new Color(0.1f, 0.9f, 1f, 0.35f);
    [SerializeField] private Color _erasePreviewColor = new Color(1f, 0.25f, 0.1f, 0.35f);

    private readonly Dictionary<Vector2Int, ElectricRoutePaintedTile> _tiles = new Dictionary<Vector2Int, ElectricRoutePaintedTile>();
    private Vector2Int _lastEditedCell;
    private bool _hasLastEditedCell;
    private Vector2Int _lastPaintPathCell;
    private bool _hasLastPaintPathCell;

    [MenuItem("Tools/Arcadia/Electric Route Brush")]
    public static void Open()
    {
        ElectricRouteBrushWindow window = GetWindow<ElectricRouteBrushWindow>("Electric Route Brush");
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGui;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Route Prefabs", EditorStyles.boldLabel);
        _routeParent = (Transform)EditorGUILayout.ObjectField("Route Parent", _routeParent, typeof(Transform), true);
        _straightPrefab = (GameObject)EditorGUILayout.ObjectField("Straight Prefab", _straightPrefab, typeof(GameObject), false);
        _cornerPrefab = (GameObject)EditorGUILayout.ObjectField("Corner Prefab", _cornerPrefab, typeof(GameObject), false);
        _threeWayPrefab = (GameObject)EditorGUILayout.ObjectField("Three Way Prefab", _threeWayPrefab, typeof(GameObject), false);
        _fourWayPrefab = (GameObject)EditorGUILayout.ObjectField("Four Way Prefab", _fourWayPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        _cellSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Cell Size", _cellSize));
        _gridOrigin = EditorGUILayout.Vector3Field("Grid Origin", _gridOrigin);
        _placementY = EditorGUILayout.FloatField("Placement Y", _placementY);
        _snapToGrid = EditorGUILayout.Toggle("Snap To Grid", _snapToGrid);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
        _mode = (BrushMode)EditorGUILayout.EnumPopup("Mode", _mode);
        _paintOnDrag = EditorGUILayout.Toggle("Paint On Drag", _paintOnDrag);
        _autoRebuildAfterEdit = EditorGUILayout.Toggle("Auto Rebuild Shapes", _autoRebuildAfterEdit);
        _forceRouteNodePreset = EditorGUILayout.Toggle("Force ElectricRouteNode Preset", _forceRouteNodePreset);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Connections", EditorStyles.boldLabel);
        _usePaintedConnections = EditorGUILayout.Toggle("Use Painted Connections", _usePaintedConnections);
        _connectSingleClickToNeighbours = EditorGUILayout.Toggle("Connect Single Click To Neighbours", _connectSingleClickToNeighbours);
        _drawConnectionDebug = EditorGUILayout.Toggle("Draw Connection Debug", _drawConnectionDebug);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Parent"))
                ScanTiles();

            if (GUILayout.Button("Rebuild Shapes"))
                RebuildAllTiles();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Infer Connections From Neighbours"))
                InferConnectionsFromNeighbourTiles();

            if (GUILayout.Button("Clear Painted Connections"))
                ClearAllPaintedConnections();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selected As Parent"))
            {
                if (Selection.activeTransform != null)
                    _routeParent = Selection.activeTransform;
            }

            if (GUILayout.Button("Create Parent"))
                CreateRouteParent();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox("Scene View: Left Click paints/erases cells on the XZ grid. Hold Alt to orbit the scene camera normally. The brush now stores explicit painted connections, so corners are rebuilt from the path you draw instead of only guessing from nearby tiles.", MessageType.Info);
    }

    private void DuringSceneGui(SceneView sceneView)
    {
        Event current = Event.current;
        if (current == null)
            return;

        if (_routeParent == null)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        if (!TryGetGridPoint(ray, out Vector3 worldPoint, out Vector2Int cell))
            return;

        DrawPreview(cell);
        if (_drawConnectionDebug)
            DrawPaintedConnections();

        if (current.alt)
            return;

        bool isLeftMouse = current.button == 0;
        bool mouseDown = isLeftMouse && current.type == EventType.MouseDown;
        bool mouseDrag = isLeftMouse && current.type == EventType.MouseDrag && _paintOnDrag;
        bool mouseUp = isLeftMouse && current.type == EventType.MouseUp;

        if (mouseUp)
        {
            _hasLastEditedCell = false;
            _hasLastPaintPathCell = false;
            return;
        }

        if (!mouseDown && !mouseDrag)
            return;

        if (_hasLastEditedCell && _lastEditedCell == cell && mouseDrag)
        {
            current.Use();
            return;
        }

        _lastEditedCell = cell;
        _hasLastEditedCell = true;

        if (_mode == BrushMode.Paint)
        {
            if (mouseDown)
                BeginPaintPath(cell);
            else
                ContinuePaintPath(cell);
        }
        else
        {
            EraseCell(cell);
        }

        current.Use();
        SceneView.RepaintAll();
    }

    private bool TryGetGridPoint(Ray ray, out Vector3 worldPoint, out Vector2Int cell)
    {
        worldPoint = Vector3.zero;
        cell = Vector2Int.zero;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, _placementY, 0f));
        if (!plane.Raycast(ray, out float enter))
            return false;

        worldPoint = ray.GetPoint(enter);

        float x = (worldPoint.x - _gridOrigin.x) / _cellSize;
        float z = (worldPoint.z - _gridOrigin.z) / _cellSize;

        if (_snapToGrid)
            cell = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(z));
        else
            cell = new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(z));

        return true;
    }

    private Vector3 GetCellWorldPosition(Vector2Int cell)
    {
        return new Vector3(
            _gridOrigin.x + cell.x * _cellSize,
            _placementY,
            _gridOrigin.z + cell.y * _cellSize
        );
    }

    private void DrawPreview(Vector2Int cell)
    {
        Vector3 center = GetCellWorldPosition(cell);
        Color color = _mode == BrushMode.Paint ? _paintPreviewColor : _erasePreviewColor;

        Handles.color = color;
        Vector3 size = new Vector3(_cellSize, 0.02f, _cellSize);
        Handles.DrawSolidRectangleWithOutline(new[]
        {
            center + new Vector3(-size.x * 0.5f, 0f, -size.z * 0.5f),
            center + new Vector3(-size.x * 0.5f, 0f, size.z * 0.5f),
            center + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f),
            center + new Vector3(size.x * 0.5f, 0f, -size.z * 0.5f)
        }, color, new Color(color.r, color.g, color.b, 0.9f));

        Handles.Label(center + Vector3.up * 0.2f, cell.ToString());
    }

    private void DrawPaintedConnections()
    {
        ScanTiles();
        Handles.color = new Color(0.2f, 0.85f, 1f, 0.9f);

        foreach (KeyValuePair<Vector2Int, ElectricRoutePaintedTile> pair in _tiles)
        {
            ElectricRoutePaintedTile tile = pair.Value;
            if (tile == null)
                continue;

            ElectricRoutePaintedTile.ConnectionMask mask = GetConnectionMask(pair.Key, tile);
            Vector3 center = GetCellWorldPosition(pair.Key) + Vector3.up * 0.06f;
            float length = _cellSize * 0.42f;

            if ((mask & ElectricRoutePaintedTile.ConnectionMask.North) != 0)
                Handles.DrawLine(center, center + Vector3.forward * length);
            if ((mask & ElectricRoutePaintedTile.ConnectionMask.East) != 0)
                Handles.DrawLine(center, center + Vector3.right * length);
            if ((mask & ElectricRoutePaintedTile.ConnectionMask.South) != 0)
                Handles.DrawLine(center, center + Vector3.back * length);
            if ((mask & ElectricRoutePaintedTile.ConnectionMask.West) != 0)
                Handles.DrawLine(center, center + Vector3.left * length);
        }
    }

    private void BeginPaintPath(Vector2Int cell)
    {
        ScanTiles();
        ElectricRoutePaintedTile tile = EnsurePaintedTile(cell);
        if (tile == null)
            return;

        if (_connectSingleClickToNeighbours)
            ConnectTileToExistingNeighbours(cell);

        _lastPaintPathCell = cell;
        _hasLastPaintPathCell = true;

        if (_autoRebuildAfterEdit)
            RebuildTouchedArea(cell);

        MarkSceneDirty();
    }

    private void ContinuePaintPath(Vector2Int targetCell)
    {
        ScanTiles();

        if (!_hasLastPaintPathCell)
        {
            BeginPaintPath(targetCell);
            return;
        }

        if (_lastPaintPathCell == targetCell)
            return;

        List<Vector2Int> path = GetManhattanPath(_lastPaintPathCell, targetCell);
        Vector2Int previous = _lastPaintPathCell;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int next = path[i];
            EnsurePaintedTile(previous);
            EnsurePaintedTile(next);
            ConnectAdjacentCells(previous, next);
            previous = next;
        }

        _lastPaintPathCell = targetCell;

        if (_autoRebuildAfterEdit)
            RebuildAllTiles();

        MarkSceneDirty();
    }

    private List<Vector2Int> GetManhattanPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int current = start;

        while (current != end)
        {
            int dx = end.x - current.x;
            int dz = end.y - current.y;
            Vector2Int step;

            if (Mathf.Abs(dx) >= Mathf.Abs(dz) && dx != 0)
                step = new Vector2Int(dx > 0 ? 1 : -1, 0);
            else if (dz != 0)
                step = new Vector2Int(0, dz > 0 ? 1 : -1);
            else
                step = new Vector2Int(dx > 0 ? 1 : -1, 0);

            current += step;
            result.Add(current);
        }

        return result;
    }

    private ElectricRoutePaintedTile EnsurePaintedTile(Vector2Int cell)
    {
        if (_tiles.TryGetValue(cell, out ElectricRoutePaintedTile existing) && existing != null)
            return existing;

        GameObject prefab = _straightPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("Electric Route Brush: Straight Prefab is missing.");
            return null;
        }

        GameObject instance = InstantiatePrefab(prefab, _routeParent);
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Paint Electric Route Tile");
        instance.name = $"ElectricRoute_{cell.x}_{cell.y}";
        instance.transform.position = GetCellWorldPosition(cell);
        instance.transform.rotation = Quaternion.identity;

        ElectricRoutePaintedTile marker = instance.GetComponent<ElectricRoutePaintedTile>();
        if (marker == null)
            marker = Undo.AddComponent<ElectricRoutePaintedTile>(instance);

        Undo.RecordObject(marker, "Set Electric Route Tile Data");
        marker.GridPosition = cell;
        EditorUtility.SetDirty(marker);

        EnsureElectricRouteNode(instance);
        _tiles[cell] = marker;
        return marker;
    }

    private void ConnectTileToExistingNeighbours(Vector2Int cell)
    {
        Vector2Int[] neighbours =
        {
            cell + Vector2Int.up,
            cell + Vector2Int.right,
            cell + Vector2Int.down,
            cell + Vector2Int.left
        };

        for (int i = 0; i < neighbours.Length; i++)
        {
            if (_tiles.ContainsKey(neighbours[i]))
                ConnectAdjacentCells(cell, neighbours[i]);
        }
    }

    private void ConnectAdjacentCells(Vector2Int a, Vector2Int b)
    {
        Vector2Int delta = b - a;
        if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
            return;

        if (!_tiles.TryGetValue(a, out ElectricRoutePaintedTile tileA) || tileA == null)
            return;

        if (!_tiles.TryGetValue(b, out ElectricRoutePaintedTile tileB) || tileB == null)
            return;

        DirectionToMasks(delta, out ElectricRoutePaintedTile.ConnectionMask maskA, out ElectricRoutePaintedTile.ConnectionMask maskB);

        Undo.RecordObject(tileA, "Connect Electric Route Tile");
        Undo.RecordObject(tileB, "Connect Electric Route Tile");

        tileA.AddConnection(maskA);
        tileB.AddConnection(maskB);

        EditorUtility.SetDirty(tileA);
        EditorUtility.SetDirty(tileB);
    }

    private void EraseCell(Vector2Int cell)
    {
        ScanTiles();
        if (!_tiles.TryGetValue(cell, out ElectricRoutePaintedTile tile) || tile == null)
            return;

        DisconnectNeighbour(cell, Vector2Int.up, ElectricRoutePaintedTile.ConnectionMask.South);
        DisconnectNeighbour(cell, Vector2Int.right, ElectricRoutePaintedTile.ConnectionMask.West);
        DisconnectNeighbour(cell, Vector2Int.down, ElectricRoutePaintedTile.ConnectionMask.North);
        DisconnectNeighbour(cell, Vector2Int.left, ElectricRoutePaintedTile.ConnectionMask.East);

        GameObject target = tile.gameObject;
        _tiles.Remove(cell);
        Undo.DestroyObjectImmediate(target);

        if (_autoRebuildAfterEdit)
            RebuildAllTiles();

        MarkSceneDirty();
    }

    private void DisconnectNeighbour(Vector2Int cell, Vector2Int direction, ElectricRoutePaintedTile.ConnectionMask neighbourMask)
    {
        Vector2Int neighbourCell = cell + direction;
        if (!_tiles.TryGetValue(neighbourCell, out ElectricRoutePaintedTile neighbour) || neighbour == null)
            return;

        Undo.RecordObject(neighbour, "Disconnect Electric Route Tile");
        neighbour.RemoveConnection(neighbourMask);
        EditorUtility.SetDirty(neighbour);
    }

    private void ScanTiles()
    {
        _tiles.Clear();
        if (_routeParent == null)
            return;

        ElectricRoutePaintedTile[] paintedTiles = _routeParent.GetComponentsInChildren<ElectricRoutePaintedTile>(true);
        for (int i = 0; i < paintedTiles.Length; i++)
        {
            ElectricRoutePaintedTile tile = paintedTiles[i];
            if (tile == null)
                continue;

            _tiles[tile.GridPosition] = tile;
        }
    }

    private void RebuildAllTiles()
    {
        if (_routeParent == null)
            return;

        ScanTiles();
        List<Vector2Int> cells = new List<Vector2Int>(_tiles.Keys);
        for (int i = 0; i < cells.Count; i++)
            RebuildTile(cells[i]);

        MarkSceneDirty();
    }

    private void RebuildTouchedArea(Vector2Int center)
    {
        RebuildTile(center);
        RebuildTile(center + Vector2Int.up);
        RebuildTile(center + Vector2Int.right);
        RebuildTile(center + Vector2Int.down);
        RebuildTile(center + Vector2Int.left);
    }

    private void RebuildTile(Vector2Int cell)
    {
        if (!_tiles.TryGetValue(cell, out ElectricRoutePaintedTile currentTile) || currentTile == null)
            return;

        TileConfig config = GetTileConfig(cell);
        GameObject desiredPrefab = GetPrefab(config.Shape);
        if (desiredPrefab == null)
        {
            Debug.LogWarning($"Electric Route Brush: Missing prefab for shape {config.Shape}.");
            return;
        }

        GameObject currentObject = currentTile.gameObject;
        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(currentObject) as GameObject;
        bool prefabMatches = sourcePrefab == desiredPrefab;

        if (!prefabMatches)
        {
            Vector3 position = GetCellWorldPosition(cell);
            Quaternion rotation = Quaternion.Euler(0f, config.YRotation, 0f);
            ElectricRoutePaintedTile.ConnectionMask savedConnections = currentTile.Connections;

            GameObject replacement = InstantiatePrefab(desiredPrefab, _routeParent);
            if (replacement == null)
                return;

            Undo.RegisterCreatedObjectUndo(replacement, "Replace Electric Route Tile Shape");
            replacement.name = $"ElectricRoute_{cell.x}_{cell.y}";
            replacement.transform.SetPositionAndRotation(position, rotation);

            ElectricRoutePaintedTile marker = replacement.GetComponent<ElectricRoutePaintedTile>();
            if (marker == null)
                marker = Undo.AddComponent<ElectricRoutePaintedTile>(replacement);

            Undo.RecordObject(marker, "Set Electric Route Tile Data");
            marker.GridPosition = cell;
            marker.Connections = savedConnections;
            EditorUtility.SetDirty(marker);

            EnsureElectricRouteNode(replacement);
            ApplyRouteNodePreset(replacement, config.RouteShape);

            _tiles[cell] = marker;
            Undo.DestroyObjectImmediate(currentObject);
        }
        else
        {
            Undo.RecordObject(currentObject.transform, "Update Electric Route Tile Transform");
            currentObject.transform.SetPositionAndRotation(GetCellWorldPosition(cell), Quaternion.Euler(0f, config.YRotation, 0f));
            EnsureElectricRouteNode(currentObject);
            ApplyRouteNodePreset(currentObject, config.RouteShape);
            EditorUtility.SetDirty(currentObject.transform);
        }
    }

    private TileConfig GetTileConfig(Vector2Int cell)
    {
        ElectricRoutePaintedTile tile = null;
        _tiles.TryGetValue(cell, out tile);
        ElectricRoutePaintedTile.ConnectionMask mask = GetConnectionMask(cell, tile);

        bool north = (mask & ElectricRoutePaintedTile.ConnectionMask.North) != 0;
        bool east = (mask & ElectricRoutePaintedTile.ConnectionMask.East) != 0;
        bool south = (mask & ElectricRoutePaintedTile.ConnectionMask.South) != 0;
        bool west = (mask & ElectricRoutePaintedTile.ConnectionMask.West) != 0;

        int count = 0;
        if (north) count++;
        if (south) count++;
        if (east) count++;
        if (west) count++;

        if (count >= 4)
            return new TileConfig(DesiredShape.FourWay, 0f, ElectricRouteNode.RouteShape.FourWay);

        if (count == 3)
        {
            if (!south)
                return new TileConfig(DesiredShape.ThreeWay, 0f, ElectricRouteNode.RouteShape.ThreeWayForwardLeftRight);
            if (!west)
                return new TileConfig(DesiredShape.ThreeWay, 90f, ElectricRouteNode.RouteShape.ThreeWayForwardLeftRight);
            if (!north)
                return new TileConfig(DesiredShape.ThreeWay, 180f, ElectricRouteNode.RouteShape.ThreeWayForwardLeftRight);
            return new TileConfig(DesiredShape.ThreeWay, -90f, ElectricRouteNode.RouteShape.ThreeWayForwardLeftRight);
        }

        if (count == 2)
        {
            if (north && south)
                return new TileConfig(DesiredShape.Straight, 0f, ElectricRouteNode.RouteShape.StraightZ);
            if (east && west)
                return new TileConfig(DesiredShape.Straight, 90f, ElectricRouteNode.RouteShape.StraightZ);
            // The corner prefab in this project has its visual 0º orientation as East + South.
            // The old mapping assumed 0º was North + East, so all corners appeared rotated one step wrong.
            // We keep the visual rotation aligned to the prefab and use CornerBackRight so ElectricRouteNode
            // connection points still match the painted path after the object rotation is applied.
            if (north && east)
                return new TileConfig(DesiredShape.Corner, -90f, ElectricRouteNode.RouteShape.CornerBackRight);
            if (east && south)
                return new TileConfig(DesiredShape.Corner, 0f, ElectricRouteNode.RouteShape.CornerBackRight);
            if (south && west)
                return new TileConfig(DesiredShape.Corner, 90f, ElectricRouteNode.RouteShape.CornerBackRight);
            return new TileConfig(DesiredShape.Corner, 180f, ElectricRouteNode.RouteShape.CornerBackRight);
        }

        if (count == 1)
        {
            if (east || west)
                return new TileConfig(DesiredShape.Straight, 90f, ElectricRouteNode.RouteShape.StraightZ);
            return new TileConfig(DesiredShape.Straight, 0f, ElectricRouteNode.RouteShape.StraightZ);
        }

        return new TileConfig(DesiredShape.Straight, 0f, ElectricRouteNode.RouteShape.StraightZ);
    }

    private ElectricRoutePaintedTile.ConnectionMask GetConnectionMask(Vector2Int cell, ElectricRoutePaintedTile tile)
    {
        if (_usePaintedConnections && tile != null && tile.Connections != ElectricRoutePaintedTile.ConnectionMask.None)
            return tile.Connections;

        return GetNeighbourInferredMask(cell);
    }

    private ElectricRoutePaintedTile.ConnectionMask GetNeighbourInferredMask(Vector2Int cell)
    {
        ElectricRoutePaintedTile.ConnectionMask mask = ElectricRoutePaintedTile.ConnectionMask.None;

        if (_tiles.ContainsKey(cell + Vector2Int.up))
            mask |= ElectricRoutePaintedTile.ConnectionMask.North;
        if (_tiles.ContainsKey(cell + Vector2Int.right))
            mask |= ElectricRoutePaintedTile.ConnectionMask.East;
        if (_tiles.ContainsKey(cell + Vector2Int.down))
            mask |= ElectricRoutePaintedTile.ConnectionMask.South;
        if (_tiles.ContainsKey(cell + Vector2Int.left))
            mask |= ElectricRoutePaintedTile.ConnectionMask.West;

        return mask;
    }

    private void InferConnectionsFromNeighbourTiles()
    {
        ScanTiles();

        foreach (KeyValuePair<Vector2Int, ElectricRoutePaintedTile> pair in _tiles)
        {
            if (pair.Value == null)
                continue;

            Undo.RecordObject(pair.Value, "Infer Electric Route Connections");
            pair.Value.Connections = GetNeighbourInferredMask(pair.Key);
            EditorUtility.SetDirty(pair.Value);
        }

        RebuildAllTiles();
        MarkSceneDirty();
    }

    private void ClearAllPaintedConnections()
    {
        ScanTiles();

        foreach (KeyValuePair<Vector2Int, ElectricRoutePaintedTile> pair in _tiles)
        {
            if (pair.Value == null)
                continue;

            Undo.RecordObject(pair.Value, "Clear Electric Route Connections");
            pair.Value.ClearConnections();
            EditorUtility.SetDirty(pair.Value);
        }

        RebuildAllTiles();
        MarkSceneDirty();
    }

    private static void DirectionToMasks(Vector2Int delta, out ElectricRoutePaintedTile.ConnectionMask fromMask, out ElectricRoutePaintedTile.ConnectionMask toMask)
    {
        if (delta == Vector2Int.up)
        {
            fromMask = ElectricRoutePaintedTile.ConnectionMask.North;
            toMask = ElectricRoutePaintedTile.ConnectionMask.South;
            return;
        }

        if (delta == Vector2Int.right)
        {
            fromMask = ElectricRoutePaintedTile.ConnectionMask.East;
            toMask = ElectricRoutePaintedTile.ConnectionMask.West;
            return;
        }

        if (delta == Vector2Int.down)
        {
            fromMask = ElectricRoutePaintedTile.ConnectionMask.South;
            toMask = ElectricRoutePaintedTile.ConnectionMask.North;
            return;
        }

        fromMask = ElectricRoutePaintedTile.ConnectionMask.West;
        toMask = ElectricRoutePaintedTile.ConnectionMask.East;
    }

    private GameObject GetPrefab(DesiredShape shape)
    {
        switch (shape)
        {
            case DesiredShape.Straight:
                return _straightPrefab;
            case DesiredShape.Corner:
                return _cornerPrefab != null ? _cornerPrefab : _straightPrefab;
            case DesiredShape.ThreeWay:
                return _threeWayPrefab != null ? _threeWayPrefab : _straightPrefab;
            case DesiredShape.FourWay:
                if (_fourWayPrefab != null)
                    return _fourWayPrefab;
                if (_threeWayPrefab != null)
                    return _threeWayPrefab;
                return _straightPrefab;
            default:
                return _straightPrefab;
        }
    }

    private static GameObject InstantiatePrefab(GameObject prefab, Transform parent)
    {
        Object obj = PrefabUtility.InstantiatePrefab(prefab, parent);
        return obj as GameObject;
    }

    private void EnsureElectricRouteNode(GameObject target)
    {
        if (target == null)
            return;

        ElectricRouteNode routeNode = target.GetComponent<ElectricRouteNode>();
        if (routeNode == null)
            Undo.AddComponent<ElectricRouteNode>(target);
    }

    private void ApplyRouteNodePreset(GameObject target, ElectricRouteNode.RouteShape shape)
    {
        if (!_forceRouteNodePreset || target == null)
            return;

        ElectricRouteNode routeNode = target.GetComponent<ElectricRouteNode>();
        if (routeNode == null)
            return;

        SerializedObject serializedObject = new SerializedObject(routeNode);
        SerializedProperty routeShapeProperty = serializedObject.FindProperty("_routeShape");
        if (routeShapeProperty != null)
            routeShapeProperty.enumValueIndex = (int)shape;

        SerializedProperty halfLengthProperty = serializedObject.FindProperty("_halfLength");
        if (halfLengthProperty != null)
            halfLengthProperty.floatValue = _cellSize * 0.5f;

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        routeNode.ApplyPreset();
        EditorUtility.SetDirty(routeNode);
    }

    private void CreateRouteParent()
    {
        GameObject parent = new GameObject("Electric Route Painted Parent");
        Undo.RegisterCreatedObjectUndo(parent, "Create Electric Route Parent");
        _routeParent = parent.transform;
        MarkSceneDirty();
    }

    private static void MarkSceneDirty()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            EditorSceneManager.MarkSceneDirty(activeScene);
    }
}
