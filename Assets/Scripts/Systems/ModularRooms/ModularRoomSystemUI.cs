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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
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
