using UnityEngine;

public class KeepWorldRotation : MonoBehaviour
{
    [SerializeField] private bool keepInitialRotation = true;
    [SerializeField] private Vector3 fixedWorldEulerRotation;

    private Quaternion fixedWorldRotation;

    private void Start()
    {
        if (keepInitialRotation)
        {
            fixedWorldRotation = transform.rotation;
            fixedWorldEulerRotation = transform.rotation.eulerAngles;
        }
        else
        {
            fixedWorldRotation = Quaternion.Euler(fixedWorldEulerRotation);
        }
    }

    private void LateUpdate()
    {
        transform.rotation = fixedWorldRotation;
    }
}