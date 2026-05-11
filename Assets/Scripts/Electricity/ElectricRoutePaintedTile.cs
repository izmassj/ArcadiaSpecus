using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ElectricRoutePaintedTile : MonoBehaviour
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

    public Vector2Int GridPosition
    {
        get => _gridPosition;
        set => _gridPosition = value;
    }

    public ConnectionMask Connections
    {
        get => _connections;
        set => _connections = value;
    }

    public bool HasConnection(ConnectionMask connection)
    {
        return (_connections & connection) != 0;
    }

    public void AddConnection(ConnectionMask connection)
    {
        _connections |= connection;
    }

    public void RemoveConnection(ConnectionMask connection)
    {
        _connections &= ~connection;
    }

    public void ClearConnections()
    {
        _connections = ConnectionMask.None;
    }
}
