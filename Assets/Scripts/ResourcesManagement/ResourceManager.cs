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

    public static event Action<ResourceType> OnResourceCritical;
    public static event Action<ResourceType> OnResourceSafe;
    public static event Action OnGameOver;

    public static event Action<DwellerNPC> OnNPCDied;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    public static event Action<ResourceType, int> OnResourceChanged;

    [Header("Consumo Automático")]
    [SerializeField] private float consumptionInterval = 10f;
    [SerializeField] private int foodConsumptionRate = 5;
    [SerializeField] private int energyConsumptionRate = 3;
    [SerializeField] private int waterConsumptionRate = 4;

    [Header("Configuración Game Over")]
    [SerializeField] private int maxAllowedDeaths = 3;
    private List<DwellerNPC> deadNPCs = new List<DwellerNPC>();
    private bool gameOverTriggered = false;

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
        FindAllDwellersAndSubscribe();
        InvokeRepeating("FindAndSubscribeNewDwellers", 5f, 5f);
    }

    void OnDestroy()
    {
        UnsubscribeAllDwellers();
    }

    private void FindAllDwellersAndSubscribe()
    {
        DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
        foreach (DwellerNPC dweller in allDwellers)
        {
            SubscribeToDweller(dweller);
        }
        Debug.Log($"Suscrito a {allDwellers.Length} NPCs para detección de muertes");
    }

    private void FindAndSubscribeNewDwellers()
    {
        DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
        foreach (DwellerNPC dweller in allDwellers)
        {
            if (!deadNPCs.Contains(dweller))
            {
                SubscribeToDweller(dweller);
            }
        }
    }

    private void SubscribeToDweller(DwellerNPC dweller)
    {
        if (dweller != null && !deadNPCs.Contains(dweller))
        {
            dweller.OnDeath += HandleNPCDied;
        }
    }

    private void UnsubscribeAllDwellers()
    {
        DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
        foreach (DwellerNPC dweller in allDwellers)
        {
            if (dweller != null)
            {
                dweller.OnDeath -= HandleNPCDied;
            }
        }
    }

    private void HandleNPCDied(DwellerNPC deadNPC)
    {
        if (deadNPCs.Contains(deadNPC)) return;

        deadNPCs.Add(deadNPC);
        Debug.Log($"Muerte registrada: {deadNPC.dwellerName}. Total muertos: {deadNPCs.Count}/{maxAllowedDeaths}");

        OnNPCDied?.Invoke(deadNPC);
        CheckGameOverCondition();
    }

    private void CheckGameOverCondition()
    {
        if (gameOverTriggered) return;

        if (deadNPCs.Count >= maxAllowedDeaths)
        {
            TriggerGameOver("Demasiados habitantes han fallecido");
            return;
        }

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
            TriggerGameOver("Todos los recursos están en nivel crítico");
            return;
        }

        DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
        int aliveCount = 0;
        foreach (DwellerNPC dweller in allDwellers)
        {
            if (!dweller.IsDead)
            {
                aliveCount++;
            }
        }

        if (aliveCount == 0)
        {
            TriggerGameOver("Todos los habitantes han fallecido");
            return;
        }
    }

    public void TriggerGameOver()
    {
        if (gameOverTriggered) return;

        gameOverTriggered = true;
        Debug.LogError("GAME OVER ACTIVADO MANUALMENTE");

        OnGameOver?.Invoke();
    }

    private void TriggerGameOver(string reason)
    {
        if (gameOverTriggered) return;

        gameOverTriggered = true;
        Debug.LogError($"GAME OVER: {reason}");

        OnGameOver?.Invoke();

        Debug.Log($"ESTADÍSTICAS FINALES - NPCs Muertos: {deadNPCs.Count}");
    }

    private string GetResourcesStatus()
    {
        string status = "";
        foreach (var resource in resources)
        {
            status += $"{resource.Key}: {resource.Value} ";
        }
        return status;
    }

    public void ResetGameOver()
    {
        gameOverTriggered = false;
        deadNPCs.Clear();
        Time.timeScale = 1f;
        Debug.Log("Estado de Game Over reiniciado");
    }

    public int GetDeadNPCCount()
    {
        return deadNPCs.Count;
    }

    public int GetMaxAllowedDeaths()
    {
        return maxAllowedDeaths;
    }

    public bool IsGameOverTriggered()
    {
        return gameOverTriggered;
    }

    public void DebugDeathStatus()
    {
        Debug.Log("=== DEBUG MUERTES NPCs ===");
        Debug.Log($"Muertes registradas: {deadNPCs.Count}");
        Debug.Log($"Límite para Game Over: {maxAllowedDeaths}");
        Debug.Log($"Game Over activado: {gameOverTriggered}");

        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();
        int actualDeadCount = 0;

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc.IsDead) actualDeadCount++;
        }

        Debug.Log($"NPCs muertos en escena: {actualDeadCount}");
        Debug.Log("=== FIN DEBUG ===");
    }

    public void ForceCheckGameOver()
    {
        Debug.Log("Forzando verificación de Game Over...");
        CheckGameOverCondition();
    }

    void InitializeResources()
    {
        foreach (var config in resourcesConfig)
        {
            resources[config.type] = config.currentAmount;
            OnResourceChanged?.Invoke(config.type, config.currentAmount);
        }
        CheckAllResources();
    }

    private IEnumerator AutoConsumptionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(consumptionInterval);

            if (gameOverTriggered) continue;

            ConsumeResource(ResourceType.Food, foodConsumptionRate);
            ConsumeResource(ResourceType.Water, waterConsumptionRate);
            ConsumeResource(ResourceType.Energy, energyConsumptionRate);

            Debug.Log("Consumo automático ejecutado");
        }
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (gameOverTriggered) return;

        if (!resources.ContainsKey(type))
            resources[type] = 0;

        int oldValue = resources[type];
        resources[type] += amount;

        Debug.Log($"{type}: +{amount} = {resources[type]}");
        OnResourceChanged?.Invoke(type, resources[type]);

        if (oldValue <= GetMinimumLevel(type) && resources[type] > GetMinimumLevel(type))
        {
            OnResourceSafe?.Invoke(type);
        }
    }

    public bool ConsumeResource(ResourceType type, int amount)
    {
        if (gameOverTriggered) return false;

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

    void CheckAllResources()
    {
        if (gameOverTriggered) return;

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
            TriggerGameOver("Todos los recursos están en nivel crítico");
        }
    }

    public void ResetAllResources()
    {
        foreach (var config in resourcesConfig)
        {
            resources[config.type] = config.currentAmount;
            OnResourceChanged?.Invoke(config.type, config.currentAmount);
        }
        Debug.Log("Todos los recursos fueron reseteados");
    }

    public ResourceConfig GetResourceConfig(ResourceType type)
    {
        return resourcesConfig.Find(c => c.type == type);
    }

    public int GetResourceAmount(ResourceType type)
    {
        return resources.ContainsKey(type) ? resources[type] : 0;
    }

    public bool HasEnoughResources(ResourceType type, int amount)
    {
        return GetResourceAmount(type) >= amount;
    }

    public int GetMinimumLevel(ResourceType type)
    {
        var config = GetResourceConfig(type);
        return config != null ? config.minimumSafeLevel : 0;
    }

    public int GetWarningLevel(ResourceType type)
    {
        var config = GetResourceConfig(type);
        return config != null ? config.warningLevel : 0;
    }

    public void DebugAddResources()
    {
        AddResource(ResourceType.Food, 50);
        AddResource(ResourceType.Water, 40);
        AddResource(ResourceType.Energy, 30);
        AddResource(ResourceType.Materials, 20);
    }

    [System.Serializable]
    public class ResourceSaveData
    {
        public int food;
        public int water;
        public int energy;
        public int materials;
        public int deadNPCCount;
        public bool gameOverTriggered;
    }

    public void LoadFromSave(ResourceSaveData saveData)
    {
        resources[ResourceType.Food] = saveData.food;
        resources[ResourceType.Water] = saveData.water;
        resources[ResourceType.Energy] = saveData.energy;
        resources[ResourceType.Materials] = saveData.materials;

        gameOverTriggered = saveData.gameOverTriggered;

        foreach (var resource in resources)
        {
            OnResourceChanged?.Invoke(resource.Key, resource.Value);
        }

        if (gameOverTriggered)
        {
            TriggerGameOver("Partida cargada en estado de Game Over");
        }

        CheckAllResources();
    }
}