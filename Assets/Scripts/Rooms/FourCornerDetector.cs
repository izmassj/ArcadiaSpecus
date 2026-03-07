using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FourCornerDetector : MonoBehaviour, ICornerDetectorOwner
{
    [Header("Setup")]
    [SerializeField] private string _cornersRootName;

    [Header("Trigger Size")]
    [SerializeField] private float _cornerTriggerWorldSize;

    private BoxCollider _mainBox;
    private CornerTrigger[] _corners = new CornerTrigger[4];

    private static readonly string[] CornerNames =
    {
        "TopLeft",
        "TopRight",
        "BottomLeft",
        "BottomRight"
    };

    private void Awake()
    {
        BuildCorners();
    }


    [ContextMenu("Build Corners")]
    public void BuildCorners()
    {
        _mainBox = GetComponent<BoxCollider>();
        if (_mainBox == null)
            return;

        Transform cornersParent = GetOrCreateCornersParent();

        Vector3 center = _mainBox.center;
        Vector3 half = _mainBox.size * 0.5f;

        Vector3[] positions = GetCornerPositions(center, half);

        for (int i = 0; i < 4; i++)
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

            corner.Setup(this, CornerNames[i]);
            _corners[i] = corner;
        }
    }

    private Vector3[] GetCornerPositions(Vector3 center, Vector3 half)
    {
        return new Vector3[]
        {
            new Vector3(center.x, center.y + half.y, center.z - half.z),
            new Vector3(center.x, center.y + half.y, center.z + half.z),
            new Vector3(center.x, center.y - half.y, center.z - half.z),
            new Vector3(center.x, center.y - half.y, center.z + half.z)
        };
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