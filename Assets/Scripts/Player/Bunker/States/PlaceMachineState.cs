using UnityEngine;

public class PlaceMachineState : PlayerBunkerState
{
    public PlaceMachineState(PlayerBunkerManager manager) : base(manager) { }

    private RoomManager _currentRoom;

    public override void Enter()
    {
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
        Ray ray = playerManager.mainCamera.ScreenPointToRay(
            playerManager.navigateInputAction.ReadValue<Vector2>()
        );

        RaycastHit hit;

        RoomManager newRoom = null;

        if (Physics.Raycast(ray, out hit))
        {
            newRoom = hit.collider.GetComponent<RoomManager>();
        }

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

    public override void Update() 
    { 
    
    }
}