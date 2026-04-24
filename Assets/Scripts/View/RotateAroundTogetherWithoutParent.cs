using UnityEngine;

public class RotateAroundTogetherWithoutParent : MonoBehaviour
{
    [SerializeField] private Transform mainObject;
    [SerializeField] private Transform[] objectsToRotate;

    private Quaternion lastRotation;

    private void Start()
    {
        lastRotation = mainObject.rotation;
    }

    private void LateUpdate()
    {
        Quaternion deltaRotation = mainObject.rotation * Quaternion.Inverse(lastRotation);

        foreach (Transform obj in objectsToRotate)
        {
            Vector3 direction = obj.position - mainObject.position;
            direction = deltaRotation * direction;

            obj.position = mainObject.position + direction;
            obj.rotation = deltaRotation * obj.rotation;
        }

        lastRotation = mainObject.rotation;
    }
}