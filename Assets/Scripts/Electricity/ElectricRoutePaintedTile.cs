using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ElectricRoutePaintedTile : MonoBehaviour, ISerializationCallbackReceiver
{
    [Flags]
    public enum ConnectionMask
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3
    }

    [SerializeField] private Vector2Int _gridPosition;
    [SerializeField] private ConnectionMask _connections;

    [SerializeField, HideInInspector] private bool _north;
    [SerializeField, HideInInspector] private bool _east;
    [SerializeField, HideInInspector] private bool _south;
    [SerializeField, HideInInspector] private bool _west;

    public Vector2Int GridPosition
    {
        get => _gridPosition;
        set => _gridPosition = value;
    }

    public ConnectionMask Connections
    {
        get
        {
            if (_connections == ConnectionMask.None && HasAnyLegacyConnection())
                _connections = BuildMaskFromLegacyBooleans();

            return _connections;
        }
        set
        {
            _connections = value;
            SyncLegacyBooleansFromMask();
        }
    }

    public bool North => HasConnection(ConnectionMask.North);
    public bool East => HasConnection(ConnectionMask.East);
    public bool South => HasConnection(ConnectionMask.South);
    public bool West => HasConnection(ConnectionMask.West);

    public int ConnectionCount
    {
        get
        {
            ConnectionMask mask = Connections;
            int count = 0;
            if ((mask & ConnectionMask.North) != 0) count++;
            if ((mask & ConnectionMask.East) != 0) count++;
            if ((mask & ConnectionMask.South) != 0) count++;
            if ((mask & ConnectionMask.West) != 0) count++;
            return count;
        }
    }

    public void SetGridPosition(Vector2Int gridPosition)
    {
        _gridPosition = gridPosition;
    }

    public void SetConnections(bool north, bool east, bool south, bool west)
    {
        ConnectionMask mask = ConnectionMask.None;
        if (north) mask |= ConnectionMask.North;
        if (east) mask |= ConnectionMask.East;
        if (south) mask |= ConnectionMask.South;
        if (west) mask |= ConnectionMask.West;
        Connections = mask;
    }

    public bool HasConnection(ConnectionMask connection)
    {
        return (Connections & connection) != 0;
    }

    public bool HasConnection(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
            return HasConnection(ConnectionMask.North);
        if (direction == Vector2Int.right)
            return HasConnection(ConnectionMask.East);
        if (direction == Vector2Int.down)
            return HasConnection(ConnectionMask.South);
        if (direction == Vector2Int.left)
            return HasConnection(ConnectionMask.West);

        return false;
    }

    public void AddConnection(ConnectionMask connection)
    {
        Connections = Connections | connection;
    }

    public void RemoveConnection(ConnectionMask connection)
    {
        Connections = Connections & ~connection;
    }

    public void ClearConnections()
    {
        Connections = ConnectionMask.None;
    }

    public void InferGridPositionFromWorld(float cellSize)
    {
        float safeCellSize = Mathf.Max(0.001f, cellSize);
        Vector3 position = transform.position;
        _gridPosition = new Vector2Int(Mathf.RoundToInt(position.x / safeCellSize), Mathf.RoundToInt(position.z / safeCellSize));
    }

    public void OnBeforeSerialize()
    {
        SyncLegacyBooleansFromMask();
    }

    public void OnAfterDeserialize()
    {
        if (_connections == ConnectionMask.None && HasAnyLegacyConnection())
            _connections = BuildMaskFromLegacyBooleans();

        SyncLegacyBooleansFromMask();
    }

    private void OnValidate()
    {
        if (_connections == ConnectionMask.None && HasAnyLegacyConnection())
            _connections = BuildMaskFromLegacyBooleans();

        SyncLegacyBooleansFromMask();
    }

    private bool HasAnyLegacyConnection()
    {
        return _north || _east || _south || _west;
    }

    private ConnectionMask BuildMaskFromLegacyBooleans()
    {
        ConnectionMask mask = ConnectionMask.None;
        if (_north) mask |= ConnectionMask.North;
        if (_east) mask |= ConnectionMask.East;
        if (_south) mask |= ConnectionMask.South;
        if (_west) mask |= ConnectionMask.West;
        return mask;
    }

    private void SyncLegacyBooleansFromMask()
    {
        _north = (_connections & ConnectionMask.North) != 0;
        _east = (_connections & ConnectionMask.East) != 0;
        _south = (_connections & ConnectionMask.South) != 0;
        _west = (_connections & ConnectionMask.West) != 0;
    }
}
