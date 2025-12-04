using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ModularRoomSystem : MonoBehaviour
{
    public static ModularRoomSystem Instance;

    [Header("Room Setup")]
    [SerializeField] private List<RoomPrefab> roomPrefabs;
    [SerializeField] private LayerMask roomLayerMask;
    [SerializeField] private float cornerCheckSize;
    [SerializeField] private float snapDistance;

    [Header("Room Parameters")]
    public float roomPlacementDistance;

    [Header("Debug Corner Markers")]
    [SerializeField] private bool showCornerDebug;
    [SerializeField] private float cornerMarkerSize;
    [SerializeField] private GameObject cornerMarkerPrefab;

    private Transform[] cornerMarkers = new Transform[8];

    private GameObject currentRoom;
    private Collider possibleRoom;
    private bool isBuilding = false;
    private RoomKind roomKind;
    private bool canPlaceRoom = false;

    private Dictionary<RoomKind, GameObject> prefabDict;

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

    private void Start()
    {
        // Convert your serialized list to a lookup table
        prefabDict = new Dictionary<RoomKind, GameObject>();
        foreach (var p in roomPrefabs)
        {
            if (!prefabDict.ContainsKey(p.kind))
                prefabDict.Add(p.kind, p.prefab);
        }
    }

    private void Update()
    {
        if (!isBuilding || currentRoom == null) return;

        FollowMouse();
        TryDetectOtherRooms();

        if (showCornerDebug)
            UpdateCornerMarkerPositions();

        if (Mouse.current.leftButton.wasPressedThisFrame && canPlaceRoom)
        {
            PlaceRoom();
        }
    }

    public void SpawnRoomPrefab(RoomKind room)
    {
        GameObject prefab = GetPrefab(room);
        if (prefab == null) return;

        if (currentRoom != null)
            Destroy(currentRoom);

        Vector3 spawnPos = GetMouseWorldPosition();
        currentRoom = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (showCornerDebug)
            CreateCornerMarkers(currentRoom);

        BoxCollider collider = currentRoom.transform.GetChild(0).GetChild(0).gameObject.GetComponent<BoxCollider>();
        if (collider != null)
            collider.isTrigger = true;

        roomKind = room;
        isBuilding = true;
        canPlaceRoom = false;

        SetRoomColor(Color.red);
    }

    private void CreateCornerMarkers(GameObject room)
    {
        if (cornerMarkerPrefab == null)
        {
            cornerMarkerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cornerMarkerPrefab.GetComponent<Collider>()); 
        }

        for (int i = 0; i < 8; i++)
        {
            GameObject marker = Instantiate(cornerMarkerPrefab, room.transform);
            marker.name = "CornerMarker_" + i;
            marker.transform.localScale = Vector3.one * cornerMarkerSize;

            cornerMarkers[i] = marker.transform;
        }
    }

    private void UpdateCornerMarkerPositions()
    {
        if (currentRoom == null || cornerMarkers == null) return;

        BoxCollider col = currentRoom.transform.GetChild(0).GetChild(0).gameObject.GetComponent<BoxCollider>();
        Bounds b = col.bounds;

        Vector3[] corners = GetCorners(b);

        for (int i = 0; i < 8; i++)
        {
            if (cornerMarkers[i] != null)
                cornerMarkers[i].position = corners[i];
        }
    }

    public void PlaceRoom()
    {
        if (!isBuilding || currentRoom == null || !canPlaceRoom) return;

        SetRoomColor(Color.white);

        BoxCollider collider = currentRoom.transform.GetChild(0).GetChild(0).gameObject.GetComponent<BoxCollider>();
        if (collider != null)
            collider.isTrigger = false;

        if (possibleRoom != null)
        {
            BoxCollider targetCollider = possibleRoom.GetComponent<BoxCollider>();
            BoxCollider selfCollider = currentRoom.transform.GetChild(0).GetChild(0).GetComponent<BoxCollider>();

            Vector3[] targetCorners = GetCorners(targetCollider.bounds);
            Vector3[] selfCorners = GetCorners(selfCollider.bounds);

            float minDistance = float.MaxValue;
            Vector3 bestSelfCorner = Vector3.zero;
            Vector3 bestTargetCorner = Vector3.zero;

            foreach (var selfCorner in selfCorners)
            {
                foreach (var targetCorner in targetCorners)
                {
                    float dist = Vector3.Distance(selfCorner, targetCorner);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestSelfCorner = selfCorner;
                        bestTargetCorner = targetCorner;
                    }
                }
            }

            selfCollider.gameObject.SetActive(false);

            Vector3 offset = bestSelfCorner - currentRoom.transform.position;
            currentRoom.transform.position = bestTargetCorner - offset;
        }


        currentRoom = null;
        isBuilding = false;
        canPlaceRoom = false;
    }



    void FollowMouse()
    {
        Vector3 pos = GetMouseWorldPosition();
        pos.z = roomPlacementDistance;
        currentRoom.transform.position = pos;
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 500f));
        world.z = 0f;
        return world;
    }


    void TryDetectOtherRooms()
    {
        BoxCollider self = currentRoom.transform.GetChild(0).GetChild(0).GetComponent<BoxCollider>();
        Bounds b = self.bounds;

        Vector3[] corners = GetCorners(b);
        bool foundValidPlacement = false;

        foreach (var corner in corners)
        {
            Collider[] hits = Physics.OverlapBox(
                corner,
                Vector3.one * cornerCheckSize,
                Quaternion.identity,
                roomLayerMask
            );

            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(currentRoom.transform))
                    continue;

                if (CanPlaceNextToRoom(hit.bounds, b))
                {
                    possibleRoom = hit;
                    foundValidPlacement = true;
                    break;
                }
                else
                {
                    possibleRoom = null; 
                }
            }

            if (foundValidPlacement)
                break;
        }

        canPlaceRoom = foundValidPlacement;
        SetRoomColor(foundValidPlacement ? Color.green : Color.red);
    }

    bool CanPlaceNextToRoom(Bounds target, Bounds self)
    {
        float verticalDistance = Mathf.Abs(self.center.y - target.center.y);
        if (verticalDistance > snapDistance) return false;

        float horizontalDistance = Mathf.Abs(self.center.x - target.center.x);
        float totalWidth = self.extents.x + target.extents.x;

        return horizontalDistance - totalWidth <= snapDistance;
    }


    Vector3[] GetCorners(Bounds b)
    {
        return new Vector3[]
        {
            new Vector3(b.min.x, b.min.y, b.min.z),
            new Vector3(b.min.x, b.min.y, b.max.z),
            new Vector3(b.min.x, b.max.y, b.min.z),
            new Vector3(b.min.x, b.max.y, b.max.z),
            new Vector3(b.max.x, b.min.y, b.min.z),
            new Vector3(b.max.x, b.min.y, b.max.z),
            new Vector3(b.max.x, b.max.y, b.min.z),
            new Vector3(b.max.x, b.max.y, b.max.z)
        };
    }

    void SetRoomColor(Color c)
    {
        foreach (Renderer r in currentRoom.GetComponentsInChildren<Renderer>())
            r.material.color = c;
    }

    public void SetCurrentRoom(GameObject newRoom)
    {
        if (newRoom == null)
        {
            Destroy(currentRoom);
            currentRoom = null;

            currentRoom = null;
            isBuilding = false;
            canPlaceRoom = false;
        }
        else
        {
            currentRoom = newRoom;
        }
    }

    GameObject GetPrefab(RoomKind kind)
    {
        return prefabDict.ContainsKey(kind) ? prefabDict[kind] : null;
    }
}