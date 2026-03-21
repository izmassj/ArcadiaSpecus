using FMOD.Studio;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlaceMachineState : PlayerBunkerState
{
    public PlaceMachineState(PlayerBunkerManager manager) : base(manager) { }

    private RoomManager _currentRoom;
    private bool _isFocusing;
    private bool _isOverRoom;

    public override void Enter()
    {
        _isOverRoom = false;
        _isFocusing = false;
        _currentRoom = null;
    }

    public override void Exit()
    {
        if (CameraBunkerManager.Instance.IsCameraDisplaced())
        {
            CameraBunkerManager.Instance.MoveCameraToOriginalPos();
        }

        if (!playerManager.UI.IsMachineButtonsBlockPanelEnabled()) 
        {
            playerManager.UI.SetMachineButtonsTextChooseRoom();
            playerManager.UI.EnableMachineButtonsBlockPanel();
        }

        if (_currentRoom != null)
        {
            _currentRoom.GetComponent<RoomManager>().UnFocusRoom();
            _currentRoom.DeactivateOutline(playerManager.defaultLayer);
            _currentRoom = null;
        }
    }

    public override void HandleInput()
    {
        if (playerManager.confirmInputAction.triggered && _currentRoom != null && _isOverRoom)
        {
            if (!_currentRoom.GetComponent<RoomManager>().IsRoomFocused() && _currentRoom.GetComponent<RoomManager>().typeOfRoom != RoomKind.DoorWall && _currentRoom.GetComponent<RoomManager>().typeOfRoom != RoomKind.Intersection)
            {
                _isFocusing = true;
                _currentRoom.GetComponent<RoomManager>().FocusRoom();
                _currentRoom.DeactivateOutline(playerManager.defaultLayer);

                if (_currentRoom.GetComponent<RoomManager>().IsRoomOccupied())
                {
                    playerManager.UI.SetMachineButtonsTextOccupiedRoom();
                    playerManager.UI.EnableMachineButtonsBlockPanel();
                }
                else
                {
                    playerManager.UI.SetMachineButtonsTextChooseRoom();
                    playerManager.UI.DisableMachineButtonsBlockPanel();
                }
            }
        }

        if (playerManager.unconfirmInputAction.triggered && _isFocusing)
        {
            _isFocusing = false;
            _currentRoom.GetComponent<RoomManager>().UnFocusRoom();
            playerManager.UI.EnableMachineButtonsBlockPanel();
        }
    }

    private void RoomRaycasting()
    {
        Ray ray = playerManager.mainCamera.ScreenPointToRay(
           playerManager.navigateInputAction.ReadValue<Vector2>()
       );

        RaycastHit hit;
        RoomManager newRoom = null;

        if (Physics.Raycast(ray, out hit))
        {
            newRoom = hit.collider.GetComponent<RoomManager>();
        }

        _isOverRoom = newRoom != null;

        if (!_isFocusing)
        {
            if (newRoom == null)
            {
                if (_currentRoom != null)
                {
                    _currentRoom.DeactivateOutline(playerManager.defaultLayer);
                    _currentRoom = null;
                }

                return;
            }

            if (newRoom.typeOfRoom == RoomKind.DoorWall || newRoom.typeOfRoom == RoomKind.Intersection)
            {
                if (_currentRoom != null)
                {
                    _currentRoom.DeactivateOutline(playerManager.defaultLayer);
                    _currentRoom = null;
                }

                return;
            }

            if (newRoom != _currentRoom)
            {
                if (_currentRoom != null)
                {
                    _currentRoom.DeactivateOutline(playerManager.defaultLayer);
                }

                newRoom.ActivateOutline(playerManager.outlineLayer);
                _currentRoom = newRoom;
            }
        }
    }

    public override void Update()
    {
        RoomRaycasting();
    }

    public RoomManager GetCurrentRoom()
    {
        return _currentRoom;
    }
}