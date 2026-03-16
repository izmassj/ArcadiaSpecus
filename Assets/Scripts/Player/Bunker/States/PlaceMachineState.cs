using UnityEngine;

public class PlaceMachineState : PlayerBunkerState
{
    public PlaceMachineState(PlayerBunkerManager manager) : base(manager) { }

    public override void Enter()
    {

    }

    public override void Exit()
    {

    }

    public override void HandleInput()
    {
        Ray ray = playerManager.mainCamera.ScreenPointToRay(playerManager.navigateInputAction.ReadValue<Vector2>());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit)) 
        { 
            if (hit.collider.gameObject.layer == playerManager.interactaingRoomsLayer)
            {

            }
        }
    }

    public override void Update()
    {

    }
}
