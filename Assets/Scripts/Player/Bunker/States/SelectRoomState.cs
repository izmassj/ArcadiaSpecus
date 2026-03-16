using UnityEngine;

public class SelectRoomState : PlayerBunkerState
{
    public SelectRoomState(PlayerBunkerManager manager) : base(manager) { }

    public override void Enter()
    {
        //Debug.Log("Selecting room");
    }

    public override void HandleInput()
    {
        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    playerManager.ChangeState(playerManager.idleState);
        //}
    }
}
