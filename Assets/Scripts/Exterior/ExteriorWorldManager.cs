using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager persistente del exterior.
///
/// - Decide qué recursos dan los CollectibleItem en escenas de exploración.
/// - Los nodos evolucionan con el tiempo: se agotan, tienen cooldown y pueden ser saqueados.
/// - Guarda el estado en PlayerPrefs (JSON) para que al volver a entrar a un exterior
///   el mundo esté "consistente".
///
/// Importante: pensado para funcionar sin editar escenas.
/// </summary>
public class ExteriorWorldManager : MonoBehaviour
{
    public static ExteriorWorldManager Instance { get; private set; }

    [Header("Persistencia")]
    [SerializeField] private bool _dontDestroyOnLoad = true;
    [SerializeField] private string _prefsKey = "ArcadiaSpecus.ExteriorWorldState";

    [Header("Debug")]
    [SerializeField] private bool _verboseLogs = true;

    private ExteriorWorldState _state;
    private readonly System.Random _rng = new System.Random();

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        ExteriorWorldManager existing = FindObjectOfType<ExteriorWorldManager>(true);
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject go = new GameObject("ExteriorWorldManager");
        Instance = go.AddComponent<ExteriorWorldManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (_dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        LoadState();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!ExteriorZoneRules.IsExteriorScene(scene.name))
        {
            return;
        }

