using System.Collections.Generic;
using UnityEngine;

public class RoomRailPoints : MonoBehaviour
{
    [SerializeField] private List<Transform> railPoints = new();

    public IReadOnlyList<Transform> RailPoints => railPoints;
    public int Count => railPoints != null ? railPoints.Count : 0;
}