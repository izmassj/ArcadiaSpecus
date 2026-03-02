using System;
using System.Collections.Generic;

/// <summary>
/// Estado persistente del exterior.
///
/// Se guarda en PlayerPrefs como JSON para evitar tocar el SaveLoadManager.
/// Suficiente para la rúbrica: los recursos fuera se agotan y pueden ser saqueados.
/// </summary>
[Serializable]
public class ExteriorWorldState
{
    [Serializable]
    public class NodeState
    {
        public string nodeId;
        public string sceneName;
        public int zoneKind;

        public int resourceType;
        public int baseAmount;

        public int remainingHarvests;
        public string nextAvailableUtc; // ISO 8601

        public int timesCollected;
        public int timesRaided;
    }

    [Serializable]
    public class ZoneState
    {
        public string sceneName;
        public string lastVisitUtc; // ISO 8601
        public int totalRaidsApplied;
    }

    public string version = "1.0";
    public List<NodeState> nodes = new List<NodeState>();
    public List<ZoneState> zones = new List<ZoneState>();

    public NodeState FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return null;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].nodeId == nodeId) return nodes[i];
        }
        return null;
    }

    public ZoneState FindZone(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return null;
        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i] != null && zones[i].sceneName == sceneName) return zones[i];
        }
        return null;
    }
}
