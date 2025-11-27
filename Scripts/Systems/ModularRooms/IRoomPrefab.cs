using UnityEngine;

public interface IRoomPrefab
{
    public RoomKind kind { get; set; }
    public GameObject prefab { get; set; }
}