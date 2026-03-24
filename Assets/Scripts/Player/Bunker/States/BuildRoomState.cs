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
        if (_currentGameObject != null && !_currentGameObject.GetComponent<RoomManager>().GetPlaced()) 
        { 
            Object.Destroy(_currentGameObject);
        }
        else
        {
            _currentGameObject = null;
        }
    }

    public override void HandleInput()
    {
        if (_currentGameObject != null) 
        {
            RoomManager roomManager = _currentGameObject.GetComponent<RoomManager>();

            if (playerManager.confirmInputAction.triggered && roomManager.cornerInteractionType == CornerInteractionType.Buildable) 
            {
                roomManager.SetPlaced();
                roomManager.SetOriginalMaterial();
                _currentGameObject.GetComponent<CornerDetector>().SnapToDetectedCorner(playerManager.roomPlacementDistance);
                roomManager.ResolveRailAfterPlacement();
                playerManager.ChangeState(playerManager.navigateState);
            }
        }
    }

    public void InstantiateRoom(RoomKind kind)
    {
        GameObject prefabRoom = playerManager.prefabsRoom[kind];

        if (kind == RoomKind.Intersection)
        {
            playerManager.intersectionManager.AddIntersection(prefabRoom);
            playerManager.ChangeState(playerManager.navigateState);
            return;
        }

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
