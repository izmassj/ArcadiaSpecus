using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Serializable]
    public class ResourceConfig
    {
        public ResourceType type;
        public int currentAmount = 20;
        public int minimumSafeLevel = 5;
        public int warningLevel = 10;
    }

    [Serializable]
    public class ResourceSaveData
    {
        public int food;
        public int water;
        public int energy;
        public int materials;
        public int deadNPCCount;
        public bool gameOverTriggered;
    }

    [Header("Configuración de Recursos")]
    public List<ResourceConfig> resourcesConfig = new List<ResourceConfig>();

    [Header("Consumo Automático Global")]
    [SerializeField] private float _consumptionInterval = 10f;
    [SerializeField] private int _foodConsumptionRate = 5;
    [SerializeField] private int _waterConsumptionRate = 4;
    [SerializeField] private int _energyConsumptionRate = 3;

    [Header("Game Over")]
    [SerializeField] private int _maxAllowedDeaths = 3;
    [SerializeField] private bool _dontDestroyOnLoad = true;

    private readonly Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();
    private readonly HashSet<DwellerNPC> _subscribedDwellers = new HashSet<DwellerNPC>();
    private readonly List<DwellerNPC> _deadDwellers = new List<DwellerNPC>();

    private Coroutine _autoConsumptionCoroutine;
    private bool _gameOverTriggered;

    public static event Action<ResourceType, int> OnResourceChanged;
    public static event Action<ResourceType> OnResourceCritical;
    public static event Action<ResourceType> OnResourceSafe;
    public static event Action<DwellerNPC> OnNPCDied;
    public static event Action OnGameOver;

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

        InitializeResourcesIfNeeded();
    }

    private void OnEnable()
    {
        // Reenlazar NPCs por si la escena cambia o si se crean nuevos.
        RefreshDwellerSubscriptions();

        if (_autoConsumptionCoroutine == null)
        {
            _autoConsumptionCoroutine = StartCoroutine(AutoConsumptionLoop());
        }
    }

    private void Start()
    {
        // Segunda pasada al iniciar (más seguro para objetos instanciados en Start de otros scripts).
        RefreshDwellerSubscriptions();
        InvokeRepeating(nameof(RefreshDwellerSubscriptions), 3f, 3f);
    }

    private void OnDisable()
    {
        if (_autoConsumptionCoroutine != null)
        {
            StopCoroutine(_autoConsumptionCoroutine);
            _autoConsumptionCoroutine = null;
        }

        CancelInvoke(nameof(RefreshDwellerSubscriptions));
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeAllDwellers();
    }

    private void InitializeResourcesIfNeeded()
    {
        _resources.Clear();

        // Si no hay configuración, crear una base mínima para no romper la escena.
        EnsureDefaultConfigs();

        for (int i = 0; i < resourcesConfig.Count; i++)
        {
            ResourceConfig _config = resourcesConfig[i];
            _resources[_config.type] = Mathf.Max(0, _config.currentAmount);
        }

        BroadcastAllResources();
        CheckGameOverCondition();
    }

    private void EnsureDefaultConfigs()
    {
        if (resourcesConfig == null)
        {
            resourcesConfig = new List<ResourceConfig>();
        }

        EnsureConfigExists(ResourceType.Food, 20, 5, 10);
        EnsureConfigExists(ResourceType.Water, 20, 5, 10);
        EnsureConfigExists(ResourceType.Energy, 20, 5, 10);
        EnsureConfigExists(ResourceType.Materials, 10, 0, 5);
    }

    private void EnsureConfigExists(ResourceType _type, int _start, int _min, int _warn)
    {
        for (int i = 0; i < resourcesConfig.Count; i++)
        {
            if (resourcesConfig[i] != null && resourcesConfig[i].type == _type)
            {
                return;
            }
        }

        resourcesConfig.Add(new ResourceConfig
        {
            type = _type,
            currentAmount = _start,
            minimumSafeLevel = _min,
            warningLevel = _warn
        });
    }

    private IEnumerator AutoConsumptionLoop()
    {
        while (true)
        {
            float _wait = Mathf.Max(0.1f, _consumptionInterval);
            yield return new WaitForSeconds(_wait);

            if (_gameOverTriggered)
            {
                continue;
            }

            // Consumo automático del bunker (comentario de audio: aquí podría sonar una alarma suave de ciclo).
            ConsumeResource(ResourceType.Food, Mathf.Max(0, _foodConsumptionRate));
            ConsumeResource(ResourceType.Water, Mathf.Max(0, _waterConsumptionRate));
            ConsumeResource(ResourceType.Energy, Mathf.Max(0, _energyConsumptionRate));

            CheckGameOverCondition();
        }
    }

    private void RefreshDwellerSubscriptions()
    {
        DwellerNPC[] _allDwellers = FindObjectsOfType<DwellerNPC>(true);

        for (int i = 0; i < _allDwellers.Length; i++)
        {
            SubscribeToDweller(_allDwellers[i]);
        }
    }

    private void SubscribeToDweller(DwellerNPC _dweller)
    {
        if (_dweller == null)
        {
            return;
        }

        if (_subscribedDwellers.Contains(_dweller))
        {
            return;
        }

        _dweller.OnDeath -= HandleNPCDied;
        _dweller.OnDeath += HandleNPCDied;
        _subscribedDwellers.Add(_dweller);
    }

    private void UnsubscribeAllDwellers()
    {
        foreach (DwellerNPC _dweller in _subscribedDwellers)
        {
            if (_dweller == null)
            {
                continue;
            }

            _dweller.OnDeath -= HandleNPCDied;
        }

        _subscribedDwellers.Clear();
    }

    private void HandleNPCDied(DwellerNPC _deadNPC)
    {
        if (_deadNPC == null)
        {
            return;
        }

        if (_deadDwellers.Contains(_deadNPC))
        {
            return;
        }

        _deadDwellers.Add(_deadNPC);
        OnNPCDied?.Invoke(_deadNPC);

        CheckGameOverCondition();
    }

    public void AddResource(ResourceType _type, int _amount)
    {
        if (_gameOverTriggered)
        {
            return;
        }

        if (_amount <= 0)
        {
            return;
        }

        int _oldValue = GetResourceAmount(_type);
        int _newValue = _oldValue + _amount;
        _resources[_type] = _newValue;

        OnResourceChanged?.Invoke(_type, _newValue);

        int _min = GetMinimumLevel(_type);
        if (_oldValue <= _min && _newValue > _min)
        {
            OnResourceSafe?.Invoke(_type);
        }
    }

    public bool ConsumeResource(ResourceType _type, int _amount)
    {
        if (_gameOverTriggered)
        {
            return false;
        }

        if (_amount <= 0)
        {
            return true;
        }

        int _current = GetResourceAmount(_type);
        if (_current < _amount)
        {
            return false;
        }

        int _oldValue = _current;
        int _newValue = _current - _amount;
        _resources[_type] = _newValue;

        OnResourceChanged?.Invoke(_type, _newValue);
        CheckResourceThresholdTransitions(_type, _oldValue, _newValue);
        return true;
    }

    private void CheckResourceThresholdTransitions(ResourceType _type, int _oldValue, int _newValue)
    {
        int _min = GetMinimumLevel(_type);

        if (_oldValue > _min && _newValue <= _min)
        {
            OnResourceCritical?.Invoke(_type);
        }
        else if (_oldValue <= _min && _newValue > _min)
        {
            OnResourceSafe?.Invoke(_type);
        }

        CheckGameOverCondition();
    }

    private void BroadcastAllResources()
    {
        foreach (KeyValuePair<ResourceType, int> _pair in _resources)
        {
            OnResourceChanged?.Invoke(_pair.Key, _pair.Value);
        }
    }

    private void CheckGameOverCondition()
    {
        if (_gameOverTriggered)
        {
            return;
        }

        // Condición 1: demasiados NPCs muertos.
        if (_deadDwellers.Count >= _maxAllowedDeaths)
        {
            TriggerGameOverInternal("Demasiados habitantes han fallecido");
            return;
        }

        // Condición 2: todos los recursos configurados críticos.
        if (_resources.Count > 0)
        {
            int _criticalCount = 0;
            foreach (KeyValuePair<ResourceType, int> _pair in _resources)
            {
                if (_pair.Value <= GetMinimumLevel(_pair.Key))
                {
                    _criticalCount++;
                }
            }

            if (_criticalCount >= _resources.Count)
            {
                TriggerGameOverInternal("Todos los recursos están en nivel crítico");
                return;
            }
        }

        // Condición 3: no queda ningún NPC vivo en escena.
        DwellerNPC[] _allDwellers = FindObjectsOfType<DwellerNPC>(true);
        if (_allDwellers.Length > 0)
        {
            int _aliveCount = 0;
            for (int i = 0; i < _allDwellers.Length; i++)
            {
                if (_allDwellers[i] != null && !_allDwellers[i].IsDead)
                {
                    _aliveCount++;
                }
            }

            if (_aliveCount == 0)
            {
                TriggerGameOverInternal("Todos los habitantes han fallecido");
            }
        }
    }

    public void TriggerGameOver()
    {
        TriggerGameOverInternal("Activado manualmente");
    }

    private void TriggerGameOverInternal(string _reason)
    {
        if (_gameOverTriggered)
        {
            return;
        }

        _gameOverTriggered = true;
        Debug.LogError($"GAME OVER: {_reason}");

        // Comentario audio: aquí iría música de derrota / stinger de game over.
        OnGameOver?.Invoke();
    }

    public void ResetGameOver()
    {
        _gameOverTriggered = false;
        _deadDwellers.Clear();
        Time.timeScale = 1f;

        // Reforzar suscripción por si se han recreado NPCs al recargar escena.
        UnsubscribeAllDwellers();
        RefreshDwellerSubscriptions();
    }

    public int GetDeadNPCCount()
    {
        // Mantiene el conteo registrado por eventos (compatible con UI/GameOverManager).
        return _deadDwellers.Count;
    }

    public int GetMaxAllowedDeaths()
    {
        return _maxAllowedDeaths;
    }

    public bool IsGameOverTriggered()
    {
        return _gameOverTriggered;
    }

    public void ForceCheckGameOver()
    {
        CheckGameOverCondition();
    }

    public void DebugDeathStatus()
    {
        Debug.Log("=== DEBUG MUERTES NPC ===");
        Debug.Log($"Muertes registradas: {_deadDwellers.Count}/{_maxAllowedDeaths}");
        Debug.Log($"GameOver: {_gameOverTriggered}");
        Debug.Log("========================");
    }

    public void ResetAllResources()
    {
        EnsureDefaultConfigs();

        for (int i = 0; i < resourcesConfig.Count; i++)
        {
            ResourceConfig _config = resourcesConfig[i];
            _resources[_config.type] = Mathf.Max(0, _config.currentAmount);
        }

        BroadcastAllResources();

        // Revisar estados críticos/seguros tras reset.
        foreach (KeyValuePair<ResourceType, int> _pair in _resources)
        {
            if (_pair.Value <= GetMinimumLevel(_pair.Key))
            {
                OnResourceCritical?.Invoke(_pair.Key);
            }
            else
            {
                OnResourceSafe?.Invoke(_pair.Key);
            }
        }
    }

    public ResourceConfig GetResourceConfig(ResourceType _type)
    {
        for (int i = 0; i < resourcesConfig.Count; i++)
        {
            if (resourcesConfig[i] != null && resourcesConfig[i].type == _type)
            {
                return resourcesConfig[i];
            }
        }

        return null;
    }

    public int GetResourceAmount(ResourceType _type)
    {
        if (_resources.TryGetValue(_type, out int _value))
        {
            return _value;
        }

        return 0;
    }

    public bool HasEnoughResources(ResourceType _type, int _amount)
    {
        return GetResourceAmount(_type) >= _amount;
    }

    public int GetMinimumLevel(ResourceType _type)
    {
        ResourceConfig _config = GetResourceConfig(_type);
        return _config != null ? Mathf.Max(0, _config.minimumSafeLevel) : 0;
    }

    public int GetWarningLevel(ResourceType _type)
    {
        ResourceConfig _config = GetResourceConfig(_type);
        return _config != null ? Mathf.Max(0, _config.warningLevel) : GetMinimumLevel(_type);
    }

    public void DebugAddResources()
    {
        AddResource(ResourceType.Food, 50);
        AddResource(ResourceType.Water, 50);
        AddResource(ResourceType.Energy, 50);
        AddResource(ResourceType.Materials, 50);
    }

    public void LoadFromSave(ResourceSaveData _saveData)
    {
        if (_saveData == null)
        {
            return;
        }

        _resources[ResourceType.Food] = Mathf.Max(0, _saveData.food);
        _resources[ResourceType.Water] = Mathf.Max(0, _saveData.water);
        _resources[ResourceType.Energy] = Mathf.Max(0, _saveData.energy);
        _resources[ResourceType.Materials] = Mathf.Max(0, _saveData.materials);

        _gameOverTriggered = _saveData.gameOverTriggered;

        // El conteo exacto de muertos se vuelve a reconstruir por eventos/escena.
        _deadDwellers.Clear();
        RefreshDwellerSubscriptions();

        BroadcastAllResources();

        if (_gameOverTriggered)
        {
            // Mantener compatibilidad con GameOverManager si se carga una partida ya perdida.
            OnGameOver?.Invoke();
        }
        else
        {
            CheckGameOverCondition();
        }
    }
}
