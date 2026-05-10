using UnityEngine;

public readonly struct ShockInfo
{
    public readonly GameObject SourceObject;
    public readonly Transform SourceTransform;
    public readonly Vector3 Center;
    public readonly float Radius;
    public readonly float Distance;
    public readonly Vector3 DirectionFromCenter;

    public ShockInfo(GameObject sourceObject, Transform sourceTransform, Vector3 center, float radius, float distance, Vector3 directionFromCenter)
    {
        SourceObject = sourceObject;
        SourceTransform = sourceTransform;
        Center = center;
        Radius = radius;
        Distance = distance;
        DirectionFromCenter = directionFromCenter;
    }
}