        ApplyZoneEvolution(scene.name);
        ApplyNodeVisibility(scene.name);
    }

    /// <summary>
    /// Llamado desde CollectibleItem al recoger.
    /// Devuelve false si el nodo no está disponible (agotado/cooldown/saqueado).
    /// </summary>
    public bool TryCollectFrom(CollectibleItem item, out ResourceType rewardType, out int rewardAmount)
    {
        rewardType = ResourceType.Materials;
        rewardAmount = 0;

        if (item == null) return false;

        string sceneName = item.gameObject.scene.name;
        if (!ExteriorZoneRules.IsExteriorScene(sceneName))
        {
            // No es exterior: respetamos comportamiento original.
            return true;
        }

        string nodeId = ComputeStableNodeId(item.gameObject);
        if (string.IsNullOrWhiteSpace(nodeId)) return true;

        ExteriorWorldState.NodeState node = GetOrCreateNode(sceneName, nodeId);
        DateTime now = DateTime.UtcNow;

        if (!IsNodeAvailable(node, now) || node.remainingHarvests <= 0)
        {
            if (_verboseLogs)
            {
                Debug.Log($"[Exterior] Nodo no disponible: {nodeId} (cooldown/agotado)");
            }
            return false;
        }

        rewardType = (ResourceType)node.resourceType;
        rewardAmount = Mathf.Max(1, node.baseAmount);

        node.remainingHarvests = Mathf.Max(0, node.remainingHarvests - 1);
        node.timesCollected++;
        node.nextAvailableUtc = now.AddMinutes(ExteriorZoneRules.GetRespawnMinutes((ExteriorZoneKind)node.zoneKind)).ToString("o");

        SaveState();
        return true;
    }

    private void ApplyNodeVisibility(string sceneName)
    {
        // Incluye inactivos para poder re-activarlos al volver.
        CollectibleItem[] items = FindObjectsOfType<CollectibleItem>(true);
        DateTime now = DateTime.UtcNow;

        int available = 0;
        int blocked = 0;

        for (int i = 0; i < items.Length; i++)
        {
            CollectibleItem item = items[i];
            if (item == null) continue;
            if (item.gameObject.scene.name != sceneName) continue;

            string nodeId = ComputeStableNodeId(item.gameObject);
            if (string.IsNullOrWhiteSpace(nodeId)) continue;

            ExteriorWorldState.NodeState node = GetOrCreateNode(sceneName, nodeId);
            bool isAvailable = IsNodeAvailable(node, now) && node.remainingHarvests > 0;

            if (item.gameObject.activeSelf != isAvailable)
            {
                item.gameObject.SetActive(isAvailable);
            }

            if (isAvailable) available++; else blocked++;
        }

        if (_verboseLogs)
        {
            ExteriorZoneKind kind = ExteriorZoneRules.GetZoneKind(sceneName);
            Debug.Log($"[Exterior] {ExteriorZoneRules.GetZoneDisplayName(kind)}: disponibles={available}, no disponibles={blocked}");
        }
    }

    private void ApplyZoneEvolution(string sceneName)
    {
        if (_state == null) LoadState();

        ExteriorWorldState.ZoneState zone = _state.FindZone(sceneName);
        DateTime now = DateTime.UtcNow;
        if (zone == null)
        {
            zone = new ExteriorWorldState.ZoneState { sceneName = sceneName, lastVisitUtc = now.ToString("o"), totalRaidsApplied = 0 };
            _state.zones.Add(zone);
            SaveState();
            return;
        }

        if (!DateTime.TryParse(zone.lastVisitUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastVisit))
        {
            lastVisit = now;
        }

        double hoursAway = Math.Max(0.0, (now - lastVisit).TotalHours);
        ExteriorZoneKind kind = ExteriorZoneRules.GetZoneKind(sceneName);
        int raidEvery = Math.Max(1, ExteriorZoneRules.GetRaidEveryHours(kind));
        int raidsToApply = Mathf.Clamp((int)Math.Floor(hoursAway / raidEvery), 0, 3);

        if (raidsToApply <= 0)
        {
            zone.lastVisitUtc = now.ToString("o");
            SaveState();
            return;
        }

        List<ExteriorWorldState.NodeState> candidates = new List<ExteriorWorldState.NodeState>();
        for (int i = 0; i < _state.nodes.Count; i++)
        {
            var n = _state.nodes[i];
            if (n == null) continue;
            if (n.sceneName != sceneName) continue;
            if (n.remainingHarvests <= 0) continue;
            candidates.Add(n);
        }

        int totalRaidedNodes = 0;
        for (int r = 0; r < raidsToApply; r++)
        {
            if (candidates.Count <= 0) break;
            int idx = _rng.Next(0, candidates.Count);
            var node = candidates[idx];

            node.remainingHarvests = Mathf.Max(0, node.remainingHarvests - 1);
            node.timesRaided++;
            totalRaidedNodes++;

            if (node.remainingHarvests <= 0)
            {
                candidates.RemoveAt(idx);
            }
        }

        zone.totalRaidsApplied += raidsToApply;
        zone.lastVisitUtc = now.ToString("o");

        SaveState();

        if (_verboseLogs)
        {
            Debug.Log($"[Exterior] Estuviste fuera {hoursAway:0.0}h: otros supervivientes han saqueado {totalRaidedNodes} nodos en '{ExteriorZoneRules.GetZoneDisplayName(kind)}'.");
        }
    }

    private bool IsNodeAvailable(ExteriorWorldState.NodeState node, DateTime now)
    {
        if (node == null) return false;
        if (string.IsNullOrWhiteSpace(node.nextAvailableUtc)) return true;

        if (!DateTime.TryParse(node.nextAvailableUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime next))
        {
            return true;
        }

        return now >= next;
    }

    private ExteriorWorldState.NodeState GetOrCreateNode(string sceneName, string nodeId)
    {
        if (_state == null) LoadState();

        ExteriorWorldState.NodeState existing = _state.FindNode(nodeId);
        if (existing != null) return existing;

        ExteriorZoneKind zoneKind = ExteriorZoneRules.GetZoneKind(sceneName);
        int hash = StableHash(nodeId);
        ResourceType type = ExteriorZoneRules.PickResourceType(zoneKind, hash);
        int baseAmount = ExteriorZoneRules.PickBaseAmount(zoneKind, type, hash);
        int maxHarvests = ExteriorZoneRules.PickMaxHarvests(zoneKind, hash);

        ExteriorWorldState.NodeState node = new ExteriorWorldState.NodeState
        {
            nodeId = nodeId,
            sceneName = sceneName,
            zoneKind = (int)zoneKind,
            resourceType = (int)type,
            baseAmount = Mathf.Max(1, baseAmount),
            remainingHarvests = Mathf.Max(1, maxHarvests),
            nextAvailableUtc = string.Empty,
            timesCollected = 0,
            timesRaided = 0
        };

        _state.nodes.Add(node);
        SaveState();
        return node;
    }

    private void LoadState()
    {
        try
        {
            string json = PlayerPrefs.GetString(_prefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                _state = new ExteriorWorldState();
                return;
            }

            _state = JsonUtility.FromJson<ExteriorWorldState>(json);
            if (_state == null) _state = new ExteriorWorldState();
        }
        catch
        {
            _state = new ExteriorWorldState();
        }
    }

    private void SaveState()
    {
        try
        {
            if (_state == null) return;
            string json = JsonUtility.ToJson(_state);
            PlayerPrefs.SetString(_prefsKey, json);
            PlayerPrefs.Save();
        }
        catch
        {
            // no-op
        }
    }

    public static string ComputeStableNodeId(GameObject go)
    {
        if (go == null) return string.Empty;

        Vector3 p = go.transform.position;
        int px = Mathf.RoundToInt(p.x * 10f);
        int py = Mathf.RoundToInt(p.y * 10f);
        int pz = Mathf.RoundToInt(p.z * 10f);
        return $"{go.scene.name}|{go.name}|{px},{py},{pz}";
    }

    private static int StableHash(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < s.Length; i++)
            {
                hash = hash * 31 + s[i];
            }
            return hash;
        }
    }

    [ContextMenu("Exterior/Reset World State")]
    public void DebugResetState()
    {
        PlayerPrefs.DeleteKey(_prefsKey);
        PlayerPrefs.Save();
        LoadState();
        Debug.Log("[Exterior] Estado exterior reseteado.");
    }

    /// <summary>
    /// Debug: simula que has estado fuera X horas para forzar saqueos/cambios al re-entrar.
    /// </summary>
    public void DebugSimulateTimeAway(string sceneName, float hours)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
        if (_state == null) LoadState();

        DateTime now = DateTime.UtcNow;
        ExteriorWorldState.ZoneState zone = _state.FindZone(sceneName);
        if (zone == null)
        {
            zone = new ExteriorWorldState.ZoneState { sceneName = sceneName, lastVisitUtc = now.ToString("o"), totalRaidsApplied = 0 };
            _state.zones.Add(zone);
        }

        DateTime backdated = now.AddHours(-Mathf.Abs(hours));
        zone.lastVisitUtc = backdated.ToString("o");
        SaveState();

        if (_verboseLogs)
        {
            Debug.Log($"[Exterior] DebugSimulateTimeAway: {sceneName} backdated {hours:0.0}h");
        }

        // Re-aplicar inmediatamente para ver el resultado si estás en esa escena.
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            ApplyZoneEvolution(sceneName);
            ApplyNodeVisibility(sceneName);
        }
    }
}
