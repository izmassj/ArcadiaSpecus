using UnityEngine;

public class MovingPlatformSurface : MonoBehaviour
{
    [Header("Carry")]
    [SerializeField] private bool _carryRotation = true;
    [SerializeField] private bool _yawOnly = true;

    public bool CarryRotation => _carryRotation;
    public bool YawOnly => _yawOnly;
}