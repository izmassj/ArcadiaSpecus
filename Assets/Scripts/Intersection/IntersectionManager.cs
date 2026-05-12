using UnityEngine;
using System.Collections.Generic;

public class IntersectionManager : MonoBehaviour
{
    [Header("Parent")]
    [SerializeField] private Transform _intersectionsParent;

    [Header("Intersection List")]
    [SerializeField] private List<GameObject> _intersectionList;

    [Header("Parameters")]
    [SerializeField] private float _separationDistance;

    public void AddIntersection(GameObject intersection)
    {
        if (intersection == null)
            return;

        RoomManager roomManager = intersection.GetComponent<RoomManager>();
        if (roomManager == null || roomManager.typeOfRoom != RoomKind.Intersection)
            return;

        if (_intersectionsParent == null)
            _intersectionsParent = transform;

        if (_intersectionList == null)
            _intersectionList = new List<GameObject>();

        Vector3 position = intersection.transform.position;

        if (_intersectionList.Count > 0 && _intersectionList[_intersectionList.Count - 1] != null)
        {
            GameObject lastIntersection = _intersectionList[_intersectionList.Count - 1];
            position = new Vector3(
                lastIntersection.transform.position.x,
                lastIntersection.transform.position.y - _separationDistance,
                lastIntersection.transform.position.z
            );
        }
        else
        {
            position = _intersectionsParent.position;
        }

        GameObject instance = Instantiate(intersection, position, intersection.transform.rotation, _intersectionsParent);
        _intersectionList.Add(instance);
    }
}
