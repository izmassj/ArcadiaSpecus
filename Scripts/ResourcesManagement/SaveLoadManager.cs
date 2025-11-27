// SaveLoadManager.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;
    private string savePath;
    private string tempDirectory;

    /// <summary>
    /// Datos completos del juego para guardar
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        public ResourceManager.ResourceSaveData resources;
        public List<DwellerNPC.DwellerSaveData> dwellers;
        public List<WorkStation.WorkStationSaveData> workStations;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // CONFIGURACIÓN MEJORADA DE DIRECTORIOS
            InitializeDirectories();

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Inicializa los directorios necesarios para guardar archivos
    /// </summary>
    void InitializeDirectories()
    {
        try
        {
            // DIRECTORIO DE GUARDADO PRINCIPAL
            savePath = Path.Combine(Application.persistentDataPath, "Saves", "arcadia_save.json");
            string saveDirectory = Path.GetDirectoryName(savePath);

            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
                Debug.Log($"Directorio de guardado creado: {saveDirectory}");
            }

            // DIRECTORIO TEMPORAL ALTERNATIVO (para evitar problemas de permisos)
            tempDirectory = Path.Combine(Application.persistentDataPath, "Temp");
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
                Debug.Log($"Directorio temporal creado: {tempDirectory}");
            }

            // CONFIGURAR VARIABLES TEMPORALES SI ES NECESARIO
            SetupTemporaryEnvironment();

            Debug.Log($"SaveLoadManager inicializado:");
            Debug.Log($"   - Guardado: {savePath}");
            Debug.Log($"   - Temporal: {tempDirectory}");
            Debug.Log($"   - Persistente: {Application.persistentDataPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error inicializando directorios: {e.Message}");
            // Fallback a ubicaciones más simples
            savePath = Path.Combine(Application.dataPath, "arcadia_save.json");
            Debug.Log($"Usando fallback: {savePath}");
        }
    }

    /// <summary>
    /// Configura el entorno temporal para Unity
    /// </summary>
    void SetupTemporaryEnvironment()
    {
        try
        {
            // Intentar configurar directorio temporal personalizado
            string customTemp = Path.Combine(Application.persistentDataPath, "UnityTemp");
            if (!Directory.Exists(customTemp))
            {
                Directory.CreateDirectory(customTemp);
            }

            // Esto puede ayudar con algunos problemas de Unity
            Environment.SetEnvironmentVariable("UNITY_TEMP_PATH", customTemp);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo configurar entorno temporal: {e.Message}");
        }
    }

    void Start()
    {
        // VERIFICACIÓN INICIAL DE PERMISOS
        CheckFilePermissions();
    }

    /// <summary>
    /// Verifica los permisos de escritura en el directorio
    /// </summary>
    void CheckFilePermissions()
    {
        try
        {
            // Probar permisos de escritura
            string testFile = Path.Combine(Path.GetDirectoryName(savePath), "permission_test.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            Debug.Log("Permisos de archivo: OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error de permisos: {e.Message}");

            // Fallback a DataPath si persistentDataPath falla
            if (!savePath.StartsWith(Application.dataPath))
            {
                savePath = Path.Combine(Application.dataPath, "Saves", "arcadia_save.json");
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                Debug.Log($"Cambiado a directorio de datos: {savePath}");
            }
        }
    }

    /// <summary>
    /// Guarda el estado actual del juego
    /// </summary>
    public void SaveGame()
    {
        try
        {
            if (ResourceManager.Instance == null)
            {
                Debug.LogError("No se puede guardar: ResourceManager no inicializado");
                return;
            }

            // GUARDADO TEMPORAL PRIMERO (patrón atómico)
            string tempSavePath = savePath + ".tmp";

            GameSaveData saveData = new GameSaveData();

            // Guardar recursos
            saveData.resources = new ResourceManager.ResourceSaveData
            {
                food = ResourceManager.Instance.GetResourceAmount(ResourceType.Food),
                water = ResourceManager.Instance.GetResourceAmount(ResourceType.Water),
                energy = ResourceManager.Instance.GetResourceAmount(ResourceType.Energy),
                materials = ResourceManager.Instance.GetResourceAmount(ResourceType.Materials)
            };

            // Guardar NPCs
            saveData.dwellers = new List<DwellerNPC.DwellerSaveData>();
            foreach (var dweller in FindObjectsOfType<DwellerNPC>())
            {
                saveData.dwellers.Add(dweller.GetSaveData());
            }

            // Guardar máquinas
            saveData.workStations = new List<WorkStation.WorkStationSaveData>();
            foreach (var station in FindObjectsOfType<WorkStation>())
            {
                saveData.workStations.Add(station.GetSaveData());
            }

            // GUARDAR EN ARCHIVO TEMPORAL PRIMERO
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(tempSavePath, json);

            // REEMPLAZAR ARCHIVO ORIGINAL (operación atómica)
            if (File.Exists(savePath))
                File.Delete(savePath);

            File.Move(tempSavePath, savePath);

            Debug.Log($"Partida guardada: {savePath} ({json.Length} bytes)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error guardando partida: {e.Message}");
            EmergencySave();
        }
    }

    /// <summary>
    /// Guardado de emergencia cuando falla el guardado normal
    /// </summary>
    void EmergencySave()
    {
        try
        {
            // GUARDADO DE EMERGENCIA EN DATA PATH
            string emergencyPath = Path.Combine(Application.dataPath, "emergency_save.json");
            GameSaveData emergencyData = new GameSaveData();

            // Solo guardar recursos críticos
            emergencyData.resources = new ResourceManager.ResourceSaveData
            {
                food = ResourceManager.Instance.GetResourceAmount(ResourceType.Food),
                water = ResourceManager.Instance.GetResourceAmount(ResourceType.Water),
                energy = ResourceManager.Instance.GetResourceAmount(ResourceType.Energy),
                materials = 0 // No crítico para emergencia
            };

            string json = JsonUtility.ToJson(emergencyData, true);
            File.WriteAllText(emergencyPath, json);
            Debug.Log($"Guardado de emergencia: {emergencyPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error en guardado de emergencia: {ex.Message}");
        }
    }

    /// <summary>
    /// Carga el juego desde un archivo guardado
    /// </summary>
    public void LoadGame()
    {
        if (!SaveExists())
        {
            Debug.LogWarning("No hay partida guardada para cargar");
            return;
        }

        try
        {
            // INTENTAR CARGA PRINCIPAL
            string json = File.ReadAllText(savePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

            // Cargar recursos
            ResourceManager.Instance.LoadFromSave(saveData.resources);

            // Cargar NPCs
            DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
            foreach (var dwellerData in saveData.dwellers)
            {
                DwellerNPC dweller = System.Array.Find(allDwellers, d => d.dwellerName == dwellerData.dwellerName);
                if (dweller != null)
                {
                    dweller.LoadData(dwellerData);
                }
            }

            // Cargar máquinas y restaurar asignaciones
            RestoreAssignments(saveData);

            Debug.Log("Partida cargada exitosamente");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error cargando partida: {e.Message}");
            AttemptEmergencyLoad();
        }
    }

    /// <summary>
    /// Intenta cargar desde el guardado de emergencia
    /// </summary>
    void AttemptEmergencyLoad()
    {
        try
        {
            // INTENTAR CARGA DE EMERGENCIA
            string emergencyPath = Path.Combine(Application.dataPath, "emergency_save.json");
            if (File.Exists(emergencyPath))
            {
                string json = File.ReadAllText(emergencyPath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                ResourceManager.Instance.LoadFromSave(saveData.resources);
                Debug.Log("Carga de emergencia exitosa");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error en carga de emergencia: {ex.Message}");
        }
    }

    /// <summary>
    /// Restaura las asignaciones de NPCs a estaciones de trabajo
    /// </summary>
    void RestoreAssignments(GameSaveData saveData)
    {
        // Restaurar asignaciones NPC → Máquina
        DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
        WorkStation[] allStations = FindObjectsOfType<WorkStation>();

        foreach (var stationData in saveData.workStations)
        {
            WorkStation station = System.Array.Find(allStations, s => s.stationId == stationData.stationId);
            if (station != null)
            {
                foreach (var workerName in stationData.assignedWorkerNames)
                {
                    DwellerNPC dweller = System.Array.Find(allDwellers, d => d.dwellerName == workerName);
                    if (dweller != null)
                    {
                        dweller.AssignToWorkStation(station);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resetea completamente el juego
    /// </summary>
    public void ResetGame()
    {
        try
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ResetAllResources();
            }

            foreach (var dweller in FindObjectsOfType<DwellerNPC>())
            {
                dweller.AssignToWorkStation(null);
            }

            // LIMPIAR ARCHIVOS DE GUARDADO
            if (SaveExists())
            {
                File.Delete(savePath);
            }

            // Limpiar guardado de emergencia
            string emergencyPath = Path.Combine(Application.dataPath, "emergency_save.json");
            if (File.Exists(emergencyPath))
            {
                File.Delete(emergencyPath);
            }

            Debug.Log("Partida reseteada completamente");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reseteando juego: {e.Message}");
        }
    }

    /// <summary>
    /// Verifica si existe un archivo de guardado
    /// </summary>
    public bool SaveExists()
    {
        return File.Exists(savePath);
    }

    /// <summary>
    /// Muestra información de debug sobre el estado del sistema de guardado
    /// </summary>
    [ContextMenu("Verificar Estado Guardado")]
    public void DebugSaveStatus()
    {
        Debug.Log("=== ESTADO DEL SISTEMA DE GUARDADO ===");
        Debug.Log($"Directorio persistente: {Application.persistentDataPath}");
        Debug.Log($"Ruta guardado: {savePath}");
        Debug.Log($"Guardado existe: {SaveExists()}");
        Debug.Log($"Temp directory: {tempDirectory}");

        try
        {
            if (SaveExists())
            {
                FileInfo info = new FileInfo(savePath);
                Debug.Log($"Tamaño archivo: {info.Length} bytes");
                Debug.Log($"Última modificación: {info.LastWriteTime}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error verificando archivo: {e.Message}");
        }
    }

    /// <summary>
    /// Guardado automático al salir de la aplicación
    /// </summary>
    void OnApplicationQuit()
    {
        // GUARDADO AUTOMÁTICO AL SALIR
        if (ResourceManager.Instance != null)
        {
            SaveGame();
            Debug.Log("Guardado automático al salir");
        }
    }
}