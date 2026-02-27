using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema de guardado/carga simple y robusto para Arcadia Specus.
/// Guarda recursos, NPCs y estaciones. Mantiene compatibilidad con los save data de Lote 2.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    [Serializable]
    public class GameSaveData
    {
        public string saveVersion = "1.0";
        public string sceneName;
        public string saveDateUtc;
        public ResourceManager.ResourceSaveData resources;
        public List<DwellerNPC.DwellerSaveData> dwellers = new List<DwellerNPC.DwellerSaveData>();
        public List<WorkStation.WorkStationSaveData> workStations = new List<WorkStation.WorkStationSaveData>();
    }

    [Header("Configuración")]
    [SerializeField] private string _saveFileName = "arcadia_save.json";
    [SerializeField] private bool _dontDestroyOnLoad = true;
    [SerializeField] private bool _autoSaveOnQuit = true;
    [SerializeField] private bool _autoSaveOnSceneChange = false;
    [SerializeField] private bool _verboseLogs = true;

    private string _savePath;
    private string _saveDirectory;
    private bool _isLoading;

    public string GetSavePath() => _savePath;
    public bool IsLoading() => _isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitializePaths();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializePaths()
    {
        _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
        _savePath = Path.Combine(_saveDirectory, _saveFileName);

        try
        {
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }

            if (_verboseLogs)
            {
                Debug.Log($"[SaveLoad] Ruta de guardado: {_savePath}");
            }
        }
        catch (Exception _e)
        {
            Debug.LogError($"[SaveLoad] No se pudo crear el directorio de guardado: {_e.Message}");
            _saveDirectory = Application.persistentDataPath;
            _savePath = Path.Combine(_saveDirectory, _saveFileName);
        }
    }

    private void OnSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        if (_autoSaveOnSceneChange && !_isLoading)
        {
            // Comentario para vosotros: aquí podríais filtrar por escenas concretas si no queréis guardar en minijuegos.
            SaveGame();
        }

        // Re-sincronizar UI por si los managers persistentes sobreviven a un cambio de escena.
        TryRefreshUI();
    }

    public bool SaveExists()
    {
        return !string.IsNullOrWhiteSpace(_savePath) && File.Exists(_savePath);
    }

    public void SaveGame()
    {
        try
        {
            if (ResourceManager.Instance == null)
            {
                Debug.LogWarning("[SaveLoad] No se guarda porque ResourceManager.Instance es null.");
                return;
            }

            EnsureSaveDirectory();

            GameSaveData _save = BuildCurrentSaveData();
            string _json = JsonUtility.ToJson(_save, true);

            string _tempPath = _savePath + ".tmp";
            File.WriteAllText(_tempPath, _json);

            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }

            File.Move(_tempPath, _savePath);

            if (_verboseLogs)
            {
                Debug.Log($"[SaveLoad] Partida guardada correctamente ({_json.Length} bytes)");
            }
        }
        catch (Exception _e)
        {
            Debug.LogError($"[SaveLoad] Error guardando: {_e.Message}");
        }
    }

    private GameSaveData BuildCurrentSaveData()
    {
        GameSaveData _save = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            saveDateUtc = DateTime.UtcNow.ToString("o"),
            resources = BuildResourcesSaveData()
        };

        DwellerNPC[] _dwellers = FindObjectsOfType<DwellerNPC>(true);
        for (int i = 0; i < _dwellers.Length; i++)
        {
            if (_dwellers[i] == null)
            {
                continue;
            }

            _save.dwellers.Add(_dwellers[i].GetSaveData());
        }

        WorkStation[] _stations = FindObjectsOfType<WorkStation>(true);
        for (int i = 0; i < _stations.Length; i++)
        {
            if (_stations[i] == null)
            {
                continue;
            }

            _save.workStations.Add(_stations[i].GetSaveData());
        }

        return _save;
    }

    private ResourceManager.ResourceSaveData BuildResourcesSaveData()
    {
        ResourceManager _rm = ResourceManager.Instance;

        return new ResourceManager.ResourceSaveData
        {
            food = _rm.GetResourceAmount(ResourceType.Food),
            water = _rm.GetResourceAmount(ResourceType.Water),
            energy = _rm.GetResourceAmount(ResourceType.Energy),
            materials = _rm.GetResourceAmount(ResourceType.Materials),
            deadNPCCount = _rm.GetDeadNPCCount(),
            gameOverTriggered = _rm.IsGameOverTriggered()
        };
    }

    public void LoadGame()
    {
        if (!SaveExists())
        {
            Debug.LogWarning("[SaveLoad] No hay guardado para cargar.");
            return;
        }

        try
        {
            string _json = File.ReadAllText(_savePath);
            GameSaveData _save = JsonUtility.FromJson<GameSaveData>(_json);

            if (_save == null)
            {
                Debug.LogError("[SaveLoad] El archivo existe pero el JSON no se pudo leer.");
                return;
            }

            _isLoading = true;
            LoadGameInternal(_save);

            if (_verboseLogs)
            {
                Debug.Log("[SaveLoad] Partida cargada correctamente.");
            }
        }
        catch (Exception _e)
        {
            Debug.LogError($"[SaveLoad] Error cargando: {_e.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LoadGameInternal(GameSaveData _save)
    {
        if (_save.resources != null && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.LoadFromSave(_save.resources);
            ResourceManager.Instance.ForceCheckGameOver();
        }

        WorkStation[] _stations = FindObjectsOfType<WorkStation>(true);
        DwellerNPC[] _dwellers = FindObjectsOfType<DwellerNPC>(true);

        // 1) Aplicar datos base de estaciones (posición, id, etc.)
        if (_save.workStations != null)
        {
            for (int i = 0; i < _save.workStations.Count; i++)
            {
                WorkStation.WorkStationSaveData _stationData = _save.workStations[i];
                if (_stationData == null)
                {
                    continue;
                }

                WorkStation _station = FindStationById(_stations, _stationData.stationId);
                if (_station != null)
                {
                    _station.LoadData(_stationData);
                }
            }
        }

        // 2) Limpiar asignaciones actuales para evitar duplicados raros al cargar varias veces.
        for (int i = 0; i < _dwellers.Length; i++)
        {
            if (_dwellers[i] != null)
            {
                _dwellers[i].AssignToWorkStation(null);
            }
        }

        // 3) Aplicar datos de NPCs (posición/needs/muerte). Mapa por nombre para compatibilidad con vuestro save.
        if (_save.dwellers != null)
        {
            for (int i = 0; i < _save.dwellers.Count; i++)
            {
                DwellerNPC.DwellerSaveData _dwellerData = _save.dwellers[i];
                if (_dwellerData == null)
                {
                    continue;
                }

                DwellerNPC _npc = FindDwellerByName(_dwellers, _dwellerData.dwellerName);
                if (_npc != null)
                {
                    _npc.LoadData(_dwellerData);
                }
            }
        }

        // 4) Restaurar asignaciones usando el save de estación (más fiable para grupos de trabajo).
        RestoreAssignments(_save, _dwellers, _stations);

        // 5) Rebalancear y refrescar UI para dejar la escena consistente.
        if (AssignmentManager.Instance != null)
        {
            AssignmentManager.Instance.RefreshCaches();
            AssignmentManager.Instance.RebalanceProductionAssignments();
        }

        TryRefreshUI();
    }

    private void RestoreAssignments(GameSaveData _save, DwellerNPC[] _dwellers, WorkStation[] _stations)
    {
        if (_save.workStations == null)
        {
            return;
        }

        for (int i = 0; i < _save.workStations.Count; i++)
        {
            WorkStation.WorkStationSaveData _stationData = _save.workStations[i];
            if (_stationData == null || _stationData.assignedWorkerNames == null)
            {
                continue;
            }

            WorkStation _station = FindStationById(_stations, _stationData.stationId);
            if (_station == null)
            {
                continue;
            }

            for (int j = 0; j < _stationData.assignedWorkerNames.Count; j++)
            {
                string _workerName = _stationData.assignedWorkerNames[j];
                if (string.IsNullOrWhiteSpace(_workerName))
                {
                    continue;
                }

                DwellerNPC _npc = FindDwellerByName(_dwellers, _workerName);
                if (_npc == null || _npc.IsDead)
                {
                    continue;
                }

                _npc.AssignToWorkStation(_station);
            }
        }
    }

    private DwellerNPC FindDwellerByName(DwellerNPC[] _dwellers, string _dwellerName)
    {
        if (_dwellers == null || string.IsNullOrWhiteSpace(_dwellerName))
        {
            return null;
        }

        for (int i = 0; i < _dwellers.Length; i++)
        {
            if (_dwellers[i] != null && _dwellers[i].dwellerName == _dwellerName)
            {
                return _dwellers[i];
            }
        }

        return null;
    }

    private WorkStation FindStationById(WorkStation[] _stations, string _stationId)
    {
        if (_stations == null || string.IsNullOrWhiteSpace(_stationId))
        {
            return null;
        }

        for (int i = 0; i < _stations.Length; i++)
        {
            if (_stations[i] != null && _stations[i].stationId == _stationId)
            {
                return _stations[i];
            }
        }

        return null;
    }

    public void ResetGame()
    {
        try
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ResetGameOver();
                ResourceManager.Instance.ResetAllResources();
            }

            DwellerNPC[] _dwellers = FindObjectsOfType<DwellerNPC>(true);
            for (int i = 0; i < _dwellers.Length; i++)
            {
                if (_dwellers[i] != null)
                {
                    _dwellers[i].AssignToWorkStation(null);
                }
            }

            if (SaveExists())
            {
                File.Delete(_savePath);
            }

            TryRefreshUI();
            Debug.Log("[SaveLoad] Juego reseteado (estado runtime + archivo de guardado).\n");
        }
        catch (Exception _e)
        {
            Debug.LogError($"[SaveLoad] Error reseteando: {_e.Message}");
        }
    }

    [ContextMenu("Guardar Partida")]
    public void ContextSave() => SaveGame();

    [ContextMenu("Cargar Partida")]
    public void ContextLoad() => LoadGame();

    [ContextMenu("Debug Estado Guardado")]
    public void DebugSaveStatus()
    {
        Debug.Log("=== DEBUG SAVELOAD ===");
        Debug.Log($"Ruta: {_savePath}");
        Debug.Log($"Existe: {SaveExists()}");

        if (SaveExists())
        {
            FileInfo _info = new FileInfo(_savePath);
            Debug.Log($"Tamaño: {_info.Length} bytes");
            Debug.Log($"Última modificación: {_info.LastWriteTime}");
        }

        Debug.Log("======================");
    }

    private void EnsureSaveDirectory()
    {
        if (string.IsNullOrWhiteSpace(_saveDirectory))
        {
            InitializePaths();
            return;
        }

        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
        }
    }

    private void TryRefreshUI()
    {
        UIManager _uiManager = FindObjectOfType<UIManager>(true);
        if (_uiManager != null)
        {
            _uiManager.RefreshAllDisplays();
        }

        NPCStatusPanel _npcPanel = FindObjectOfType<NPCStatusPanel>(true);
        if (_npcPanel != null)
        {
            _npcPanel.UpdateUI();
        }

        ResourceIndicator[] _resourceIndicators = FindObjectsOfType<ResourceIndicator>(true);
        for (int i = 0; i < _resourceIndicators.Length; i++)
        {
            if (_resourceIndicators[i] != null)
            {
                _resourceIndicators[i].EmergencyUpdate();
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (_autoSaveOnQuit)
        {
            SaveGame();
        }
    }
}
