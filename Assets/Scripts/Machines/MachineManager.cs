using UnityEngine;

public class MachineManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] public MachineKind typeOfMachine;

    [Header("NPC")]
    [SerializeField] private RoomManager _ownerRoom;
    [SerializeField] private Transform _workPointA;
    [SerializeField] private Transform _workPointB;
    [SerializeField] private Vector3 _workPointOffset = Vector3.zero;
    [SerializeField] private float _fallbackSlotSeparation = 0.6f;

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
            workPointWorldPosition = GetWorkPointWorldPosition(0);
            return true;
        }

        if (_workerInSlotB == worker)
        {
            slotIndex = 1;
            workPointWorldPosition = GetWorkPointWorldPosition(1);
            return true;
        }

        if (_workerInSlotA == null)
        {
            _workerInSlotA = worker;
            slotIndex = 0;
            workPointWorldPosition = GetWorkPointWorldPosition(0);
            return true;
        }

        if (_workerInSlotB == null)
        {
            _workerInSlotB = worker;
            slotIndex = 1;
            workPointWorldPosition = GetWorkPointWorldPosition(1);
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

    public Vector3 GetWorkPointWorldPosition(int slotIndex)
    {
        if (slotIndex == 0 && _workPointA != null)
            return _workPointA.position;

        if (slotIndex == 1 && _workPointB != null)
            return _workPointB.position;

        ResolveOwnerRoom();

        Vector3 basePosition = transform.position + _workPointOffset;

        if (_ownerRoom == null)
        {
            float offset = slotIndex == 0 ? _fallbackSlotSeparation * 0.5f : -_fallbackSlotSeparation * 0.5f;
            return basePosition + transform.right * offset;
        }

        Vector3 railCenter = _ownerRoom.GetRailCenterWorldPosition();
        Vector3 toRail = (railCenter - basePosition).normalized;

        if (toRail.sqrMagnitude <= 0.0001f)
            toRail = -transform.forward;

        Vector3 side = Vector3.Cross(Vector3.up, toRail).normalized;
        Vector3 centerPoint = (railCenter + basePosition) * 0.5f;
        float sideOffset = slotIndex == 0 ? _fallbackSlotSeparation * 0.5f : -_fallbackSlotSeparation * 0.5f;
        return centerPoint + side * sideOffset;
    }

    private void ResolveOwnerRoom()
    {
        if (_ownerRoom != null)
            return;

        _ownerRoom = GetComponentInParent<RoomManager>();
    }
}
