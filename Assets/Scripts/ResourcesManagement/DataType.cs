// DataType.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DwellerData
{
    public string dwellerName;
    public Vector3 position;
    public string assignedStationId;
    public float workEfficiency;
}

[System.Serializable]
public class WorkStationData
{
    public string stationId;
    public string stationName;
    public List<string> assignedWorkerNames;
    public Vector3 position;
    public Quaternion rotation;
}

/// <summary>
/// Estados posibles de un NPC
/// </summary>
public enum DwellerState
{
    Idle,
    MovingToWork,
    Working,
    Eating,
    Drinking,
    Resting,
    Dead // NUEVO: Estado de muerte
}