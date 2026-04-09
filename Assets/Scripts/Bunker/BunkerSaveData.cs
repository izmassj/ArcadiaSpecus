using System;
using System.Collections.Generic;

[Serializable]
public class BunkerSaveFileData
{
    public BunkerSaveMetadata metadata = new BunkerSaveMetadata();
    public BunkerWorldSaveData world = new BunkerWorldSaveData();
}

[Serializable]
public class BunkerSaveMetadata
{
    public string slotId;
    public string displayName;
    public int gameMode;
    public long createdUtcTicks;
    public long updatedUtcTicks;
    public int previewDay;
    public int previewScrap;
    public int previewElectricity;
    public int previewWater;
    public int previewFood;
    public int previewNpcCount;
}

[Serializable]
public class BunkerWorldSaveData
{
    public BunkerResourceSaveData resources = new BunkerResourceSaveData();
    public BunkerDayCycleSaveData dayCycle = new BunkerDayCycleSaveData();
    public int colonyMode;
    public List<BunkerSavedMachineData> machines = new List<BunkerSavedMachineData>();
    public List<BunkerSavedNpcData> npcs = new List<BunkerSavedNpcData>();
}

[Serializable]
public class BunkerSavedMachineData
{
    public string roomId;
    public int machineKind;
    public MachineRuntimeSaveData runtime = new MachineRuntimeSaveData();
}

[Serializable]
public class BunkerSavedNpcData
{
    public int prefabIndex;
    public float hunger;
    public float thirst;
    public float fatigue;
    public string targetRoomId;
}
