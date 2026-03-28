using UnityEngine;

public class MachineManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] public MachineKind typeOfMachine;

    [Header("NPC")]
    [SerializeField] private RoomManager _ownerRoom;
    [SerializeField] private Transform _workPointA;
    [SerializeField] private Transform _workPointB;

    private NPCBunkerWorker _workerInSlotA;
    private NPCBunkerWorker _workerInSlotB;

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

    public bool TryReserveWorkSlot(NPCBunkerWorker worker, out int slotIndex, out Vector3 workPointWorldPosition)
    {
        slotIndex = -1;
        workPointWorldPosition = transform.position;

        if (worker == null)
            return false;

        if (_workerInSlotA == worker)
        {
            slotIndex = 0;
            workPointWorldPosition = GetWorkPointWorldPosition(slotIndex);
            return true;
        }

        if (_workerInSlotB == worker)
        {
            slotIndex = 1;
            workPointWorldPosition = GetWorkPointWorldPosition(slotIndex);
            return true;
        }

        if (_workerInSlotA == null)
        {
            _workerInSlotA = worker;
            slotIndex = 0;
            workPointWorldPosition = GetWorkPointWorldPosition(slotIndex);
            return true;
        }

        if (_workerInSlotB == null)
        {
            _workerInSlotB = worker;
            slotIndex = 1;
            workPointWorldPosition = GetWorkPointWorldPosition(slotIndex);
            return true;
        }

        return false;
    }

    public void ReleaseWorkSlot(NPCBunkerWorker worker)
    {
        if (worker == null)
            return;

        if (_workerInSlotA == worker)
            _workerInSlotA = null;

        if (_workerInSlotB == worker)
            _workerInSlotB = null;
    }

    public int GetOccupancyCount()
    {
        int count = 0;

        if (_workerInSlotA != null)
            count++;

        if (_workerInSlotB != null)
            count++;

        return count;
    }

    public Vector3 GetWorkPointWorldPosition()
    {
        if (_workerInSlotA == null)
            return GetWorkPointWorldPosition(0);

        if (_workerInSlotB == null)
            return GetWorkPointWorldPosition(1);

        return GetWorkPointWorldPosition(0);
    }

    public Vector3 GetWorkPointWorldPosition(int slotIndex)
    {
        if (slotIndex <= 0 && _workPointA != null)
            return _workPointA.position;

        if (slotIndex == 1 && _workPointB != null)
            return _workPointB.position;

        ResolveOwnerRoom();

        Vector3 machinePosition = transform.position;
        Vector3 railCenter = _ownerRoom != null ? _ownerRoom.GetRailCenterWorldPosition() : transform.position;
        Vector3 basePoint = (railCenter + machinePosition) * 0.5f;

        Vector3 toMachine = machinePosition - railCenter;
        toMachine.y = 0f;

        Vector3 side = Vector3.Cross(Vector3.up, toMachine.normalized);

        if (side.sqrMagnitude <= 0.0001f)
            side = Vector3.ProjectOnPlane(transform.right, Vector3.up);

        if (side.sqrMagnitude <= 0.0001f)
            side = Vector3.right;

        side.Normalize();

        float sideSign = slotIndex == 1 ? 0.5f : -0.5f;
        return basePoint + side * (sideSign);
    }

    private void ResolveOwnerRoom()
    {
        if (_ownerRoom != null)
            return;

        _ownerRoom = GetComponentInParent<RoomManager>();
    }
}
