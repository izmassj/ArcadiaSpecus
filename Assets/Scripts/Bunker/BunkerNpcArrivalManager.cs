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
        RoomManager startRoom = _handoffRoom;

        if (startRoom == null)
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

        Vector3 spawnPosition = startIntersection.GetElevatorWorldPosition();
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

        bunkerWorker.PrimeArrivalRoomReference(startRoom);
        bunkerWorker.LoadNeedsValues(npcData.hunger, npcData.thirst, npcData.fatigue);

        railWalker.SetStartRoomReferenceOnly(startRoom);
        railWalker.SnapToIntersectionPort(startIntersection, IntersectionRailPort.Elevator);

        IntersectionRailPort startPort = startIntersection.GetRoomPort(startRoom);
        bool leftElevatorOnRailCorrectly = false;

        if (startPort != IntersectionRailPort.None)
        {
            if (railWalker.MovePortToPort(startIntersection, IntersectionRailPort.Elevator, startPort))
            {
                railWalker.QueueMoveToRoom(startRoom);
                leftElevatorOnRailCorrectly = true;
                yield return WaitForRailWalker(railWalker);
            }
        }

        if (!leftElevatorOnRailCorrectly)
            railWalker.InitializeFromRoom(startRoom);

        if (_entryElevator != null)
            yield return _entryElevator.PlayCloseAndWait();

        if (_delayAfterArrivalOnRail > 0f)
            yield return new WaitForSeconds(_delayAfterArrivalOnRail);

        if (_roomRegistry != null && !string.IsNullOrWhiteSpace(npcData.targetRoomId) && _roomRegistry.TryGetMachineByRoomId(npcData.targetRoomId, out MachineManager targetMachine))
        {
            bunkerWorker.AssignMachine(targetMachine);
        }
        else if (_colonyManager != null)
        {
            _colonyManager.RefreshWorldLists();
            _colonyManager.AssignBestMachineNow(bunkerWorker);
        }
    }

    private IEnumerator WaitForRailWalker(NPCRailWalker railWalker)
    {
        if (railWalker == null)
            yield break;

        while (railWalker.IsMovingOnRail)
            yield return null;
    }
}
