using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BunkerNpcArrivalManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private IntersectionElevator _entryElevator;
    [SerializeField] private Transform _spawnPointInsideElevator;
    [SerializeField] private Transform _railHandoffPoint;
    [SerializeField] private RoomManager _handoffRoom;
    [SerializeField] private RoomManager _leftHandoffRoom;
    [SerializeField] private RoomManager _rightHandoffRoom;
    [SerializeField] private BunkerSceneRoomRegistry _roomRegistry;
    [SerializeField] private BunkerColonyManager _colonyManager;

    [Header("NPC Prefabs")]
    [SerializeField] private GameObject[] _npcPrefabs;

    [Header("Timing")]
    [SerializeField] private float _delayBetweenNpcs = 0.2f;
    [SerializeField] private float _delayAfterArrivalOnRail = 0.1f;

    public IEnumerator SpawnArrivalSequence(List<BunkerSavedNpcData> npcDataList)
    {
        if (npcDataList == null || npcDataList.Count == 0)
            yield break;

        for (int i = 0; i < npcDataList.Count; i++)
        {
            BunkerSavedNpcData npcData = npcDataList[i];
            yield return SpawnSingleNpcRoutine(npcData);

            if (_delayBetweenNpcs > 0f)
                yield return new WaitForSeconds(_delayBetweenNpcs);
        }

        if (_colonyManager != null)
        {
            _colonyManager.RefreshWorldLists();
            _colonyManager.EvaluateAssignments();
        }
    }

    private IEnumerator SpawnSingleNpcRoutine(BunkerSavedNpcData npcData)
    {
        RoomManager startRoom = ResolveArrivalRoom(npcData, out IntersectionRailPort arrivalPort);
        if (!IsValidArrivalRoom(startRoom))
            yield break;

        if (_npcPrefabs == null || _npcPrefabs.Length == 0)
            yield break;

        IntersectionSplineExpander startIntersection = startRoom.GetRailOwner();
        if (startIntersection == null)
            yield break;

        int prefabIndex = Mathf.Clamp(npcData.prefabIndex, 0, _npcPrefabs.Length - 1);
        GameObject npcPrefab = _npcPrefabs[prefabIndex];
        if (npcPrefab == null)
            yield break;

        if (_entryElevator != null)
            yield return _entryElevator.PlayOpenAndWait();

        if (arrivalPort == IntersectionRailPort.None)
            arrivalPort = startIntersection.GetRoomPort(startRoom);

        Vector3 spawnPosition = startIntersection.GetElevatorWorldPositionForPort(arrivalPort);
        if (_spawnPointInsideElevator != null)
            spawnPosition = _spawnPointInsideElevator.position;

        GameObject npcInstance = Instantiate(npcPrefab, spawnPosition, Quaternion.identity);

        BunkerSpawnedNpcRuntime runtimeTag = npcInstance.GetComponent<BunkerSpawnedNpcRuntime>();
        if (runtimeTag == null)
            runtimeTag = npcInstance.AddComponent<BunkerSpawnedNpcRuntime>();
        runtimeTag.SetPrefabIndex(prefabIndex);

        NPCRailWalker railWalker = npcInstance.GetComponent<NPCRailWalker>();
        NPCBunkerWorker bunkerWorker = npcInstance.GetComponent<NPCBunkerWorker>();

        if (railWalker == null || bunkerWorker == null)
            yield break;

        bunkerWorker.LoadNeedsValues(npcData.hunger, npcData.thirst, npcData.fatigue);

        railWalker.SnapToIntersectionPort(startIntersection, IntersectionRailPort.Elevator, arrivalPort);

        IntersectionRailPort startPort = arrivalPort != IntersectionRailPort.None
            ? arrivalPort
            : startIntersection.GetRoomPort(startRoom);

        bool reachedValidStartRoom = false;

        if (startPort != IntersectionRailPort.None)
        {
            bool movedToPort = railWalker.MovePortToPort(startIntersection, IntersectionRailPort.Elevator, startPort);

            if (movedToPort)
                yield return WaitForRailWalker(railWalker);

            bool movedToRoom = railWalker.MoveToRoomFromBranchStart(startRoom);

            if (movedToRoom)
                yield return WaitForRailWalker(railWalker);

            reachedValidStartRoom = movedToPort && movedToRoom;
        }

        if (!reachedValidStartRoom)
            railWalker.InitializeFromRoom(startRoom);

        bunkerWorker.PrimeArrivalRoomReference(startRoom);
        railWalker.SetStartRoomReferenceOnly(startRoom);

        if (_entryElevator != null)
            yield return _entryElevator.PlayCloseAndWait();

        if (_delayAfterArrivalOnRail > 0f)
            yield return new WaitForSeconds(_delayAfterArrivalOnRail);

        if (_roomRegistry != null &&
            !string.IsNullOrWhiteSpace(npcData.targetRoomId) &&
            _roomRegistry.TryGetMachineByRoomId(npcData.targetRoomId, out MachineManager targetMachine))
        {
            bunkerWorker.AssignMachine(targetMachine);
        }
        else if (_colonyManager != null)
        {
            _colonyManager.RefreshWorldLists();
            _colonyManager.AssignBestMachineNow(bunkerWorker);
        }
    }

    private RoomManager ResolveArrivalRoom(BunkerSavedNpcData npcData, out IntersectionRailPort arrivalPort)
    {
        arrivalPort = ResolveArrivalPortFromTarget(npcData);

        if (arrivalPort == IntersectionRailPort.LeftRooms && IsValidArrivalRoom(_leftHandoffRoom))
            return _leftHandoffRoom;

        if (arrivalPort == IntersectionRailPort.RightRooms && IsValidArrivalRoom(_rightHandoffRoom))
            return _rightHandoffRoom;

        if (IsValidArrivalRoom(_handoffRoom))
        {
            IntersectionSplineExpander handoffIntersection = _handoffRoom.GetRailOwner();
            if (handoffIntersection != null)
                arrivalPort = handoffIntersection.GetRoomPort(_handoffRoom);

            return _handoffRoom;
        }

        if (IsValidArrivalRoom(_rightHandoffRoom))
        {
            arrivalPort = _rightHandoffRoom.GetRailOwner().GetRoomPort(_rightHandoffRoom);
            return _rightHandoffRoom;
        }

        if (IsValidArrivalRoom(_leftHandoffRoom))
        {
            arrivalPort = _leftHandoffRoom.GetRailOwner().GetRoomPort(_leftHandoffRoom);
            return _leftHandoffRoom;
        }

        return null;
    }

    private bool IsValidArrivalRoom(RoomManager room)
    {
        if (room == null)
            return false;

        if (room.GetRailOwner() == null)
            return false;

        if (room.GetBranchIndex() < 0)
            return false;

        return true;
    }

    private IntersectionRailPort ResolveArrivalPortFromTarget(BunkerSavedNpcData npcData)
    {
        if (npcData == null || _roomRegistry == null || string.IsNullOrWhiteSpace(npcData.targetRoomId))
            return IntersectionRailPort.None;

        if (!_roomRegistry.TryGetRoomById(npcData.targetRoomId, out RoomManager targetRoom) || targetRoom == null)
            return IntersectionRailPort.None;

        IntersectionSplineExpander targetIntersection = targetRoom.GetRailOwner();
        if (targetIntersection == null)
            return IntersectionRailPort.None;

        return targetIntersection.GetRoomPort(targetRoom);
    }

    private IEnumerator WaitForRailWalker(NPCRailWalker railWalker)
    {
        if (railWalker == null)
            yield break;

        while (railWalker.IsMovingOnRail)
            yield return null;
    }
}