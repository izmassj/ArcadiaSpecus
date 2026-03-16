using UnityEngine;

public class IdleState : PlayerBunkerState
{
    public IdleState(PlayerBunkerManager manager) : base(manager) {}

    public override void Enter()
    {
        
    }

    public override void Update()
    {

    }

    public override void HandleInput()
    {
        Ray ray = playerManager.mainCamera.ScreenPointToRay(playerManager.navigateInputAction.ReadValue<Vector2>());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.GetComponent<RoomManager>() != null)
            {

            }
        }
    }
}
