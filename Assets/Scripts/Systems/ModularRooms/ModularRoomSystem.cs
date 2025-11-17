using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class ModularRoomSystem : MonoBehaviour
{
    public static ModularRoomSystem Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    [SerializeField] private List<RoomPrefab> roomPrefabs;
    [SerializeField] private LayerMask roomLayerMask;

    private RoomKind roomKind;

    private bool isBuilding = false;
    private GameObject currentRoom;
    private Dictionary<RoomKind, GameObject> roomPrefabDict;

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
    public void SpawnRoomPrefab(RoomKind room)
    {
        GameObject prefab = GetPrefab(room);
        if (prefab == null) return;

        // Instantiate and assign to currentRoom
        currentRoom = Instantiate(prefab);
        isBuilding = true;
    }

    void Update()
    {
        if (isBuilding && currentRoom != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Convert screen position to world position
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));

            currentRoom.transform.position = worldPos;
        }
    }


    private GameObject GetPrefab(RoomKind target)
    {
        for (int i = 0; i < roomPrefabs.Count; i++)
        {
            if (roomPrefabs[i].kind == target)
                return roomPrefabs[i].prefab;
        }
        return null;
    }


    bool CubeCornerTouch(BoxCollider col, LayerMask mask)
    {
        Bounds b = col.bounds;

        Vector3[] corners = {
        new(b.min.x, b.min.y, b.min.z),
        new(b.min.x, b.min.y, b.max.z),
        new(b.min.x, b.max.y, b.min.z),
        new(b.min.x, b.max.y, b.max.z),
        new(b.max.x, b.min.y, b.min.z),
        new(b.max.x, b.min.y, b.max.z),
        new(b.max.x, b.max.y, b.min.z),
        new(b.max.x, b.max.y, b.max.z)
    };

        foreach (var c in corners)
            if (Physics.CheckSphere(c, 0.001f, mask))
                return true;

        return false;
    }

}
