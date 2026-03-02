using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private bool _isGameOver;
    private bool _isInitialized;

    private void Awake()
    {
        InitializeSingleton();
        SceneFlowManager.EnsureInstance();

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
        if (Instance == this)
            Instance = null;

        UnsubscribeFromEvents();
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameOverManager duplicado detectado. Se destruye el nuevo.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void SubscribeToEvents()
    {
        ResourceManager.OnGameOver -= HandleGameOverFromResourceEvent;
        ResourceManager.OnGameOver += HandleGameOverFromResourceEvent;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void UnsubscribeFromEvents()
    {
        ResourceManager.OnGameOver -= HandleGameOverFromResourceEvent;
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

        ValidateUIReferences();
        ConfigureButtons();

        if (hidePanel && gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void ValidateUIReferences()
    {
        if (gameOverPanel == null)
            Debug.LogError("GameOverManager: gameOverPanel no asignado.");

        if (restartButton == null)
            Debug.LogWarning("GameOverManager: restartButton no asignado.");

        if (mainMenuButton == null)
            Debug.LogWarning("GameOverManager: mainMenuButton no asignado.");
    }

    private void ConfigureButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }

    private void HandleGameOverFromResourceEvent()
    {
        if (_isGameOver)
            return;

        _isGameOver = true;
        Time.timeScale = 0f;

        // Música/SFX de derrota (rúbrica: feedback al Game Over).
        if (PersistentMusic.instance != null)
            PersistentMusic.instance.PlayAlert();

        VFXManager.EnsureInstance();
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayOnCamera(VFXKind.GameOver);

        ShowGameOverPanelFromRuntimeData();
    }

    private void ShowGameOverPanelFromRuntimeData()
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("GameOverManager: no se puede mostrar Game Over porque falta el panel.");
            return;
        }

        gameOverPanel.SetActive(true);
        SetupGameOverUIFromResourceManager();
        // Feedback ya disparado en HandleGameOverFromResourceEvent (evitamos doble SFX).
    }

    private void SetupGameOverUIFromResourceManager()
    {
        if (gameOverTitle != null)
            gameOverTitle.text = "GAME OVER";

        if (ResourceManager.Instance == null)
        {
            if (gameOverDescription != null)
                gameOverDescription.text = "Se ha alcanzado una condición de derrota.";

            if (gameOverStats != null)
                gameOverStats.text = string.Empty;

            return;
        }

        int deadCount = ResourceManager.Instance.GetDeadNPCCount();
        int maxDeaths = ResourceManager.Instance.GetMaxAllowedDeaths();

        if (gameOverDescription != null)
        {
            if (deadCount >= maxDeaths)
            {
                gameOverDescription.text = $"Demasiados habitantes han fallecido\n({deadCount}/{maxDeaths})";
            }
            else
            {
                gameOverDescription.text = "Se ha alcanzado una condición de derrota.";
            }
        }

        if (gameOverStats != null)
        {
            gameOverStats.text =
                "ESTADÍSTICAS FINALES\n" +
                $"- Habitantes muertos: {ResourceManager.Instance.GetDeadNPCCount()}\n" +
                $"- Comida: {ResourceManager.Instance.GetResourceAmount(ResourceType.Food)}\n" +
                $"- Agua: {ResourceManager.Instance.GetResourceAmount(ResourceType.Water)}\n" +
                $"- Energía: {ResourceManager.Instance.GetResourceAmount(ResourceType.Energy)}\n" +
                $"- Materiales: {ResourceManager.Instance.GetResourceAmount(ResourceType.Materials)}";
        }
    }

    /// <summary>
    /// API pública para forzar Game Over desde otros sistemas (debug o condiciones especiales).
    /// </summary>
    public void ForceGameOver()
    {
        if (_isGameOver)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.IsGameOverTriggered())
        {
            ResourceManager.Instance.TriggerGameOver();
            return;
        }

        _isGameOver = true;
        Time.timeScale = 0f;
        ShowGameOver("GAME OVER", "Se ha alcanzado una condición de derrota.", string.Empty);
    }

    /// <summary>
    /// API pública para mostrar el panel con textos personalizados.
    /// </summary>
    public void ShowGameOver(string titleText, string descriptionText, string statsText)
    {
        _isGameOver = true;
        Time.timeScale = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverTitle != null)
            gameOverTitle.text = string.IsNullOrWhiteSpace(titleText) ? "GAME OVER" : titleText;

        if (gameOverDescription != null)
            gameOverDescription.text = descriptionText ?? string.Empty;

        if (gameOverStats != null)
            gameOverStats.text = statsText ?? string.Empty;

        // Feedback audiovisual (VFX + SFX)
        if (PersistentMusic.instance != null)
            PersistentMusic.instance.PlayAlert();

        VFXManager.EnsureInstance();
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayOnCamera(VFXKind.GameOver);

        // (Si quieres animar el panel: aquí es el sitio.)
    }

    public void HideGameOver()
    {
        _isGameOver = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al cargar escenas, reconfiguramos botones por si el panel/UI fue reinstanciado.
        ConfigureButtons();

        // Si es una carga Single, el juego no debería seguir en Game Over visual.
        if (mode == LoadSceneMode.Single)
        {
            _isGameOver = false;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            Time.timeScale = 1f;
        }

        TrySyncWithResourceManager();
    }

    private void TrySyncWithResourceManager()
    {
        if (_isGameOver)
            return;

        if (ResourceManager.Instance != null && ResourceManager.Instance.IsGameOverTriggered())
        {
            HandleGameOverFromResourceEvent();
        }
    }

    public void RestartGame()
    {
        if (PersistentMusic.instance != null)
            PersistentMusic.instance.PlayUiClick();

        Time.timeScale = 1f;

        // Reinicio de estado global si existe ResourceManager persistente.
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetGameOver();
            ResourceManager.Instance.ResetAllResources();
        }

        // Si tuvierais lógica de "revivir" sin recargar escena, aquí sería el punto.
        // ReviveAllNPCs();

        SceneFlowManager.EnsureInstance().RestartCurrentScene();
    }

    public void GoToMainMenu()
    {
        if (PersistentMusic.instance != null)
            PersistentMusic.instance.PlayUiBack();

        Time.timeScale = 1f;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetGameOver();
        }

        SceneFlowManager.EnsureInstance().LoadMainMenu();
    }

    public void ForceRestart()
    {
        RestartGame();
    }

    private void ReviveAllNPCs()
    {
        // Método mantenido como utilidad para futuros usos.
        DwellerNPC[] allDwellers = FindObjectsOfType<DwellerNPC>();
        for (int i = 0; i < allDwellers.Length; i++)
        {
            if (allDwellers[i] != null && allDwellers[i].IsDead)
            {
                allDwellers[i].Revive();
            }
        }
    }

    public bool IsGameOver()
    {
        return _isGameOver;
    }

    [ContextMenu("Force Game Over")]
    public void ContextForceGameOver()
    {
        ForceGameOver();
    }

    [ContextMenu("Force Restart")]
    public void ContextForceRestart()
    {
        ForceRestart();
    }

    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log("=== GAME OVER MANAGER STATUS ===");
        Debug.Log($"Game Over activo: {_isGameOver}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"Panel asignado: {gameOverPanel != null}");
        Debug.Log($"Panel activo: {gameOverPanel != null && gameOverPanel.activeInHierarchy}");
        Debug.Log($"ResourceManager existe: {ResourceManager.Instance != null}");
    }
}