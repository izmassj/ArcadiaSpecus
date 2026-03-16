using System;
using UnityEngine;


public class CornerTrigger : MonoBehaviour
{
    private string _cornerName;
    private int _touchCount;

    public CornerType type;
    public bool IsTouching => _touchCount > 0;
    public string CornerName => _cornerName;

    public CornerType? DetectedType { get; private set; }

    public void Setup(string cornerName)
    {
        _cornerName = cornerName;
        type = Enum.Parse<CornerType>(cornerName);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger Enter {gameObject.name}");

        CornerTrigger otherCorner = other.GetComponent<CornerTrigger>();

        if (otherCorner == null)
            return;

        _touchCount++;
        DetectedType = otherCorner.type;

        Debug.Log($"{transform.root.name} -> esquina {_cornerName} tocando con {otherCorner.transform.root.name}:{otherCorner.CornerName}");

    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Trigger Exit {gameObject.name}");

        CornerTrigger otherCorner = other.GetComponent<CornerTrigger>();

        if (otherCorner == null)
            return;

        _touchCount = Mathf.Max(0, _touchCount - 1);


        Debug.Log($"{transform.root.name} -> esquina {_cornerName} dejó de tocar con {otherCorner.transform.root.name}:{otherCorner.CornerName}");
    }
}