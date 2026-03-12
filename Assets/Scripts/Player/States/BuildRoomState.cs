using System.Linq;
using UnityEngine;

public class BuildRoomState : PlayerBunkerState
{
    public BuildRoomState(PlayerBunkerManager manager) : base(manager) {}

    private GameObject _currentGameObject;
    private Vector3 _currentRoomPosition = Vector3.zero;

    public override void Enter()
    {
    }

    public override void Exit() 
    { 
        
    } 

    public override void HandleInput()
    {

    }

    public void InstantiateRoom(RoomKind kind)
    {
        switch (kind)
        {
            case RoomKind.Intersection:
                GameObject prefabRoom = playerManager.prefabsRoom.First(foo => foo.kind == kind).prefab;
                prefabRoom.transform.position = _currentRoomPosition;
                _currentGameObject = Object.Instantiate(prefabRoom);
                break;
            case RoomKind.Left:
                Object.Instantiate(playerManager.prefabsRoom.First(foo => foo.kind == kind).prefab);
                break;
            case RoomKind.Middle:
                Object.Instantiate(playerManager.prefabsRoom.First(foo => foo.kind == kind).prefab);
                break;
            case RoomKind.Right:
                Object.Instantiate(playerManager.prefabsRoom.First(foo => foo.kind == kind).prefab);
                break;
            case RoomKind.DoorWall:
                Object.Instantiate(playerManager.prefabsRoom.First(foo => foo.kind == kind).prefab);
                break;
        }
    }

    public override void Update()
    {
        if (_currentGameObject != null) 
        {
            _currentRoomPosition = playerManager.navigateInputAction.ReadValue<Vector2>();
            _currentRoomPosition.z = playerManager.roomPlacementDistance;
        }
    }
}
