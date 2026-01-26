using UnityEngine;
using UnityEngine.UI;

public class MachinePlacementSystemUI : MonoBehaviour
{
    [Header("Machine Buttons")]
    [SerializeField] private Button _waterMachineBtn;
    [SerializeField] private Button _foodMachineBtn;
    [SerializeField] private Button _energyMachineBtn;
    [SerializeField] private Button _scrapMachineBtn;
    [SerializeField] private Button _bedBtn;
    [SerializeField] private Button _waterDispenserBtn;
    [SerializeField] private Button _foodDispenserBtn;

    [Header("Mode Control Buttons")]
    [SerializeField] private Button _enterMachineUIBtn;
    [SerializeField] private Button _exitMachineUIBtn;

    [Header("UI Containers")]
    [SerializeField] private GameObject _machinesButtonsContainer;
    [SerializeField] private GameObject _machinesEnterButtonContainer;
    [SerializeField] private GameObject _modularRoomEnterButtonContainer;

    private void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        SetupMachineButtons();
        SetupModeButtons();
    }

    private void SetupMachineButtons()
    {
        _waterMachineBtn.onClick.AddListener(() => SpawnMachine(MachineKind.WATER_MACHINE));
        _foodMachineBtn.onClick.AddListener(() => SpawnMachine(MachineKind.FOOD_MACHINE));
        _energyMachineBtn.onClick.AddListener(() => SpawnMachine(MachineKind.ENERGY_MACHINE));
        _scrapMachineBtn.onClick.AddListener(() => SpawnMachine(MachineKind.SCRAP_MACHINE));
        _bedBtn.onClick.AddListener(() => SpawnMachine(MachineKind.BED));
        _waterDispenserBtn.onClick.AddListener(() => SpawnMachine(MachineKind.WATER_DISPENSER));
        _foodDispenserBtn.onClick.AddListener(() => SpawnMachine(MachineKind.FOOD_DISPENSER));
    }

    private void SetupModeButtons()
    {
        _enterMachineUIBtn.onClick.AddListener(EnterMachineMode);
        _exitMachineUIBtn.onClick.AddListener(ExitMachineMode);
    }

    private void SpawnMachine(MachineKind machineKind)
    {
        if (MachinePlacementSystem.Instance != null)
            MachinePlacementSystem.Instance.SpawnMachinePrefab(machineKind);
    }

    private void EnterMachineMode()
    {
        SetUIVisibility(
            machinesActive: true,
            modularRoomActive: false,
            showEnterButton: false
        );
    }

    private void ExitMachineMode()
    {
        SetUIVisibility(
            machinesActive: false,
            modularRoomActive: true,
            showEnterButton: true
        );

        DisableCurrentGhostMachine();
    }

    private void SetUIVisibility(bool machinesActive, bool modularRoomActive, bool showEnterButton)
    {
        _modularRoomEnterButtonContainer.SetActive(modularRoomActive);
        _machinesButtonsContainer.SetActive(machinesActive);
        _machinesEnterButtonContainer.SetActive(showEnterButton);
    }

    private void DisableCurrentGhostMachine()
    {
        if (MachinePlacementSystem.Instance != null)
            MachinePlacementSystem.Instance.SetCurrentMachine(null);
    }
}