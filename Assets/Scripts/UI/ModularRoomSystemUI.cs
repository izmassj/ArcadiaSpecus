using UnityEngine;
using UnityEngine.UI;

public class ModularRoomSystemUI : MonoBehaviour
{
    [Header("Room Buttons")]
    [SerializeField] private Button _doorWallBtn;
    [SerializeField] private Button _rightRoomBtn;
    [SerializeField] private Button _midRoomBtn;
    [SerializeField] private Button _leftRoomBtn;
    [SerializeField] private Button _intersectionBtn;

    [Header("Mode Control Buttons")]
    [SerializeField] private Button _enterModularRoomUIBtn;
    [SerializeField] private Button _exitModularRoomUIBtn;

    [Header("UI Containers")]
    [SerializeField] private GameObject _modularRoomButtonsContainer;
    [SerializeField] private GameObject _machinesEnterButtonContainer;
    [SerializeField] private GameObject _modularRoomEnterButtonContainer;

    private void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        SetupRoomButtons();
        SetupModeButtons();
    }

    private void SetupRoomButtons()
    {
        _doorWallBtn.onClick.AddListener(() => SpawnRoom(RoomKind.DOORWALL));
        _rightRoomBtn.onClick.AddListener(() => SpawnRoom(RoomKind.RIGHT));
        _midRoomBtn.onClick.AddListener(() => SpawnRoom(RoomKind.MIDDLE));
        _leftRoomBtn.onClick.AddListener(() => SpawnRoom(RoomKind.LEFT));
        _intersectionBtn.onClick.AddListener(() => SpawnRoom(RoomKind.INTERSECTION));
    }

    private void SetupModeButtons()
    {
        _enterModularRoomUIBtn.onClick.AddListener(EnterRoomMode);
        _exitModularRoomUIBtn.onClick.AddListener(ExitRoomMode);
    }

    private void SpawnRoom(RoomKind roomKind)
    {
        if (ModularRoomSystem.Instance != null)
            ModularRoomSystem.Instance.SpawnRoomPrefab(roomKind);
    }

    private void EnterRoomMode()
    {
        SetUIVisibility(
            roomsActive: true,
            machinesActive: false,
            showEnterButton: false
        );
    }

    private void ExitRoomMode()
    {
        SetUIVisibility(
            roomsActive: false,
            machinesActive: true,
            showEnterButton: true
        );

        DisableCurrentGhostRoom();
    }

    private void SetUIVisibility(bool roomsActive, bool machinesActive, bool showEnterButton)
    {
        _machinesEnterButtonContainer.SetActive(machinesActive);
        _modularRoomButtonsContainer.SetActive(roomsActive);
        _modularRoomEnterButtonContainer.SetActive(showEnterButton);
    }

    private void DisableCurrentGhostRoom()
    {
        if (ModularRoomSystem.Instance != null)
            ModularRoomSystem.Instance.SetCurrentRoom(null);
    }
}