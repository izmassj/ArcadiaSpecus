using UnityEngine;
using UnityEngine.UI;

public class PlayerBunkerUIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerBunkerManager _playerManager;

    [Header("Construction - Pre-Action Buttons")]
    [SerializeField] private Button _buildRoomsButton;
    [SerializeField] private Button _placeMachinesButton;

    [Header("Construction - Build Rooms Panel")]
    [SerializeField] private RectTransform _buildRoomsPanel;

    [Header("Construction - Build Rooms")]
    [SerializeField] private Button _middleRoomButton;
    [SerializeField] private Button _doorWallButton;
    [SerializeField] private Button _rightRoomButton;
    [SerializeField] private Button _leftRoomButton;
    [SerializeField] private Button _intersectionButton;

    [Header("Construction - Place Panel")]
    [SerializeField] private RectTransform _placeMachinesPanel;

    [Header("Construction - Place Produce Machines")]
    [SerializeField] private Button _electricityMachineButton;
    [SerializeField] private Button _waterMachineButton;
    [SerializeField] private Button _foodMachineButton;
    [SerializeField] private Button _scrapMachineButton;
    
    [Header("Construction - Place Station Machines")]
    [SerializeField] private Button _bedroomStationButton;
    [SerializeField] private Button _waterStationButton;
    [SerializeField] private Button _foodStationButton;

    //////////////////////
    //      ROOMS       //
    //////////////////////

    public void OnBuildRoomsButtonPressed()
    {
        if (_buildRoomsPanel.gameObject.activeInHierarchy) 
        { 
            _buildRoomsPanel.gameObject.SetActive(false);
            _playerManager.ChangeState(_playerManager.idleState);
        }
        else
        {
            _placeMachinesPanel.gameObject.SetActive(false);
            _buildRoomsPanel.gameObject.SetActive(true);

            _playerManager.EnterBuildRoomMode();
        }
    }

    public void OnBuildSpecificRoomButtonPressed(int kind)
    {
        if (_playerManager.GetCurrentState() is BuildRoomState buildRoomState)
        {
            buildRoomState.InstantiateRoom((RoomKind)kind);
            _buildRoomsPanel.gameObject.SetActive(false);
        }
    }

    //////////////////////
    //     MACHINES     //
    //////////////////////
    
    public void OnPlaceMachinesButtonPressed()
    {
        if (_placeMachinesPanel.gameObject.activeInHierarchy)
        {
            _placeMachinesPanel.gameObject.SetActive(false);
            _playerManager.ChangeState(_playerManager.idleState);
        }
        else
        {
            _placeMachinesPanel.gameObject.SetActive(true);
            _buildRoomsPanel.gameObject.SetActive(false);

            _playerManager.EnterPlaceMachineMode();
        }
    }

}