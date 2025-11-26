using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModularRoomSystemUI2 : MonoBehaviour
{
    [SerializeField] GameObject[] roomPrefabs;

    [SerializeField] private Button doorWallBtn;
    [SerializeField] private Button rightRoomBtn;
    [SerializeField] private Button midRoomBtn;
    [SerializeField] private Button leftRoomBtn;
    [SerializeField] private Button intersectionBtn;

    private ModularRoomSystem roomSystem;

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

 

    public void SpawnRoomPrefab() 
    {
        
    }

    public void SelectedButton() 
    { 
        
    }
}
