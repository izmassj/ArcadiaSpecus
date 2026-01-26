using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] public GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverTitle;
    [SerializeField] private TextMeshProUGUI gameOverDescription;
    [SerializeField] private TextMeshProUGUI gameOverStats;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private bool _isGameOver = false;
    private bool _isInitialized = false;


    private void Awake()
    {
        InitializeSingleton();
        InitializeGameOverSystem(hidePanel: true);
        SubscribeToEvents();
        TrySyncWithResourceManager();
    }

    private void OnEnable()
    {
        InitializeGameOverSystem(hidePanel: !_isGameOver);
        TrySyncWithResourceManager();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnsubscribeFromEvents();
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameOverManager duplicate detected, destroying the new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("GameOverManager initialized (Awake)");
    }

    private void SubscribeToEvents()
    {
        ResourceManager.OnGameOver -= HandleGameOver;
        ResourceManager.OnGameOver += HandleGameOver;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void UnsubscribeFromEvents()
    {
        ResourceManager.OnGameOver -= HandleGameOver;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeGameOverSystem(bool hidePanel)
    {
        if (_isInitialized)
        {
            if (hidePanel && !_isGameOver && gameOverPanel != null)
                gameOverPanel.SetActive(false);
            return;
        }

        _isInitialized = true;
        Debug.Log("Initializing Game Over system...");

        ValidateUIReferences();
        ConfigureButtons();

        if (hidePanel && gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("GameOverPanel deactivated at start");
        }

        Debug.Log("Game Over system initialized correctly");
    }

    private void ValidateUIReferences()
    {
        if (gameOverPanel == null)
            Debug.LogError("GameOverPanel not assigned in GameOverManager!");

        if (restartButton == null)
            Debug.LogWarning("restartButton not assigned");

        if (mainMenuButton == null)
            Debug.LogWarning("mainMenuButton not assigned");
    }

    private void ConfigureButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            Debug.Log("RestartButton configured");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            Debug.Log("MainMenuButton configured");
        }
    }

    private void HandleGameOver()
    {
        if (_isGameOver)
        {
            Debug.LogWarning("Game Over already active, ignoring...");
            return;
        }

        _isGameOver = true;
        Debug.Log("ACTIVATING GAME OVER (event)...");

        Time.timeScale = 0f;
        ShowGameOverPanel();
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("Cannot activate GameOverPanel - null reference!");
            return;
        }

        gameOverPanel.SetActive(true);
        SetupGameOverUI();
        Debug.Log("GameOverPanel ACTIVATED AND VISIBLE");
    }

    private void SetupGameOverUI()
    {
        Debug.Log("Setting up Game Over UI...");

        if (gameOverTitle != null)
            gameOverTitle.text = "GAME OVER";

        if (ResourceManager.Instance == null)
        {
            SetupFallbackUI();
            return;
        }

        SetupResourceBasedUI();
    }

    private void SetupFallbackUI()
    {
        if (gameOverDescription != null)
            gameOverDescription.text = "The game has ended.";

        if (gameOverStats != null)
            gameOverStats.text = "";

        Debug.LogWarning("ResourceManager.Instance is null; UI shown in fallback mode.");
    }

    private void SetupResourceBasedUI()
    {
        int deadCount = ResourceManager.Instance.GetDeadNPCCount();
        int maxDeaths = ResourceManager.Instance.GetMaxAllowedDeaths();

        if (gameOverDescription != null)
        {
            gameOverDescription.text = deadCount >= maxDeaths
                ? $"Too many inhabitants have died\n({deadCount} of {maxDeaths} allowed)"
                : "Defeat condition reached.";
        }

        if (gameOverStats != null)
        {
            gameOverStats.text = GenerateStatsText();
        }

        Debug.Log("Game Over UI configured");
    }

    private string GenerateStatsText()
    {
        if (ResourceManager.Instance == null) return "";

        return $"Final Statistics:\n" +
               $"• Dead inhabitants: {ResourceManager.Instance.GetDeadNPCCount()}\n" +
               $"• Food: {ResourceManager.Instance.GetResourceAmount(ResourceType.Food)}\n" +
               $"• Water: {ResourceManager.Instance.GetResourceAmount(ResourceType.Water)}\n" +
               $"• Energy: {ResourceManager.Instance.GetResourceAmount(ResourceType.Energy)}";
    }

    public void ForceGameOver()
    {
        if (_isGameOver)
        {
            Debug.LogWarning("Game Over already active, ignoring...");
            return;
        }

        if (ResourceManager.Instance != null && !ResourceManager.Instance.IsGameOverTriggered())
        {
            Debug.LogWarning("FORCING GAME OVER through ResourceManager...");
            ResourceManager.Instance.TriggerGameOver();
            return;
        }

        Debug.LogWarning("FORCING GAME OVER manually (fallback)...");
        HandleGameOver();
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySyncWithResourceManager();
    }

    private void TrySyncWithResourceManager()
    {
        if (_isGameOver) return;

        if (ResourceManager.Instance?.IsGameOverTriggered() == true)
        {
            Debug.LogWarning("ResourceManager already has Game Over active. Synchronizing UI...");
            HandleGameOver();
        }
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMainMenu()
    {
        Debug.Log("Loading main menu...");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ForceRestart()
    {
        Debug.Log("FORCING RESTART FROM GameOverManager...");
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

        Debug.Log($"Revived {revivedCount} NPCs of {allDwellers.Length} total");
    }

    public bool IsGameOver() => _isGameOver;

    [ContextMenu("Force Game Over")]
    public void ContextForceGameOver() => ForceGameOver();

    [ContextMenu("Force Restart")]
    public void ContextForceRestart() => ForceRestart();

    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log("=== GAME OVER MANAGER STATUS ===");
        Debug.Log($"Game Over active?: {_isGameOver}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"GameOverPanel assigned: {gameOverPanel != null}");
        Debug.Log($"GameOverPanel active: {gameOverPanel != null && gameOverPanel.activeInHierarchy}");
        Debug.Log($"ResourceManager: {ResourceManager.Instance != null}");

        if (ResourceManager.Instance != null)
            Debug.Log($"Dead NPCs: {ResourceManager.Instance.GetDeadNPCCount()}");

        Debug.Log("=== END DEBUG ===");
    }
}