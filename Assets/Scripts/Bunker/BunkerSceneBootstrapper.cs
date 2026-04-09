using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class BunkerStartModeDefinition
{
    public int npcCount = 1;
    public int productionMachineCount = 1;
    public int stationMachineCount = 0;
    public int scrap = 25;
    public int electricity = 10;
    public int water = 10;
    public int food = 10;
}

public class BunkerSceneBootstrapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private BunkerResourceManager _resourceManager;
    [SerializeField] private BunkerDayCycleManager _dayCycleManager;
    [SerializeField] private BunkerColonyManager _colonyManager;
    [SerializeField] private BunkerSceneRoomRegistry _roomRegistry;
    [SerializeField] private BunkerNpcArrivalManager _npcArrivalManager;
    [SerializeField] private BunkerExplorationRewardPanelUI _explorationRewardPanel;

    [Header("Start Modes")]
    [SerializeField] private BunkerStartModeDefinition _multitudDefinition = new BunkerStartModeDefinition
    {
        npcCount = 3,
        productionMachineCount = 2,
        stationMachineCount = 1,
        scrap = 15,
        electricity = 5,
        water = 5,
        food = 5
    };

    [SerializeField] private BunkerStartModeDefinition _prosperoDefinition = new BunkerStartModeDefinition
    {
        npcCount = 1,
        productionMachineCount = 1,
        stationMachineCount = 0,
        scrap = 40,
        electricity = 15,
        water = 15,
        food = 15
    };

    [Header("Auto Save")]
    [SerializeField] private bool _autoSaveEnabled = true;
    [SerializeField] private float _autoSaveIntervalSeconds = 20f;

    private float _autoSaveTimer;
    private bool _bootstrapFinished;
    private BunkerPendingRewardData _pendingExplorationOutcomeToShow;

    private void Start()
    {
        BunkerSessionLaunch.SetCurrentBunkerScene(SceneManager.GetActiveScene().name);
        StartCoroutine(BootstrapRoutine());
    }

    private void Update()
    {
        if (!_bootstrapFinished || !_autoSaveEnabled || string.IsNullOrWhiteSpace(BunkerSessionLaunch.CurrentSlotId))
            return;

        _autoSaveTimer += Time.unscaledDeltaTime;
        if (_autoSaveTimer < _autoSaveIntervalSeconds)
            return;

        _autoSaveTimer = 0f;
        SaveCurrentSession();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveCurrentSession();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveCurrentSession();
    }

    private void OnApplicationQuit()
    {
        SaveCurrentSession();
    }

    private IEnumerator BootstrapRoutine()
    {
        if (_transition != null)
            _transition.SetCoveredInstant();

        yield return null;

        string slotId = BunkerSessionLaunch.CurrentSlotId;
        BunkerLaunchOperation operation = BunkerSessionLaunch.ConsumePendingOperation();

        if (string.IsNullOrWhiteSpace(slotId))
        {
            if (_transition != null)
                yield return _transition.PlayRevealRoutine();

            _bootstrapFinished = true;
            yield break;
        }

        BunkerSaveFileData saveFile = BunkerSaveSystem.Load(slotId);
        if (saveFile == null)
        {
            if (_transition != null)
                yield return _transition.PlayRevealRoutine();

            _bootstrapFinished = true;
            yield break;
        }

        if (operation == BunkerLaunchOperation.Create)
        {
            BuildInitialWorldForMode(saveFile);
            BunkerSaveSystem.Save(saveFile);
        }

        ApplyWorldStateWithoutNpcVisuals(saveFile.world);
        ApplyPendingExplorationOutcomeToLiveWorld(saveFile.world);
        yield return null;

        if (_transition != null)
            yield return _transition.PlayRevealRoutine();

        if (_npcArrivalManager != null)
            yield return _npcArrivalManager.SpawnArrivalSequence(saveFile.world.npcs);

        if (_colonyManager != null)
        {
            _colonyManager.RefreshWorldLists();
            _colonyManager.EvaluateAssignments();
        }

        if (_explorationRewardPanel != null && _pendingExplorationOutcomeToShow.HasAnyOutcome())
            yield return _explorationRewardPanel.ShowRoutine(_pendingExplorationOutcomeToShow);

        SaveCurrentSession();
        _bootstrapFinished = true;
    }

    public void SaveCurrentSession()
    {
        string slotId = BunkerSessionLaunch.CurrentSlotId;
        if (string.IsNullOrWhiteSpace(slotId))
            return;

        BunkerSaveFileData saveFile = BunkerSaveSystem.Load(slotId);
        if (saveFile == null)
            return;

        saveFile.world = CaptureWorldState();
        BunkerSaveSystem.Save(saveFile);
    }

    private void ApplyPendingExplorationOutcomeToLiveWorld(BunkerWorldSaveData worldData)
    {
        _pendingExplorationOutcomeToShow = BunkerSessionLaunch.ConsumePendingRewards();
        if (!_pendingExplorationOutcomeToShow.HasAnyOutcome())
            return;

        if (_resourceManager != null)
        {
            _resourceManager.Add(BunkerResourceType.Scrap, _pendingExplorationOutcomeToShow.scrap);
            _resourceManager.Add(BunkerResourceType.Electricity, _pendingExplorationOutcomeToShow.electricity);
            _resourceManager.Add(BunkerResourceType.Water, _pendingExplorationOutcomeToShow.water);
            _resourceManager.Add(BunkerResourceType.Food, _pendingExplorationOutcomeToShow.food);
        }

        if (worldData == null || _pendingExplorationOutcomeToShow.joinedInhabitants <= 0)
            return;

        for (int i = 0; i < _pendingExplorationOutcomeToShow.joinedInhabitants; i++)
        {
            worldData.npcs.Add(new BunkerSavedNpcData
            {
                prefabIndex = UnityEngine.Random.Range(0, 2),
                hunger = 0f,
                thirst = 0f,
                fatigue = 0f,
                targetRoomId = string.Empty
            });
        }
    }

    private void BuildInitialWorldForMode(BunkerSaveFileData saveFile)
    {
        if (saveFile == null || saveFile.metadata == null)
            return;

        BunkerGameMode mode = (BunkerGameMode)saveFile.metadata.gameMode;
        BunkerStartModeDefinition definition = mode == BunkerGameMode.Multitud ? _multitudDefinition : _prosperoDefinition;

        saveFile.world = new BunkerWorldSaveData();
        saveFile.world.resources.scrap = definition.scrap;
        saveFile.world.resources.electricity = definition.electricity;
        saveFile.world.resources.water = definition.water;
        saveFile.world.resources.food = definition.food;
        saveFile.world.dayCycle = new BunkerDayCycleSaveData
        {
            currentDay = 1,
            currentDayMinutesElapsed = 0f,
            isPaused = false,
            isWaitingForNextDay = false,
            todayCollectedAmounts = new int[Enum.GetValues(typeof(BunkerResourceType)).Length],
            pendingSummaryData = null
        };
        saveFile.world.colonyMode = (int)BunkerManagementMode.Stable;

        System.Random rng = new System.Random();

        List<MachineKind> productionPool = new List<MachineKind>
        {
            MachineKind.ElectricityMachine,
            MachineKind.FoodMachine,
            MachineKind.ScrapMachine,
            MachineKind.WaterMachine
        };

        List<MachineKind> stationPool = new List<MachineKind>
        {
            MachineKind.Bed,
            MachineKind.VendingMachine,
            MachineKind.WaterCooler
        };

        for (int i = 0; i < definition.productionMachineCount; i++)
        {
            RoomManager room = _roomRegistry != null ? _roomRegistry.GetRandomFreeProductionRoom(rng) : null;
            if (room == null)
                break;

            MachineKind kind = productionPool[rng.Next(0, productionPool.Count)];
            saveFile.world.machines.Add(new BunkerSavedMachineData
            {
                roomId = _roomRegistry.GetRoomId(room),
                machineKind = (int)kind,
                runtime = new MachineRuntimeSaveData()
            });
        }

        for (int i = 0; i < definition.stationMachineCount; i++)
        {
            RoomManager room = _roomRegistry != null ? _roomRegistry.GetRandomFreeStationRoom(rng) : null;
            if (room == null)
                break;

            MachineKind kind = stationPool[rng.Next(0, stationPool.Count)];
            saveFile.world.machines.Add(new BunkerSavedMachineData
            {
                roomId = _roomRegistry.GetRoomId(room),
                machineKind = (int)kind,
                runtime = new MachineRuntimeSaveData()
            });
        }

        for (int i = 0; i < definition.npcCount; i++)
        {
            saveFile.world.npcs.Add(new BunkerSavedNpcData
            {
                prefabIndex = UnityEngine.Random.Range(0, 2),
                hunger = 0f,
                thirst = 0f,
                fatigue = 0f,
                targetRoomId = string.Empty
            });
        }
    }

    private void ApplyWorldStateWithoutNpcVisuals(BunkerWorldSaveData worldData)
    {
        if (worldData == null)
            return;

        if (_roomRegistry != null)
        {
            _roomRegistry.ClearAllRegisteredMachines();

            if (worldData.machines != null)
            {
                for (int i = 0; i < worldData.machines.Count; i++)
                {
                    BunkerSavedMachineData machineData = worldData.machines[i];
                    MachineManager machine = _roomRegistry.PlaceMachineInRoom(machineData.roomId, (MachineKind)machineData.machineKind);
                    if (machine != null)
                        machine.LoadRuntimeSaveData(machineData.runtime);
                }
            }
        }

        if (_resourceManager != null)
            _resourceManager.LoadFromData(worldData.resources);

        if (_dayCycleManager != null)
            _dayCycleManager.LoadFromData(worldData.dayCycle);

        if (_colonyManager != null)
            _colonyManager.LoadMode((BunkerManagementMode)worldData.colonyMode, false);
    }

    private BunkerWorldSaveData CaptureWorldState()
    {
        BunkerWorldSaveData worldData = new BunkerWorldSaveData();

        if (_resourceManager != null)
            worldData.resources = _resourceManager.GetSaveData();

        if (_dayCycleManager != null)
            worldData.dayCycle = _dayCycleManager.GetSaveData();

        if (_colonyManager != null)
            worldData.colonyMode = (int)_colonyManager.GetCurrentMode();

        if (_roomRegistry != null)
        {
            for (int i = 0; i < _roomRegistry.Entries.Count; i++)
            {
                BunkerRoomRegistryEntry entry = _roomRegistry.Entries[i];
                if (entry.room == null)
                    continue;

                MachineManager machine = entry.room.GetCurrentMachine();
                if (machine == null)
                    continue;

                worldData.machines.Add(new BunkerSavedMachineData
                {
                    roomId = entry.id,
                    machineKind = (int)machine.typeOfMachine,
                    runtime = machine.GetRuntimeSaveData()
                });
            }
        }

        NPCBunkerWorker[] workers = FindObjectsByType<NPCBunkerWorker>(FindObjectsSortMode.None);
        for (int i = 0; i < workers.Length; i++)
        {
            NPCBunkerWorker worker = workers[i];
            if (worker == null)
                continue;

            int prefabIndex = 0;
            BunkerSpawnedNpcRuntime runtimeTag = worker.GetComponent<BunkerSpawnedNpcRuntime>();
            if (runtimeTag != null)
                prefabIndex = runtimeTag.PrefabIndex;

            string targetRoomId = string.Empty;
            MachineManager targetMachine = worker.GetAssignedOrTargetMachine();
            if (_roomRegistry != null && targetMachine != null)
            {
                RoomManager ownerRoom = targetMachine.GetOwnerRoom();
                targetRoomId = _roomRegistry.GetRoomId(ownerRoom);
            }

            worldData.npcs.Add(new BunkerSavedNpcData
            {
                prefabIndex = prefabIndex,
                hunger = worker.GetHunger(),
                thirst = worker.GetThirst(),
                fatigue = worker.GetFatigue(),
                targetRoomId = targetRoomId
            });
        }

        return worldData;
    }
}
