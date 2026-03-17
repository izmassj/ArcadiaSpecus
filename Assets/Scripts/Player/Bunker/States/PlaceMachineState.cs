using FMOD.Studio;
using UnityEngine;

public class PlaceMachineState : PlayerBunkerState
{
    public PlaceMachineState(PlayerBunkerManager manager) : base(manager) { }

    private RoomManager _currentRoom;
    private bool _isFocusing;

    public override void Enter()
    {
        _isFocusing = false;
        _currentRoom = null;
    }

    public override void Exit()
    {
        if (_currentRoom != null)
        {
            _currentRoom.DeactivateOutline(playerManager.defaultLayer);
            _currentRoom = null;
        }
    }

    public override void HandleInput()
    {
        if (playerManager.confirmInputAction.triggered && _currentRoom != null)
        {
            if (!_currentRoom.GetComponent<RoomManager>().IsRoomFocused())
            {
                _isFocusing = true;
                _currentRoom.GetComponent<RoomManager>().FocusRoom();
                _currentRoom.DeactivateOutline(playerManager.defaultLayer);
            }
        }

        if (playerManager.unconfirmInputAction.triggered && _isFocusing)
        {
            _isFocusing = false;
            _currentRoom.GetComponent<RoomManager>().UnFocusRoom();
        }
    }

    public override void Update() 
    {
        Ray ray = playerManager.mainCamera.ScreenPointToRay(playerManager.navigateInputAction.ReadValue<Vector2>());

        RaycastHit hit;

        RoomManager newRoom = null;

        if (Physics.Raycast(ray, out hit))
        {
            newRoom = hit.collider.GetComponent<RoomManager>();
        }

        if (!_isFocusing)
        {
            if (newRoom != _currentRoom)
            {
                if (_currentRoom != null)
                {
                    _currentRoom.DeactivateOutline(playerManager.defaultLayer);
                }

                if (newRoom != null)
                {
                    newRoom.ActivateOutline(playerManager.outlineLayer);
                }

                _currentRoom = newRoom;
            }
        }
    }
}