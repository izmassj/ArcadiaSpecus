using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitle;
    public TextMeshProUGUI gameOverDescription;
    public TextMeshProUGUI gameOverStats;
    public Button restartButton;
    public Button mainMenuButton;

    private bool isGameOver = false;
    private bool initialized = false;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("🗑️ GameOverManager duplicado detectado, destruyendo el nuevo.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Debug.Log("🎮 GameOverManager inicializado (Awake)");

        // Inicializa UI/botones una única vez (Awake se ejecuta aunque el GO esté desactivado)
        InitializeGameOverSystem(hidePanel: true);

        // Suscripción temprana: funciona aunque este componente esté en un GO desactivado
        ResourceManager.OnGameOver -= HandleGameOver;
        ResourceManager.OnGameOver += HandleGameOver;

        // Si recargas escena o ya venía en GameOver, sincroniza
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        TrySyncWithResourceManager();
    }

    private void OnEnable()
    {
        // Si este GO se activa tarde (p.ej. porque estaba dentro del panel desactivado),
        // NO queremos que "inicialice" apagando el panel si ya hay Game Over.
        InitializeGameOverSystem(hidePanel: !isGameOver);

        TrySyncWithResourceManager();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySyncWithResourceManager();
    }

    private void InitializeGameOverSystem(bool hidePanel)
    {
        if (initialized)
        {
            // Si ya estaba inicializado, solo aplicamos hidePanel cuando tenga sentido.
            if (hidePanel && !isGameOver && gameOverPanel != null)
                gameOverPanel.SetActive(false);

            return;
        }

        initialized = true;
        Debug.Log("🔄 Inicializando sistema de Game Over...");

        if (gameOverPanel != null)
        {
            if (hidePanel)
            {
                gameOverPanel.SetActive(false);
                Debug.Log("✅ GameOverPanel DESACTIVADO al inicio");
            }
        }
        else
        {
            Debug.LogError("❌ GameOverPanel no asignado en GameOverManager!");
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            Debug.Log("✅ RestartButton configurado");
        }
        else
        {
            Debug.LogWarning("⚠️ restartButton no asignado");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            Debug.Log("✅ MainMenuButton configurado");
        }
        else
        {
            Debug.LogWarning("⚠️ mainMenuButton no asignado");
        }

        Debug.Log("✅ Sistema de Game Over inicializado correctamente");
    }

    private void TrySyncWithResourceManager()
    {
        if (isGameOver) return;

        if (ResourceManager.Instance != null && ResourceManager.Instance.IsGameOverTriggered())
        {
            Debug.LogWarning("🎮 ResourceManager ya tiene Game Over activo. Sincronizando UI...");
            HandleGameOver();
        }
    }

    private void HandleGameOver()
    {
        if (isGameOver)
        {
            Debug.LogWarning("⚠️ Game Over ya estaba activado, ignorando...");
            return;
        }

        isGameOver = true;
        Debug.Log("🎮 ACTIVANDO GAME OVER (evento)...");

        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            SetupGameOverUI();
            Debug.Log("✅ GameOverPanel ACTIVADO Y VISIBLE");
        }
        else
        {
            Debug.LogError("❌ No se puede activar GameOverPanel - referencia nula!");
        }
    }

    public void ForceGameOver()
    {
        if (isGameOver)
        {
            Debug.LogWarning("⚠️ Game Over ya estaba activado, ignorando...");
            return;
        }

        // Mejor dispararlo desde ResourceManager para tener un único flujo
        if (ResourceManager.Instance != null && !ResourceManager.Instance.IsGameOverTriggered())
        {
            Debug.LogWarning("🎮 FORZANDO GAME OVER a través de ResourceManager...");
            ResourceManager.Instance.TriggerGameOver();
            return;
        }

        // Fallback: mostrar UI igualmente
        Debug.LogWarning("🎮 FORZANDO GAME OVER manual (fallback)...");
        HandleGameOver();
    }

    private void SetupGameOverUI()
    {
        Debug.Log("🔄 Configurando UI de Game Over...");

        if (gameOverTitle != null)
            gameOverTitle.text = "GAME OVER";

        // Si no hay ResourceManager, mostramos un texto genérico
        if (ResourceManager.Instance == null)
        {
            if (gameOverDescription != null)
                gameOverDescription.text = "La partida ha terminado.";

            if (gameOverStats != null)
                gameOverStats.text = "";

            Debug.LogWarning("⚠️ ResourceManager.Instance es nulo; UI mostrada en modo fallback.");
            return;
        }

        if (gameOverDescription != null)
        {
            int deadCount = ResourceManager.Instance.GetDeadNPCCount();
            int maxDeaths = ResourceManager.Instance.GetMaxAllowedDeaths();

            if (deadCount >= maxDeaths)
                gameOverDescription.text = $"Demasiados habitantes han fallecido\n({deadCount} de {maxDeaths} permitidos)";
            else
                gameOverDescription.text = "Condición de derrota alcanzada.";
        }

        if (gameOverStats != null)
        {
            string statsText = "Estadísticas Finales:\n";
            statsText += $"• Habitantes fallecidos: {ResourceManager.Instance.GetDeadNPCCount()}\n";
            statsText += $"• Comida: {ResourceManager.Instance.GetResourceAmount(ResourceType.Food)}\n";
            statsText += $"• Agua: {ResourceManager.Instance.GetResourceAmount(ResourceType.Water)}\n";
            statsText += $"• Energía: {ResourceManager.Instance.GetResourceAmount(ResourceType.Energy)}";

            gameOverStats.text = statsText;
        }

        Debug.Log("✅ UI de Game Over configurada");
    }

    private void RestartGame()
    {
        Debug.Log("🔄 Iniciando reinicio del juego...");

        Time.timeScale = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        isGameOver = false;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetGameOver();
            ResourceManager.Instance.ResetAllResources();
        }

        ReviveAllNPCs();

        if (AssignmentManager.Instance != null)
        {
            AssignmentManager.Instance.ResetAllAssignments();
            AssignmentManager.Instance.AutoAssignAll();
        }

        Debug.Log("✅ Juego reiniciado completamente");
    }

    public void ForceRestart()
    {
        Debug.Log("🔄 FORZANDO REINICIO DESDE GameOverManager...");
        RestartGame();
    }

    private void ReviveAllNPCs()
    {
        DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
        int revivedCount = 0;

        foreach (DwellerNPC dweller in allDwellers)
        {
            if (dweller != null && dweller.IsDead)
            {
                dweller.Revive();
                revivedCount++;
            }
        }

        Debug.Log($"🔁 Revividos {revivedCount} NPCs de {allDwellers.Length} totales");
    }

    private void GoToMainMenu()
    {
        Debug.Log("🏠 Cargando menú principal (recargando escena actual)...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool IsGameOver() => isGameOver;

    [ContextMenu("🔴 Forzar Game Over")]
    public void ContextForceGameOver() => ForceGameOver();

    [ContextMenu("🔄 Forzar Reinicio")]
    public void ContextForceRestart() => ForceRestart();

    [ContextMenu("📊 Debug Estado")]
    public void DebugStatus()
    {
        Debug.Log("=== GAME OVER MANAGER STATUS ===");
        Debug.Log($"¿Game Over activo?: {isGameOver}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"GameOverPanel asignado: {gameOverPanel != null}");
        Debug.Log($"GameOverPanel activo: {gameOverPanel != null && gameOverPanel.activeInHierarchy}");
        Debug.Log($"ResourceManager: {ResourceManager.Instance != null}");

        if (ResourceManager.Instance != null)
            Debug.Log($"NPCs muertos: {ResourceManager.Instance.GetDeadNPCCount()}");

        Debug.Log("=== FIN DEBUG ===");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ResourceManager.OnGameOver -= HandleGameOver;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        Debug.Log("🗑️ GameOverManager destruido");
    }
}
