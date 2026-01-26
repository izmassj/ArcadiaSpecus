using UnityEngine;

[System.Serializable]
public class PlatformMovementSettings
{
    public float speed = 3f;
    public float distance = 5f;
    public Vector3 movementDirection = Vector3.forward;
    public bool pingPong = true;
    public bool startOnContact = false;
    public bool oneWayWithReset = false;
}