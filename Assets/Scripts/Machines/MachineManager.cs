using UnityEngine;

public class MachineManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] public MachineKind typeOfMachine;

    [Header("NPC")]
    [SerializeField] private RoomManager _ownerRoom;
    [SerializeField] private Transform _workPoint;
    [SerializeField] private Vector3 _workPointOffset = Vector3.zero;

    private void Awake()
    {
        ResolveOwnerRoom();
    }

    public void AssignOwnerRoom(RoomManager room)
    {
        _ownerRoom = room;
    }

    public RoomManager GetOwnerRoom()
    {
        ResolveOwnerRoom();
        return _ownerRoom;
    }

    public Vector3 GetWorkPointWorldPosition()
    {
        if (_workPoint != null)
            return _workPoint.position;

        ResolveOwnerRoom();

        if (_ownerRoom == null)
            return transform.position + _workPointOffset;

        Vector3 railCenter = _ownerRoom.GetRailCenterWorldPosition();
        Vector3 machinePosition = transform.position + _workPointOffset;
        return (railCenter + machinePosition) * 0.5f;
    }

    private void ResolveOwnerRoom()
    {
        if (_ownerRoom != null)
            return;

        _ownerRoom = GetComponentInParent<RoomManager>();
    }
}
