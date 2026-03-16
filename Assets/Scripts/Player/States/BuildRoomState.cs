using System.Linq;
using UnityEngine;

public class BuildRoomState : PlayerBunkerState
{
    public BuildRoomState(PlayerBunkerManager manager) : base(manager) { }

    private GameObject _currentGameObject;

    private Vector3 _currentRoomPosition = Vector3.zero;

    public override void Enter()
    {
    }

    public override void Exit()
    {
        _currentGameObject = null;
    }

    public override void HandleInput()
    {
        if (_currentGameObject != null) 
        { 
            if (playerManager.confirmInputAction.triggered) 
            {
                _currentGameObject.GetComponent<RoomManager>().SetPlaced();
                _currentGameObject.GetComponent<RoomManager>().SetOriginalMaterial();
                _currentGameObject.transform.position = _currentGameObject.GetComponent<CornerDetector>().GetForeignRoom().transform.position;
                playerManager.ChangeState(playerManager.idleState);
            }
        }
    }

    public void InstantiateRoom(RoomKind kind)
    {
        GameObject prefabRoom = playerManager.prefabsRoom.First(foo => foo.kind == kind).prefab;

        _currentGameObject = Object.Instantiate(prefabRoom, _currentRoomPosition, Quaternion.identity);
        _currentGameObject.GetComponent<RoomManager>().SetOnRoomBuildMaterial();
    }

    public override void Update()
    {
        if (_currentGameObject != null && !_currentGameObject.GetComponent<RoomManager>().GetPlaced())
        {
            Vector2 input = playerManager.navigateInputAction.ReadValue<Vector2>();
            _currentRoomPosition = new Vector3(input.x, input.y, playerManager.roomPlacementDistance);

            _currentGameObject.transform.position = playerManager.mainCamera.ScreenToWorldPoint(_currentRoomPosition);
        }
    }
}