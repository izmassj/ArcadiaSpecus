using System;
using System.Collections.Generic;
using UnityEngine;

public class CornerTrigger : MonoBehaviour
{
    private string _cornerName;
    private readonly List<CornerTrigger> _touchingCorners = new();

    public CornerType type;
    public GameObject Parent { get; private set; }
    public bool IsTouching => _touchingCorners.Count > 0;
    public string CornerName => _cornerName;

    public CornerType? DetectedType { get; private set; }
    public CornerTrigger DetectedCorner { get; private set; }

    public void Setup(string cornerName)
    {
        _cornerName = cornerName;
        type = Enum.Parse<CornerType>(cornerName);

        Parent = null;
        DetectedType = null;
        DetectedCorner = null;
        _touchingCorners.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        CornerTrigger otherCorner = other.GetComponent<CornerTrigger>();

        if (otherCorner == null)
            return;

        if (otherCorner.transform.root == transform.root)
            return;

        if (!_touchingCorners.Contains(otherCorner))
            _touchingCorners.Add(otherCorner);

        RefreshDetection();
    }

    private void OnTriggerExit(Collider other)
    {
        CornerTrigger otherCorner = other.GetComponent<CornerTrigger>();

        if (otherCorner == null)
            return;

        _touchingCorners.Remove(otherCorner);

        RefreshDetection();
    }

    private void RefreshDetection()
    {
        if (_touchingCorners.Count == 0)
        {
            Parent = null;
            DetectedType = null;
            DetectedCorner = null;
            return;
        }

        DetectedCorner = _touchingCorners[0];
        DetectedType = DetectedCorner.type;
        Parent = DetectedCorner.transform.root.gameObject;
    }
}