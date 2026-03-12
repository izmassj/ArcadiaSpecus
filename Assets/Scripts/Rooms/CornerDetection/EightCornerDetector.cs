using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EightCornerDetector : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private string CornersRootName;

    [Header("Trigger Size")]
    [SerializeField] private float _cornerTriggerWorldSize;

    [Header("Corners")]
    [SerializeField] public CornerTypeBoolDictionary _cornersRule;
    [SerializeField] public CornerTypeBoolDictionary _cornersDetected;


    private BoxCollider _mainBox;
    private CornerTrigger[] _corners = new CornerTrigger[8];

    private static readonly string[] CornerNames =
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
        _cornersDetected = new CornerTypeBoolDictionary();

        for (int i = 0; i < 8; i++)
            _cornersDetected[(CornerType)i] = false;
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

        Vector3 c = _mainBox.center;
        Vector3 h = _mainBox.size * 0.5f;

        Vector3[] positions =
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

        for (int i = 0; i < 8; i++)
        {
            CornerTrigger corner = GetOrCreateCorner(cornersParent, CornerNames[i]);

            Transform t = corner.transform;
            t.SetParent(cornersParent, false);
            t.localPosition = positions[i];
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            BoxCollider trigger = corner.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.size = Vector3.one * _cornerTriggerWorldSize;

            corner.Setup(CornerNames[i]);
            _corners[i] = corner;
        }
    }

    private Transform GetOrCreateCornersParent()
    {
        Transform cornersParent = transform.Find(CornersRootName);

        if (cornersParent == null)
        {
            GameObject go = new GameObject(CornersRootName);
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

    public void UpdateDetectedCorners()
    {
        for (int i = 0; i < _corners.Length; i++)
        {
            if (_corners[i] != null && _corners[i].IsTouching)
            {
                _cornersDetected[_corners[i].type] = true;
            }
            else
            {
                _cornersDetected[_corners[i].type] = false;
            }
        }
    }


    public void PrintDetectedCorners()
    {
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
}