using UnityEngine;
using UnityEngine.UI;

public class MachinePlacementSystemUI : MonoBehaviour
{
    [Header("Machine Buttons UI")]
    [SerializeField] private Button waterMachineBtn;
    [SerializeField] private Button foodMachineBtn;
    [SerializeField] private Button energyMachineBtn;
    [SerializeField] private Button scrapMachineBtn;
    [SerializeField] private Button bedBtn;
    [SerializeField] private Button waterDispenserBtn;
    [SerializeField] private Button foodDispenserBtn;

    [Header("Enter/Exit Machine Buttons UI")]
    [SerializeField] private Button enterMachineUIBtn;
    [SerializeField] private Button exitMachineUIBtn;

    [Header("Machine UI GameObjects")]
    [SerializeField] private GameObject machinesButtonsGameObj;
    [SerializeField] private GameObject machinesEnterButtonGameObj;

    public void SetUpUI()
    {
        waterMachineBtn.onClick.AddListener(() => MachinePlacementSystem.Instance.SpawnMachinePrefab(MachineKind.WATER_MACHINE));
        foodMachineBtn.onClick.AddListener(() => MachinePlacementSystem.Instance.SpawnMachinePrefab(MachineKind.FOOD_MACHINE));
        energyMachineBtn.onClick.AddListener(() => MachinePlacementSystem.Instance.SpawnMachinePrefab(MachineKind.ENERGY_MACHINE));
        scrapMachineBtn.onClick.AddListener(() => MachinePlacementSystem.Instance.SpawnMachinePrefab(MachineKind.SCRAP_MACHINE));
        bedBtn.onClick.AddListener(() => MachinePlacementSystem.Instance.SpawnMachinePrefab(MachineKind.BED));
        waterDispenserBtn.onClick.AddListener(() => MachinePlacementSystem.Instance.SpawnMachinePrefab(MachineKind.WATER_DISPENSER));
        foodDispenserBtn.onClick.AddListener(() => MachinePlacementSystem.Instance.SpawnMachinePrefab(MachineKind.FOOD_DISPENSER));

        enterMachineUIBtn.onClick.AddListener(() => ActivateDeactivateUIObjects(machinesEnterButtonGameObj, machinesButtonsGameObj));
        exitMachineUIBtn.onClick.AddListener(() => ActivateDeactivateUIObjects(machinesButtonsGameObj, machinesEnterButtonGameObj));
        exitMachineUIBtn.onClick.AddListener(DisableCurrentGhostMachine);
    }

    void Awake()
    {
        SetUpUI();
    }

    private void DisableCurrentGhostMachine()
    {
        MachinePlacementSystem.Instance.SetCurrentMachine(null);
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