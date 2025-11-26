// ResourceManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [System.Serializable]
    public class ResourceConfig
    {
        public ResourceType type;
        public int currentAmount;
        public int minimumSafeLevel;
        public int warningLevel;
    }

    [Header("Configuración de Recursos")]
    public List<ResourceConfig> resourcesConfig = new List<ResourceConfig>();

    // Eventos estáticos para notificar cambios en los recursos
    public static event Action<ResourceType> OnResourceCritical;
    public static event Action<ResourceType> OnResourceSafe;
    public static event Action OnGameOver;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    public static event Action<ResourceType, int> OnResourceChanged;

    [Header("Consumo Automático")]
    [SerializeField] private float consumptionInterval = 10f;
    [SerializeField] private int foodConsumptionRate = 5;
    [SerializeField] private int energyConsumptionRate = 3;
    [SerializeField] private int waterConsumptionRate = 4;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(AutoConsumptionLoop());
    }

    /// <summary>
    /// Inicializa los recursos basado en la configuración
    /// </summary>
    void InitializeResources()
    {
        foreach (var config in resourcesConfig)
        {
            resources[config.type] = config.currentAmount;
            OnResourceChanged?.Invoke(config.type, config.currentAmount);
        }
        CheckAllResources();
    }

    /// <summary>
    /// Bucle de consumo automático de recursos
    /// </summary>
    private IEnumerator AutoConsumptionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(consumptionInterval);

            ConsumeResource(ResourceType.Food, foodConsumptionRate);
            ConsumeResource(ResourceType.Water, waterConsumptionRate);
            ConsumeResource(ResourceType.Energy, energyConsumptionRate);

            Debug.Log("Consumo automático ejecutado");
        }
    }

    /// <summary>
    /// Añade una cantidad específica de un recurso
    /// </summary>
    public void AddResource(ResourceType type, int amount)
    {
        if (!resources.ContainsKey(type))
            resources[type] = 0;

        int oldValue = resources[type];
        resources[type] += amount;

        Debug.Log($"{type}: +{amount} = {resources[type]}");
        OnResourceChanged?.Invoke(type, resources[type]);

        // Verificar si el recurso salió de estado crítico
        if (oldValue <= GetMinimumLevel(type) && resources[type] > GetMinimumLevel(type))
        {
            OnResourceSafe?.Invoke(type);
        }
    }

    /// <summary>
    /// Consume una cantidad específica de un recurso
    /// </summary>
    /// <returns>True si se pudo consumir, false si no hay suficientes recursos</returns>
    public bool ConsumeResource(ResourceType type, int amount)
    {
        if (!resources.ContainsKey(type) || resources[type] < amount)
        {
            Debug.LogWarning($"No hay suficientes {type}. Necesitas {amount}, tienes {GetResourceAmount(type)}");
            return false;
        }

        int oldValue = resources[type];
        resources[type] -= amount;

        Debug.Log($"{type}: -{amount} = {resources[type]}");
        OnResourceChanged?.Invoke(type, resources[type]);

        CheckResourceState(type, oldValue);
        return true;
    }

    /// <summary>
    /// Verifica el estado del recurso después de un consumo
    /// </summary>
    void CheckResourceState(ResourceType type, int oldValue)
    {
        int current = resources[type];
        int minimum = GetMinimumLevel(type);

        if (oldValue > minimum && current <= minimum)
        {
            Debug.LogWarning($"{type} está en nivel CRÍTICO! ({current}/{minimum})");
            OnResourceCritical?.Invoke(type);
        }

        CheckAllResources();
    }

    /// <summary>
    /// Verifica el estado de todos los recursos
    /// </summary>
    void CheckAllResources()
    {
        int criticalCount = 0;
        foreach (var resource in resources)
        {
            if (resource.Value <= GetMinimumLevel(resource.Key))
            {
                criticalCount++;
            }
        }

        if (criticalCount >= resources.Count)
        {
            Debug.LogError("GAME OVER - Todos los recursos están en nivel crítico!");
            OnGameOver?.Invoke();
        }
    }

    /// <summary>
    /// Resetea todos los recursos a sus valores iniciales
    /// </summary>
    public void ResetAllResources()
    {
        foreach (var config in resourcesConfig)
        {
            resources[config.type] = config.currentAmount;
            OnResourceChanged?.Invoke(config.type, config.currentAmount);
        }
        Debug.Log("Todos los recursos fueron reseteados");
    }

    /// <summary>
    /// Obtiene la configuración de un recurso específico
    /// </summary>
    public ResourceConfig GetResourceConfig(ResourceType type)
    {
        return resourcesConfig.Find(c => c.type == type);
    }

    /// <summary>
    /// Obtiene la cantidad actual de un recurso
    /// </summary>
    public int GetResourceAmount(ResourceType type)
    {
        return resources.ContainsKey(type) ? resources[type] : 0;
    }

    /// <summary>
    /// Verifica si hay suficientes recursos de un tipo
    /// </summary>
    public bool HasEnoughResources(ResourceType type, int amount)
    {
        return GetResourceAmount(type) >= amount;
    }

    /// <summary>
    /// Obtiene el nivel mínimo seguro de un recurso
    /// </summary>
    public int GetMinimumLevel(ResourceType type)
    {
        var config = GetResourceConfig(type);
        return config != null ? config.minimumSafeLevel : 0;
    }

    /// <summary>
    /// Obtiene el nivel de advertencia de un recurso
    /// </summary>
    public int GetWarningLevel(ResourceType type)
    {
        var config = GetResourceConfig(type);
        return config != null ? config.warningLevel : 0;
    }

    /// <summary>
    /// Añade recursos de debug para testing
    /// </summary>
    public void DebugAddResources()
    {
        AddResource(ResourceType.Food, 50);
        AddResource(ResourceType.Water, 40);
        AddResource(ResourceType.Energy, 30);
        AddResource(ResourceType.Materials, 20);
    }

    /// <summary>
    /// Datos para guardar el estado de los recursos
    /// </summary>
    [System.Serializable]
    public class ResourceSaveData
    {
        public int food;
        public int water;
        public int energy;
        public int materials;
    }

    /// <summary>
    /// Carga los recursos desde datos guardados
    /// </summary>
    public void LoadFromSave(ResourceSaveData saveData)
    {
        resources[ResourceType.Food] = saveData.food;
        resources[ResourceType.Water] = saveData.water;
        resources[ResourceType.Energy] = saveData.energy;
        resources[ResourceType.Materials] = saveData.materials;

        foreach (var resource in resources)
        {
            OnResourceChanged?.Invoke(resource.Key, resource.Value);
        }
        CheckAllResources();
    }
}