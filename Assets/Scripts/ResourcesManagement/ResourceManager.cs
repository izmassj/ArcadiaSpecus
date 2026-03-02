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
        // Legacy (1.0)
        public int food;
        public int water;
        public int energy;
        public int materials;

        // 1.1+
        public int oxygen;
        public int medicine;
        public int rations;
        public int fuel;
        public int metal;
        public int electronics;

        public int deadNPCCount;
        public bool gameOverTriggered;
    }

    [Header("Configuración de Recursos")]
    public List<ResourceConfig> resourcesConfig = new List<ResourceConfig>();

    [Header("Consumo Automático Global")]
    [SerializeField] private float _consumptionInterval = 10f;

    [SerializeField] private int _foodConsumptionRate = 5;
    [SerializeField] private int _waterConsumptionRate = 4;

    [Tooltip("Consumo base de energía (vida en el bunker)")]
    [SerializeField] private int _energyConsumptionRate = 3;

    [Tooltip("Consumo base de oxígeno (vida en el bunker)")]
    [SerializeField] private int _oxygenConsumptionRate = 2;

    [Header("Power / Combustible")]
    [Tooltip("1 unidad de Fuel se convierte en X de Energy cuando falta energía.")]
    [SerializeField] private int _fuelToEnergyRatio = 5;

    [Header("Dinámicas (Bé rúbrica)")]
    [Tooltip("Multiplicador que acelera el deterioro de necesidades cuando faltan recursos.")]
    [SerializeField, Range(0.5f, 3f)] private float _needsDegradationMultiplier = 1f;

    [Tooltip("Si falta energía, algunas estaciones/máquinas se desactivan completamente (requierenPower=true).")]
    [SerializeField] private bool _powerOutageDisablesStations = true;

    [Header("Game Over")]
    [SerializeField] private int _maxAllowedDeaths = 3;
    [SerializeField] private bool _dontDestroyOnLoad = true;

    private readonly Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();
    private readonly HashSet<DwellerNPC> _subscribedDwellers = new HashSet<DwellerNPC>();
    private readonly List<DwellerNPC> _deadDwellers = new List<DwellerNPC>();

    private Coroutine _autoConsumptionCoroutine;
    private bool _gameOverTriggered;

    [Header("Producción Global")]
    [SerializeField, Range(0.1f, 5f)] private float _globalProductionMultiplier = 1f;

    private bool _powerOnlineCached = true;

    /// <summary>Evento cuando cambia el estado de energía (apagón / vuelve la luz).</summary>
    public static event Action<bool> OnPowerStateChanged;

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
        RefreshDwellerSubscriptions();

        if (_autoConsumptionCoroutine == null)
        {
            _autoConsumptionCoroutine = StartCoroutine(AutoConsumptionLoop());
        }
    }

    private void Start()
    {
        RefreshDwellerSubscriptions();
        InvokeRepeating(nameof(RefreshDwellerSubscriptions), 3f, 3f);

        // Inicializar estados derivados (multiplicadores / power).
        UpdateGlobalDynamics();
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

    /// <summary>
    /// Multiplicador global aplicado a estaciones de producción (por ejemplo, por combos de habitaciones).
    /// </summary>
    public float GetGlobalProductionMultiplier()
    {
        return _globalProductionMultiplier;
    }

    /// <summary>
    /// Cambia el multiplicador global de producción.
    /// Nota: no se guarda en Save (se recalcula desde combos al cargar/colocar).
    /// </summary>
    public void SetGlobalProductionMultiplier(float multiplier)
    {
        _globalProductionMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
    }

    /// <summary>
    /// Multiplicador que se aplica a la degradación de necesidades (hambre/sed/fatiga).
    /// NPCNeeds lo consulta cada frame.
    /// </summary>
    public float GetNeedsDegradationMultiplier()
    {
        return Mathf.Clamp(_needsDegradationMultiplier, 0.5f, 3f);
    }

    /// <summary>
    /// Consideramos "luz" si Energy está por encima del mínimo seguro.
    /// Esto se usa para desactivar por completo estaciones/máquinas si falta electricidad (rúbrica Bé).
    /// </summary>
    public bool IsPowerOnline()
    {
        int energy = GetResourceAmount(ResourceType.Energy);
        int min = GetMinimumLevel(ResourceType.Energy);
        return energy > min;
    }

    public bool ShouldPowerOutageDisableStations()
    {
        return _powerOutageDisablesStations;
    }

    private void InitializeResourcesIfNeeded()
    {
        _resources.Clear();

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

        // Categoría: Sustenance (3 recursos)
        EnsureConfigExists(ResourceType.Food, 20, 5, 10);
        EnsureConfigExists(ResourceType.Water, 20, 5, 10);
        EnsureConfigExists(ResourceType.Rations, 10, 2, 6);

        // Categoría: Power (3 recursos)
        EnsureConfigExists(ResourceType.Energy, 20, 5, 10);
        EnsureConfigExists(ResourceType.Oxygen, 20, 5, 10);
        EnsureConfigExists(ResourceType.Fuel, 6, 0, 3);

        // Categoría: Construction (3 recursos)
        EnsureConfigExists(ResourceType.Materials, 10, 0, 5);
        EnsureConfigExists(ResourceType.Metal, 5, 0, 3);
        EnsureConfigExists(ResourceType.Electronics, 3, 0, 2);

        // Extra (no obligatorio por rúbrica, pero ya existe en el enum)
        EnsureConfigExists(ResourceType.Medicine, 3, 0, 1);
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

            // 1) Sustenance: comida o raciones, y agua
            ConsumeFoodOrRations(Mathf.Max(0, _foodConsumptionRate));
            ConsumeResource(ResourceType.Water, Mathf.Max(0, _waterConsumptionRate));

            // 2) Power: oxígeno y energía (con Fuel como respaldo)
            int oxygenRate = Mathf.Max(0, _oxygenConsumptionRate);
            if (!IsPowerOnline())
            {
                // Sin luz, el soporte vital va peor: consume más oxígeno.
                oxygenRate = Mathf.RoundToInt(oxygenRate * 1.5f);
            }
            ConsumeResource(ResourceType.Oxygen, oxygenRate);

            ConsumeEnergyWithFuelBackup(Mathf.Max(0, _energyConsumptionRate));

            // 3) Dinámicas relacionadas (Bé): afecta rendimiento y necesidades
            UpdateGlobalDynamics();

            CheckGameOverCondition();
        }
    }

    /// <summary>
    /// Intenta consumir Food; si no hay suficiente, usa Rations como alternativa.
    /// Esto permite tener >2 recursos en la categoría y que se relacionen.
    /// </summary>
    public bool ConsumeFoodOrRations(int _amount)
    {
        if (_amount <= 0) return true;

        if (ConsumeResource(ResourceType.Food, _amount))
            return true;

        return ConsumeResource(ResourceType.Rations, _amount);
    }

    private void ConsumeEnergyWithFuelBackup(int _amount)
    {
        if (_amount <= 0) return;

        // Si no hay energía suficiente, intentamos convertir Fuel a Energy.
        int currentEnergy = GetResourceAmount(ResourceType.Energy);
        if (currentEnergy < _amount)
        {
            int deficit = _amount - currentEnergy;
            TryConvertFuelToEnergy(deficit);
        }

        // Consumimos la energía si ahora alcanza
        ConsumeResource(ResourceType.Energy, _amount);
    }

    /// <summary>
    /// Convierte Fuel en Energy para cubrir un déficit.
    /// No crea energía gratis: consume Fuel.
    /// </summary>
    private void TryConvertFuelToEnergy(int deficitEnergy)
    {
        int ratio = Mathf.Max(1, _fuelToEnergyRatio);
        int fuelAvailable = GetResourceAmount(ResourceType.Fuel);
        if (fuelAvailable <= 0) return;

        // fuelNeeded = ceil(deficit/ratio)
        int fuelNeeded = Mathf.CeilToInt(deficitEnergy / (float)ratio);
        fuelNeeded = Mathf.Clamp(fuelNeeded, 0, fuelAvailable);

        if (fuelNeeded <= 0) return;

        bool consumedFuel = ConsumeResource(ResourceType.Fuel, fuelNeeded);
        if (!consumedFuel) return;

        int gainedEnergy = fuelNeeded * ratio;
        AddResource(ResourceType.Energy, gainedEnergy);
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

        // Al subir recursos, puede cambiar estado de power / dinámicas.
        UpdateGlobalDynamics();
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

        // Al bajar recursos, puede cambiar estado de power / dinámicas.
        UpdateGlobalDynamics();

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

    private void UpdateGlobalDynamics()
    {
        // 1) Performance: si falta comida/agua baja rendimiento global (ej. producción).
        int foodTotal = GetResourceAmount(ResourceType.Food) + GetResourceAmount(ResourceType.Rations);
        int water = GetResourceAmount(ResourceType.Water);

        int foodWarn = GetWarningLevel(ResourceType.Food);
        int foodMin = GetMinimumLevel(ResourceType.Food);
        int waterWarn = GetWarningLevel(ResourceType.Water);
        int waterMin = GetMinimumLevel(ResourceType.Water);

        float targetProdMult = 1f;
        float targetNeedsMult = 1f;

        if (foodTotal <= foodWarn || water <= waterWarn)
        {
            targetProdMult *= 0.85f;
            targetNeedsMult *= 1.15f;
        }

        if (foodTotal <= foodMin || water <= waterMin)
        {
            targetProdMult *= 0.7f;
            targetNeedsMult *= 1.35f;
        }

        // 2) Power: si falta energía, apagón => estaciones/piezas que requierenPower se paran.
        bool powerOnline = IsPowerOnline();
        if (!powerOnline)
        {
            // Sin luz, las necesidades empeoran un poco (ambiente hostil).
            targetNeedsMult *= 1.10f;
        }

        // 3) Oxygen: si falta oxígeno, penalización fuerte (supervivencia).
        int oxygen = GetResourceAmount(ResourceType.Oxygen);
        int oxygenMin = GetMinimumLevel(ResourceType.Oxygen);
        int oxygenWarn = GetWarningLevel(ResourceType.Oxygen);

        if (oxygen <= oxygenWarn)
            targetNeedsMult *= 1.15f;

        if (oxygen <= oxygenMin)
            targetNeedsMult *= 1.25f;

        // Aplicar (sin spikes)
        _globalProductionMultiplier = Mathf.Clamp(targetProdMult, 0.1f, 5f);
        _needsDegradationMultiplier = Mathf.Clamp(targetNeedsMult, 0.5f, 3f);

        // Notificar cambio de power
        if (powerOnline != _powerOnlineCached)
        {
            _powerOnlineCached = powerOnline;
            OnPowerStateChanged?.Invoke(powerOnline);
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

        OnGameOver?.Invoke();
    }

    public void ResetGameOver()
    {
        _gameOverTriggered = false;
        _deadDwellers.Clear();
        Time.timeScale = 1f;

        UnsubscribeAllDwellers();
        RefreshDwellerSubscriptions();

        UpdateGlobalDynamics();
    }

    public int GetDeadNPCCount()
    {
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

    public void ResetAllResources()
    {
        EnsureDefaultConfigs();

        for (int i = 0; i < resourcesConfig.Count; i++)
        {
            ResourceConfig _config = resourcesConfig[i];
            _resources[_config.type] = Mathf.Max(0, _config.currentAmount);
        }

        BroadcastAllResources();

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

        UpdateGlobalDynamics();
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

        // Si alguien consulta un recurso nuevo no inicializado, lo creamos a 0 para no explotar.
        _resources[_type] = 0;
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
        AddResource(ResourceType.Rations, 25);

        AddResource(ResourceType.Energy, 50);
        AddResource(ResourceType.Oxygen, 50);
        AddResource(ResourceType.Fuel, 10);

        AddResource(ResourceType.Materials, 50);
        AddResource(ResourceType.Metal, 20);
        AddResource(ResourceType.Electronics, 10);
    }

    public void LoadFromSave(ResourceSaveData _saveData, string saveVersion = "1.0")
    {
        if (_saveData == null)
        {
            return;
        }

        // Asegurar que tenemos configs/valores para TODOS los recursos antes de aplicar load.
        EnsureDefaultConfigs();
        for (int i = 0; i < resourcesConfig.Count; i++)
        {
            ResourceConfig _config = resourcesConfig[i];
            if (!_resources.ContainsKey(_config.type))
                _resources[_config.type] = Mathf.Max(0, _config.currentAmount);
        }

        // Siempre cargamos legacy.
        _resources[ResourceType.Food] = Mathf.Max(0, _saveData.food);
        _resources[ResourceType.Water] = Mathf.Max(0, _saveData.water);
        _resources[ResourceType.Energy] = Mathf.Max(0, _saveData.energy);
        _resources[ResourceType.Materials] = Mathf.Max(0, _saveData.materials);

        // 1.1+ (si el save es antiguo, NO pisamos estos a 0)
        bool isLegacy = string.IsNullOrWhiteSpace(saveVersion) || saveVersion.StartsWith("1.0");

        if (!isLegacy)
        {
            _resources[ResourceType.Oxygen] = Mathf.Max(0, _saveData.oxygen);
            _resources[ResourceType.Medicine] = Mathf.Max(0, _saveData.medicine);
            _resources[ResourceType.Rations] = Mathf.Max(0, _saveData.rations);
            _resources[ResourceType.Fuel] = Mathf.Max(0, _saveData.fuel);
            _resources[ResourceType.Metal] = Mathf.Max(0, _saveData.metal);
            _resources[ResourceType.Electronics] = Mathf.Max(0, _saveData.electronics);
        }

        _gameOverTriggered = _saveData.gameOverTriggered;

        _deadDwellers.Clear();
        RefreshDwellerSubscriptions();

        BroadcastAllResources();
        UpdateGlobalDynamics();

        if (_gameOverTriggered)
        {
            OnGameOver?.Invoke();
        }
        else
        {
            CheckGameOverCondition();
        }
    }

    // Compatibilidad con llamadas antiguas (SaveLoadManager viejo)
    public void LoadFromSave(ResourceSaveData _saveData)
    {
        LoadFromSave(_saveData, "1.0");
    }
}
