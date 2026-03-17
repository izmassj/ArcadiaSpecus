using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(BoxCollider))]
public class CornerDetector : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private string _cornersRootName;

    [Header("Trigger Size")]
    [SerializeField] private float _cornerTriggerWorldSize;

    [Header("Corners")]
    [SerializeField] public CornerTypeCornerTypeDictionary _cornersRule;

    [Header("Debug")]
    [SerializeField] public CornerTypeBoolDictionary _cornersDetected;

    private BoxCollider _mainBox;
    private CornerTrigger[] _corners;

    private GameObject _foreignRoom;

    private static readonly string[] EightCornerNames =
    {
        "TopFrontLeft",
        "TopFrontRight",
        "TopBackLeft",
        "TopBackRight",
        "BottomFrontLeft",
        "BottomFrontRight",
        "BottomBackLeft",
        "BottomBackRight"
    };

    private void Awake()
    {
        BuildCorners();
    }

    private void Start()
    {
        InitializeDetectedDictionary();
    }

    private void Update()
    {
        UpdateDetectedCorners();
    }

    public void BuildCorners()
    {
        _mainBox = GetComponent<BoxCollider>();
        if (_mainBox == null)
            return;

        Transform cornersParent = GetOrCreateCornersParent();

        string[] names = EightCornerNames;
        Vector3[] positions = GetCornerPositions();

        _corners = new CornerTrigger[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            CornerTrigger corner = GetOrCreateCorner(cornersParent, names[i]);

            Transform t = corner.transform;
            t.SetParent(cornersParent, false);
            t.localPosition = positions[i];
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            BoxCollider trigger = corner.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.size = Vector3.one * _cornerTriggerWorldSize;

            corner.Setup(names[i]);
            _corners[i] = corner;
        }
    }

    private void InitializeDetectedDictionary()
    {
        _cornersDetected = new CornerTypeBoolDictionary();

        if (_corners == null)
            return;

        for (int i = 0; i < _corners.Length; i++)
        {
            if (_corners[i] != null)
                _cornersDetected[_corners[i].type] = false;
        }
    }

    public void UpdateDetectedCorners()
    {
        if (_corners == null || _cornersDetected == null)
        {
            _foreignRoom = null;
            return;
        }
        else
        {
            _foreignRoom = _corners[0].Parent;
        }

            for (int i = 0; i < _corners.Length; i++)
            {
                if (_corners[i] == null)
                    continue;

                _cornersDetected[_corners[i].type] = _corners[i].IsTouching;
            }
    }

    public void SnapToDetectedCorner(float distance)
    {
        if (!TryGetSnapCorners(out CornerTrigger myCorner, out CornerTrigger otherCorner))
            return;

        Vector3 delta = otherCorner.transform.position - myCorner.transform.position;
        transform.position += delta;
        transform.position = new Vector3(transform.position.x, transform.position.y, distance);
    }

    public bool TryGetSnapCorners(out CornerTrigger myCorner, out CornerTrigger otherCorner)
    {
        myCorner = null;
        otherCorner = null;

        if (_corners == null)
            return false;

        for (int i = 0; i < _corners.Length; i++)
        {
            CornerTrigger corner = _corners[i];

            if (corner == null)
                continue;

            if (!corner.IsTouching)
                continue;

            if (corner.DetectedCorner == null)
                continue;

            myCorner = corner;
            otherCorner = corner.DetectedCorner;
            return true;
        }

        return false;
    }

    public CornerInteractionType EvaluateDetectedCorners()
    {
        bool isTouchingAnything = false;

        for (int i = 0; i < _corners.Length; i++)
        {
            CornerTrigger corner = _corners[i];

            if (corner == null)
                continue;

            _cornersDetected[corner.type] = corner.IsTouching;

            if (!corner.IsTouching)
                continue;

            isTouchingAnything = true;

            if (!_cornersRule.ContainsKey(corner.type))
                return CornerInteractionType.NonBuildable;

            if (!corner.DetectedType.HasValue)
                return CornerInteractionType.NonBuildable;

            if (_cornersRule[corner.type] != corner.DetectedType.Value)
                return CornerInteractionType.NonBuildable;

            RoomManager ownRoomManager = GetComponent<RoomManager>();
            RoomManager collidingRoomManager = corner.DetectedCorner.transform.root.GetComponent<RoomManager>();

            if (ownRoomManager != null && collidingRoomManager != null)
            {
                if (ownRoomManager.typeOfRoom == RoomKind.Right && collidingRoomManager.typeOfRoom == RoomKind.Right)
                    return CornerInteractionType.NonBuildable;

                if (ownRoomManager.typeOfRoom == RoomKind.Left && collidingRoomManager.typeOfRoom == RoomKind.Left)
                    return CornerInteractionType.NonBuildable;
            }
        }

        if (!isTouchingAnything)
            return CornerInteractionType.Static;

        return CornerInteractionType.Buildable;
    }

    public void PrintDetectedCorners()
    {
        if (_corners == null)
            return;

        List<string> detected = new List<string>();

        for (int i = 0; i < _corners.Length; i++)
        {
            if (_corners[i] != null && _corners[i].IsTouching)
                detected.Add(_corners[i].CornerName);
        }

        if (detected.Count == 0)
        {
            Debug.Log($"{name} -> 0 esquinas detectadas.");
            return;
        }

        Debug.Log($"{name} -> {detected.Count} esquinas detectadas: {string.Join(", ", detected)}");
    }

    private Vector3[] GetCornerPositions()
    {
        Vector3 c = _mainBox.center;
        Vector3 h = _mainBox.size * 0.5f;

        return new Vector3[]
        {
            new Vector3(c.x - h.x, c.y + h.y, c.z + h.z),
            new Vector3(c.x + h.x, c.y + h.y, c.z + h.z),
            new Vector3(c.x - h.x, c.y + h.y, c.z - h.z),
            new Vector3(c.x + h.x, c.y + h.y, c.z - h.z),

            new Vector3(c.x - h.x, c.y - h.y, c.z + h.z),
            new Vector3(c.x + h.x, c.y - h.y, c.z + h.z),
            new Vector3(c.x - h.x, c.y - h.y, c.z - h.z),
            new Vector3(c.x + h.x, c.y - h.y, c.z - h.z)
        };
    }

    private Transform GetOrCreateCornersParent()
    {
        Transform cornersParent = transform.Find(_cornersRootName);

        if (cornersParent == null)
        {
            GameObject go = new GameObject(_cornersRootName);
            cornersParent = go.transform;
            cornersParent.SetParent(transform, false);
        }

        cornersParent.localPosition = Vector3.zero;
        cornersParent.localRotation = Quaternion.identity;
        cornersParent.localScale = Vector3.one;

        return cornersParent;
    }

    private CornerTrigger GetOrCreateCorner(Transform parent, string cornerName)
    {
        Transform child = parent.Find(cornerName);

        if (child == null)
        {
            GameObject go = new GameObject(cornerName);
            child = go.transform;
            child.SetParent(parent, false);
        }

        CornerTrigger trigger = child.GetComponent<CornerTrigger>();
        if (trigger == null)
            trigger = child.gameObject.AddComponent<CornerTrigger>();

        BoxCollider box = child.GetComponent<BoxCollider>();
        if (box == null)
            box = child.gameObject.AddComponent<BoxCollider>();

        return trigger;
    }

    public GameObject GetForeignRoom()
    {
        if (_foreignRoom != null)
        {
            return _foreignRoom; 
        }
        return null;
    }
}