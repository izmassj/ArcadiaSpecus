using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class AnalyticsResourceAmount
{
    public string resourceType;
    public int amount;
}

[Serializable]
public class AnalyticsMachineAmount
{
    public string machineType;
    public int amount;
}

[Serializable]
public class AnalyticsEventData
{
    public string eventName;
    public string sceneName;
    public float timeSinceSessionStart;
    public string extraData;
}

[Serializable]
public class AnalyticsLevelRun
{
    public string sceneName;
    public string startedAtUtc;
    public string endedAtUtc;
    public float durationSeconds;
    public int deaths;
    public bool completed;
    public int rewardScrap;
    public int rewardElectricity;
    public int rewardWater;
    public int rewardFood;
    public int rewardJoinedInhabitants;
}

[Serializable]
public class GameSessionAnalyticsData
{
    public string sessionId;
    public string slotId;
    public string gameVersion;
    public string startedAtUtc;
    public string endedAtUtc;
    public string startSceneName;
    public string endSceneName;

    public float durationSeconds;
    public int bunkerDaysEnded;
    public int levelsStarted;
    public int levelsCompleted;
    public int playerDeaths;
    public int gameOvers;
    public int machineCollections;
    public int joinedInhabitantsGained;

    public List<AnalyticsResourceAmount> resourcesCollected = new List<AnalyticsResourceAmount>();
    public List<AnalyticsMachineAmount> machinesCollectedByType = new List<AnalyticsMachineAmount>();
    public List<AnalyticsLevelRun> levelRuns = new List<AnalyticsLevelRun>();
    public List<AnalyticsEventData> events = new List<AnalyticsEventData>();
}

public class GameAnalyticsManager : MonoBehaviour
{
    public static GameAnalyticsManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private GameSessionAnalyticsData _currentData;
    [SerializeField] private string _lastSavedJsonPath;

    private float _sessionStartRealtime;
    private bool _sessionFinished;
    private AnalyticsLevelRun _currentLevelRun;

