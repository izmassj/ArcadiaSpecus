using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModularRoomSystemUI : MonoBehaviour
{
    [Header("Modular Room Buttons UI")]
    [SerializeField] private Button doorWallBtn;
    [SerializeField] private Button rightRoomBtn;
    [SerializeField] private Button midRoomBtn;
    [SerializeField] private Button leftRoomBtn;
    [SerializeField] private Button intersectionBtn;

    [Header("Modular Room Buttons UI")]
    [SerializeField] private Button waterMachineBtn;
    [SerializeField] private Button foodMachineBtn;
    [SerializeField] private Button energyMachineBtn;
    [SerializeField] private Button scrapMachineBtn;
    [SerializeField] private Button bedBtn;
    [SerializeField] private Button waterDispenserBtn;
    [SerializeField] private Button foodDispenserBtn;

    [Header("Enter/Exit Modular Room Buttons UI")]
    [SerializeField] private Button enterModularRoomUIBtn;
    [SerializeField] private Button exitModularRoomUIBtn;

    [Header("Enter/Exit Machine Buttons UI")]
    [SerializeField] private Button enterMachineRoomUIBtn;
    [SerializeField] private Button exitMachineRoomUIBtn;

    [Header("Modular Room UI GameObjects")]
    [SerializeField] private GameObject modularRoomButtonsGameObj;
    [SerializeField] private GameObject modularRoomEnterButtonGameObj;

    [Header("Machine UI GameObjects")]
    [SerializeField] private GameObject machinesButtonsGameObj;
    [SerializeField] private GameObject machinesEnterButtonGameObj;

    private ModularRoomSystem roomSystem;

    public void SetUpUI()
    {
        doorWallBtn.onClick.AddListener(() => ModularRoomSystem.Instance.SpawnRoomPrefab(RoomKind.DOORWALL));
        rightRoomBtn.onClick.AddListener(() => ModularRoomSystem.Instance.SpawnRoomPrefab(RoomKind.RIGHT));
        midRoomBtn.onClick.AddListener(() => ModularRoomSystem.Instance.SpawnRoomPrefab(RoomKind.MIDDLE));
        leftRoomBtn.onClick.AddListener(() => ModularRoomSystem.Instance.SpawnRoomPrefab(RoomKind.LEFT));
        intersectionBtn.onClick.AddListener(() => ModularRoomSystem.Instance.SpawnRoomPrefab(RoomKind.INTERSECTION));

        enterModularRoomUIBtn.onClick.AddListener(() => ActivateDeactivateUIObjects(modularRoomEnterButtonGameObj, modularRoomButtonsGameObj));
        exitModularRoomUIBtn.onClick.AddListener(() => ActivateDeactivateUIObjects(modularRoomButtonsGameObj, modularRoomEnterButtonGameObj));
        exitModularRoomUIBtn.onClick.AddListener(DisableCurrentGhostRoom);
    }

    void Awake()
    {
        SetUpUI();
    }

    void Start()
    {
        
    }

    void Update()
    {

    }

    private void DisableCurrentGhostRoom()
    {
        ModularRoomSystem.Instance.SetCurrentRoom(null);
    }

    private void ActivateDeactivateUIObjects(GameObject obj1, GameObject obj2)
    {
        if (obj1.activeInHierarchy) 
        {
            obj1.SetActive(false);
            obj2.SetActive(true);
        } 
        else
        {
            obj1.SetActive(true);
            obj2.SetActive(false);
        }
    }
}
