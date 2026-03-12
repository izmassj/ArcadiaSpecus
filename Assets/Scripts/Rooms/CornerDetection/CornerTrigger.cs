using UnityEngine;


public class CornerTrigger : MonoBehaviour
{
    private ICornerDetectorOwner _owner;
    private string _cornerName;
    private int _touchCount;

    public bool IsTouching => _touchCount > 0;
    public string CornerName => _cornerName;

    public void Setup(ICornerDetectorOwner owner, string cornerName)
    {
        _owner = owner;
        _cornerName = cornerName;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger Enter {gameObject.name}");

        CornerTrigger otherCorner = other.GetComponent<CornerTrigger>();

        if (otherCorner == null)
            return;

        _touchCount++;

        Debug.Log($"{transform.root.name} -> esquina {_cornerName} tocando con {otherCorner.transform.root.name}:{otherCorner.CornerName}");

        _owner?.PrintDetectedCorners();
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Trigger Exit {gameObject.name}");

        CornerTrigger otherCorner = other.GetComponent<CornerTrigger>();

        if (otherCorner == null)
            return;

        _touchCount = Mathf.Max(0, _touchCount - 1);

        Debug.Log($"{transform.root.name} -> esquina {_cornerName} dejó de tocar con {otherCorner.transform.root.name}:{otherCorner.CornerName}");

        _owner?.PrintDetectedCorners();
    }
}