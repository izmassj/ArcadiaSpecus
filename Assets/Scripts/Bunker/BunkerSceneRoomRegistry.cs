using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BunkerRoomRegistryEntry
{
    public string id;
    public RoomManager room;
    public bool allowStartingProductionMachine = true;
    public bool allowStartingStationMachine = true;
}

public class BunkerSceneRoomRegistry : MonoBehaviour
{
    [SerializeField] private PlayerBunkerManager _playerManager;
    [SerializeField] private List<BunkerRoomRegistryEntry> _entries = new List<BunkerRoomRegistryEntry>();

    public IReadOnlyList<BunkerRoomRegistryEntry> Entries => _entries;

    public void ClearAllRegisteredMachines()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].room != null)
                _entries[i].room.ClearMachineInstant();
        }
    }

    public bool TryGetRoomById(string roomId, out RoomManager room)
    {
        room = null;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].id != roomId)
                continue;

            room = _entries[i].room;
            return room != null;
        }

        return false;
    }

    public string GetRoomId(RoomManager room)
    {
        if (room == null)
            return string.Empty;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].room == room)
                return _entries[i].id;
        }

        return string.Empty;
    }

    public bool TryGetMachineByRoomId(string roomId, out MachineManager machine)
    {
        machine = null;

        if (!TryGetRoomById(roomId, out RoomManager room) || room == null)
            return false;

        machine = room.GetCurrentMachine();
        return machine != null;
    }

    public MachineManager PlaceMachineInRoom(string roomId, MachineKind machineKind)
    {
        if (!TryGetRoomById(roomId, out RoomManager room))
            return null;

        return PlaceMachineInRoom(room, machineKind);
    }

    public MachineManager PlaceMachineInRoom(RoomManager room, MachineKind machineKind)
    {
        if (room == null || _playerManager == null)
            return null;

        GameObject machinePrefab = _playerManager.machinesPrefab[machineKind];
        if (machinePrefab == null)
            return null;

        return room.InstantiateMachineAndGet(machinePrefab);
    }

    public RoomManager GetRandomFreeProductionRoom(System.Random rng)
    {
        return GetRandomFreeRoom(rng, true);
    }

    public RoomManager GetRandomFreeStationRoom(System.Random rng)
    {
        return GetRandomFreeRoom(rng, false);
    }

    private RoomManager GetRandomFreeRoom(System.Random rng, bool production)
    {
        List<RoomManager> candidates = new List<RoomManager>();

        for (int i = 0; i < _entries.Count; i++)
        {
            BunkerRoomRegistryEntry entry = _entries[i];
            if (entry.room == null)
                continue;

            if (entry.room.IsRoomOccupied())
                continue;

            if (entry.room.typeOfRoom == RoomKind.Intersection || entry.room.typeOfRoom == RoomKind.DoorWall)
                continue;

            if (production && !entry.allowStartingProductionMachine)
                continue;

            if (!production && !entry.allowStartingStationMachine)
                continue;

            candidates.Add(entry.room);
        }

        if (candidates.Count == 0)
            return null;

        int index = rng != null ? rng.Next(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }
}
