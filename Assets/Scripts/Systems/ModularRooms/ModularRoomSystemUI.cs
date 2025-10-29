using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModularRoomSystemUI : MonoBehaviour
{
    enum ModularRoomsUI 
    { 
        NONSELECTED, SELECTED,
        PLACED
    }
    ModularRoomsUI currentState;

    [SerializeField] GameObject[] roomPrefabs;

    void Start()
    {
        
    }

    void Update()
    {
        switch (currentState)
        {
            case ModularRoomsUI.SELECTED:
                break;
            case ModularRoomsUI.NONSELECTED:
                break;
            case ModularRoomsUI.PLACED:
                break;
        }

    }

    public void SpawnRoomPrefab() 
    {
        
    }

    public void SelectedButton() 
    { 
        
    }
}
