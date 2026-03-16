using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour 
{
    [Header("Refs")]
    [SerializeField] private CornerDetector _cornerDetector;

    [Header("Materials")]
    [SerializeField] private Material _onRoomBuildMat;
    [SerializeField] private List<Material> _roomMats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
