using NUnit.Framework;
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
        if (intersection.GetComponent<RoomManager>().typeOfRoom != RoomKind.Intersection)
            return;

        if (_intersectionList.Count > 1)
        {
            GameObject lastIntersection = _intersectionList[_intersectionList.Count - 1];

            GameObject newIntersection = intersection;

            newIntersection.transform.position = new Vector3(lastIntersection.transform.position.x, lastIntersection.transform.position.y - _separationDistance, lastIntersection.transform.position.z);

            Instantiate(newIntersection, _intersectionsParent);

            _intersectionList.Add(newIntersection);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