    public GameSessionAnalyticsData CurrentData => _currentData;
    public string LastSavedJsonPath => _lastSavedJsonPath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartNewSession();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        FinishSessionAndSaveJson();
    }

    public void StartNewSession()
    {
        _sessionFinished = false;
        _sessionStartRealtime = Time.realtimeSinceStartup;

        _currentData = new GameSessionAnalyticsData();
        _currentData.sessionId = Guid.NewGuid().ToString("N");
        _currentData.slotId = BunkerSessionLaunch.CurrentSlotId;
        _currentData.gameVersion = Application.version;
        _currentData.startedAtUtc = DateTime.UtcNow.ToString("o");
        _currentData.startSceneName = SceneManager.GetActiveScene().name;
        _currentData.endSceneName = _currentData.startSceneName;

        RegisterEvent("session_started", _currentData.startSceneName, "");
    }

    public string FinishSessionAndSaveJson()
    {
        if (_currentData == null)
            return "";

        if (_sessionFinished)
            return _lastSavedJsonPath;

        _sessionFinished = true;

        CloseCurrentLevelRun(false, 0, 0, 0, 0, 0);

        _currentData.endedAtUtc = DateTime.UtcNow.ToString("o");
        _currentData.endSceneName = SceneManager.GetActiveScene().name;
        _currentData.durationSeconds = Time.realtimeSinceStartup - _sessionStartRealtime;

        RegisterEvent("session_finished", _currentData.endSceneName, "");

        string folder = Path.Combine(Application.persistentDataPath, "AnalyticsSessions");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fileName = "session_" + _currentData.sessionId + ".json";
        _lastSavedJsonPath = Path.Combine(folder, fileName);

        string json = JsonUtility.ToJson(_currentData, true);
        File.WriteAllText(_lastSavedJsonPath, json);

        if (MongoAnalyticsUploader.Instance != null)
            _ = MongoAnalyticsUploader.Instance.UploadSessionAsync(_currentData);

        Debug.Log("Analytics JSON saved: " + _lastSavedJsonPath);
        return _lastSavedJsonPath;
    }

    [ContextMenu("Finish Session And Save JSON")]
    private void FinishSessionAndSaveJsonFromInspector()
    {
        FinishSessionAndSaveJson();
    }

    public void RegisterBunkerDayEnded(int dayNumber)
    {
        if (_currentData == null)
            return;

        _currentData.bunkerDaysEnded++;
        RegisterEvent("bunker_day_ended", SceneManager.GetActiveScene().name, "day=" + dayNumber);
    }

    public void RegisterLevelStarted(string levelSceneName)
    {
        if (_currentData == null)
            return;

        CloseCurrentLevelRun(false, 0, 0, 0, 0, 0);

        _currentData.levelsStarted++;

        _currentLevelRun = new AnalyticsLevelRun();
        _currentLevelRun.sceneName = levelSceneName;
        _currentLevelRun.startedAtUtc = DateTime.UtcNow.ToString("o");

        _currentData.levelRuns.Add(_currentLevelRun);

        RegisterEvent("level_started", levelSceneName, "");
    }

    public void RegisterLevelCompleted(string sceneName, int scrap, int electricity, int water, int food, int joinedInhabitants)
    {
        if (_currentData == null)
            return;

        _currentData.levelsCompleted++;
        _currentData.joinedInhabitantsGained += Mathf.Max(0, joinedInhabitants);

        CloseCurrentLevelRun(true, scrap, electricity, water, food, joinedInhabitants);

        RegisterEvent("level_completed", sceneName, "scrap=" + scrap + ";electricity=" + electricity + ";water=" + water + ";food=" + food + ";joined=" + joinedInhabitants);
    }

    public void RegisterPlayerDeath(string sceneName)
    {
        if (_currentData == null)
            return;

        _currentData.playerDeaths++;

        if (_currentLevelRun != null)
            _currentLevelRun.deaths++;

        RegisterEvent("player_death", sceneName, "");
    }

    public void RegisterGameOver(string sceneName)
    {
        if (_currentData == null)
            return;

        _currentData.gameOvers++;
        RegisterEvent("game_over", sceneName, "");
    }

    public void RegisterResourceCollected(string resourceType, int amount)
    {
        if (_currentData == null || amount <= 0)
            return;

        AnalyticsResourceAmount entry = GetResourceEntry(resourceType);
        entry.amount += amount;

        RegisterEvent("resource_collected", SceneManager.GetActiveScene().name, resourceType + "=" + amount);
    }

    public void RegisterMachineCollected(string machineType)
    {
        if (_currentData == null)
            return;

        _currentData.machineCollections++;

        AnalyticsMachineAmount entry = GetMachineEntry(machineType);
        entry.amount++;

        RegisterEvent("machine_collected", SceneManager.GetActiveScene().name, machineType);
    }

    public void RegisterEvent(string eventName, string sceneName, string extraData)
    {
        if (_currentData == null)
            return;

        AnalyticsEventData eventData = new AnalyticsEventData();
        eventData.eventName = eventName;
        eventData.sceneName = sceneName;
        eventData.timeSinceSessionStart = Time.realtimeSinceStartup - _sessionStartRealtime;
        eventData.extraData = extraData;

        _currentData.events.Add(eventData);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_currentData != null)
            _currentData.endSceneName = scene.name;
    }

    private void CloseCurrentLevelRun(bool completed, int scrap, int electricity, int water, int food, int joinedInhabitants)
    {
        if (_currentLevelRun == null)
            return;

        if (!string.IsNullOrWhiteSpace(_currentLevelRun.endedAtUtc))
            return;

        _currentLevelRun.endedAtUtc = DateTime.UtcNow.ToString("o");
        _currentLevelRun.durationSeconds = Time.realtimeSinceStartup - _sessionStartRealtime - GetLevelStartOffset(_currentLevelRun);
        _currentLevelRun.completed = completed;
        _currentLevelRun.rewardScrap = scrap;
        _currentLevelRun.rewardElectricity = electricity;
        _currentLevelRun.rewardWater = water;
        _currentLevelRun.rewardFood = food;
        _currentLevelRun.rewardJoinedInhabitants = joinedInhabitants;

        _currentLevelRun = null;
    }

    private float GetLevelStartOffset(AnalyticsLevelRun levelRun)
    {
        for (int i = _currentData.events.Count - 1; i >= 0; i--)
        {
            AnalyticsEventData eventData = _currentData.events[i];

            if (eventData.eventName == "level_started" && eventData.sceneName == levelRun.sceneName)
                return eventData.timeSinceSessionStart;
        }

        return 0f;
    }

    private AnalyticsResourceAmount GetResourceEntry(string resourceType)
    {
        for (int i = 0; i < _currentData.resourcesCollected.Count; i++)
        {
            if (_currentData.resourcesCollected[i].resourceType == resourceType)
                return _currentData.resourcesCollected[i];
        }

        AnalyticsResourceAmount entry = new AnalyticsResourceAmount();
        entry.resourceType = resourceType;
        entry.amount = 0;
        _currentData.resourcesCollected.Add(entry);
        return entry;
    }

    private AnalyticsMachineAmount GetMachineEntry(string machineType)
    {
        for (int i = 0; i < _currentData.machinesCollectedByType.Count; i++)
        {
            if (_currentData.machinesCollectedByType[i].machineType == machineType)
                return _currentData.machinesCollectedByType[i];
        }

        AnalyticsMachineAmount entry = new AnalyticsMachineAmount();
        entry.machineType = machineType;
        entry.amount = 0;
        _currentData.machinesCollectedByType.Add(entry);
        return entry;
    }
}