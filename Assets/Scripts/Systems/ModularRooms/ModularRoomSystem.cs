using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ModularRoomSystem : MonoBehaviour
{
    [SerializeField] private List<IRoomPrefab> roomPrefabs;
    [SerializeField] private LayerMask roomLayerMask;
    [SerializeField] private Color ghostColor;

    private RoomKind roomKind;
    private GameObject currentGhost;
    private Dictionary<RoomKind,  GameObject> roomPrefabDict;

    // Start is called before the first frame update
    void Start()
    {
        roomPrefabDict = new Dictionary<RoomKind, GameObject>();
        foreach (var roomPrefab in roomPrefabs)
        {
            roomPrefabDict[roomPrefab.kind] = roomPrefab.prefab;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
