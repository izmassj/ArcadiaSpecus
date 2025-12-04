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

    /// <summary>
    /// Initializes the GameOverManager as a singleton and sets up event subscriptions
    /// </summary>
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameOverManager duplicate detected, destroying the new one.");
            return;
        }
        Instance = this;

        Debug.Log("GameOverManager initialized (Awake)");

        // Initialize UI/buttons once (Awake runs even if GO is deactivated)
        InitializeGameOverSystem(hidePanel: true);

        // Early subscription: works even if this component is in a deactivated GO
        ResourceManager.OnGameOver -= HandleGameOver;
        ResourceManager.OnGameOver += HandleGameOver;

        // If scene reloads or already came in GameOver state, synchronize
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        TrySyncWithResourceManager();
    }

    private void OnEnable()
    {
        // If this GO activates late (e.g., because it was inside a deactivated panel),
        // we don't want to "initialize" by turning off the panel if there's already Game Over.
        InitializeGameOverSystem(hidePanel: !isGameOver);

        TrySyncWithResourceManager();
    }

    /// <summary>
    /// Handles scene loading to synchronize game over state
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySyncWithResourceManager();
    }

    /// <summary>
    /// Initializes the game over system UI components
    /// </summary>
    /// <param name="hidePanel">Whether to hide the game over panel initially</param>
    private void InitializeGameOverSystem(bool hidePanel)
    {
        if (initialized)
        {
            // If already initialized, only apply hidePanel when it makes sense
            if (hidePanel && !isGameOver && gameOverPanel != null)
                gameOverPanel.SetActive(false);

            return;
        }

        initialized = true;
        Debug.Log("Initializing Game Over system...");

        if (gameOverPanel != null)
        {
            if (hidePanel)
            {
                gameOverPanel.SetActive(false);
                Debug.Log("GameOverPanel deactivated at start");
            }
        }
        else
        {
            Debug.LogError("GameOverPanel not assigned in GameOverManager!");
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            Debug.Log("RestartButton configured");
        }
        else
        {
            Debug.LogWarning("restartButton not assigned");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            Debug.Log("MainMenuButton configured");
        }
        else
        {
            Debug.LogWarning("mainMenuButton not assigned");
        }

        Debug.Log("Game Over system initialized correctly");
    }

    /// <summary>
    /// Synchronizes with ResourceManager if game over is already triggered
    /// </summary>
    private void TrySyncWithResourceManager()
    {
        if (isGameOver) return;

        if (ResourceManager.Instance != null && ResourceManager.Instance.IsGameOverTriggered())
        {
            Debug.LogWarning("ResourceManager already has Game Over active. Synchronizing UI...");
            HandleGameOver();
        }
    }

    /// <summary>
    /// Handles the game over event from ResourceManager
    /// </summary>
    private void HandleGameOver()
    {
        if (isGameOver)
        {
            Debug.LogWarning("Game Over already active, ignoring...");
            return;
        }

        isGameOver = true;
        Debug.Log("ACTIVATING GAME OVER (event)...");

        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            SetupGameOverUI();
            Debug.Log("GameOverPanel ACTIVATED AND VISIBLE");
        }
        else
        {
            Debug.LogError("Cannot activate GameOverPanel - null reference!");
        }
    }

    /// <summary>
    /// Forces game over state manually
    /// </summary>
    public void ForceGameOver()
    {
        if (isGameOver)
        {
            Debug.LogWarning("Game Over already active, ignoring...");
            return;
        }

        // Better to trigger it from ResourceManager to have a single flow
        if (ResourceManager.Instance != null && !ResourceManager.Instance.IsGameOverTriggered())
        {
            Debug.LogWarning("FORCING GAME OVER through ResourceManager...");
            ResourceManager.Instance.TriggerGameOver();
            return;
        }

        // Fallback: show UI anyway
        Debug.LogWarning("FORCING GAME OVER manually (fallback)...");
        HandleGameOver();
    }

    /// <summary>
    /// Sets up the game over UI with appropriate text and statistics
    /// </summary>
    private void SetupGameOverUI()
    {
        Debug.Log("Setting up Game Over UI...");

        if (gameOverTitle != null)
            gameOverTitle.text = "GAME OVER";

        // If no ResourceManager, show generic text
        if (ResourceManager.Instance == null)
        {
            if (gameOverDescription != null)
                gameOverDescription.text = "The game has ended.";

            if (gameOverStats != null)
                gameOverStats.text = "";

            Debug.LogWarning("ResourceManager.Instance is null; UI shown in fallback mode.");
            return;
        }

        if (gameOverDescription != null)
        {
            int deadCount = ResourceManager.Instance.GetDeadNPCCount();
            int maxDeaths = ResourceManager.Instance.GetMaxAllowedDeaths();

            if (deadCount >= maxDeaths)
                gameOverDescription.text = $"Too many inhabitants have died\n({deadCount} of {maxDeaths} allowed)";
            else
                gameOverDescription.text = "Defeat condition reached.";
        }

        if (gameOverStats != null)
        {
            string statsText = "Final Statistics:\n";
            statsText += $"• Dead inhabitants: {ResourceManager.Instance.GetDeadNPCCount()}\n";
            statsText += $"• Food: {ResourceManager.Instance.GetResourceAmount(ResourceType.Food)}\n";
            statsText += $"• Water: {ResourceManager.Instance.GetResourceAmount(ResourceType.Water)}\n";
            statsText += $"• Energy: {ResourceManager.Instance.GetResourceAmount(ResourceType.Energy)}";

            gameOverStats.text = statsText;
        }

        Debug.Log("Game Over UI configured");
    }

    /// <summary>
    /// Restarts the game by resetting all systems
    /// </summary>
    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        //Debug.Log("Initiating game restart...");

        //Time.timeScale = 1f;

        //if (gameOverPanel != null)
        //    gameOverPanel.SetActive(false);

        //isGameOver = false;

        //if (ResourceManager.Instance != null)
        //{
        //    ResourceManager.Instance.ResetGameOver();
        //    ResourceManager.Instance.ResetAllResources();
        //}

        //ReviveAllNPCs();

        //if (AssignmentManager.Instance != null)
        //{
        //    AssignmentManager.Instance.ResetAllAssignments();
        //    AssignmentManager.Instance.AutoAssignAll();
        //}

        //Debug.Log("Game completely restarted");
    }

    /// <summary>
    /// Forces game restart from external calls
    /// </summary>
    public void ForceRestart()
    {
        Debug.Log("FORCING RESTART FROM GameOverManager...");
        RestartGame();
    }

    /// <summary>
    /// Revives all dead NPCs in the scene
    /// </summary>
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

    /// <summary>
    /// Returns to main menu by reloading the current scene
    /// </summary>
    private void GoToMainMenu()
    {
        Debug.Log("Loading main menu (reloading current scene)...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Returns whether game over state is active
    /// </summary>
    public bool IsGameOver() => isGameOver;

    [ContextMenu("Force Game Over")]
    public void ContextForceGameOver() => ForceGameOver();

    [ContextMenu("Force Restart")]
    public void ContextForceRestart() => ForceRestart();

    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log("=== GAME OVER MANAGER STATUS ===");
        Debug.Log($"Game Over active?: {isGameOver}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"GameOverPanel assigned: {gameOverPanel != null}");
        Debug.Log($"GameOverPanel active: {gameOverPanel != null && gameOverPanel.activeInHierarchy}");
        Debug.Log($"ResourceManager: {ResourceManager.Instance != null}");

        if (ResourceManager.Instance != null)
            Debug.Log($"Dead NPCs: {ResourceManager.Instance.GetDeadNPCCount()}");

        Debug.Log("=== END DEBUG ===");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ResourceManager.OnGameOver -= HandleGameOver;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        Debug.Log("GameOverManager destroyed");
    }
}