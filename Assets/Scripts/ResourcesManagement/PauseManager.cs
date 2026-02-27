using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menú de pausa + settings en juego.
/// Migrado a New Input System (sin Input.GetKey / Input.GetButton).
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject unsavedChangesPanel;
    public GameObject gameOverPanel; // Se mantiene referencia por compatibilidad visual, no lógica principal.

    [Header("Game Over UI (fallback legacy si faltara GameOverManager)")]
    public TextMeshProUGUI gameOverTitle;
    public TextMeshProUGUI gameOverDescription;
    public TextMeshProUGUI gameOverStats;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Resolution")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Input (New Input System)")]
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private string _pauseActionName = "Pause";
    [SerializeField] private bool _allowKeyboardEscapeFallback = true;
    [SerializeField] private bool _allowGamepadStartFallback = true;

    private bool _isPaused;
    private bool _legacyGameOverFallbackListenersAdded;

    private Resolution[] _resolutions;
    private GameSettingsService.SettingsSnapshot _tempSettings;

    private InputAction _pauseAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (_playerInput == null)
            _playerInput = FindObjectOfType<PlayerInput>();
    }

    private void Start()
    {
        SceneFlowManager.EnsureInstance();
        InputDeviceTracker.EnsureInstance();

        ForceDeactivateAllPanels();
        CachePauseAction();

        SetupResolutionOptions();

        GameSettingsService.SettingsSnapshot savedSettings = GameSettingsService.LoadFromPrefs();
        GameSettingsService.WriteToUI(
            musicSlider,
            sfxSlider,
            resolutionDropdown,
            fullscreenToggle,
            savedSettings,
            _resolutions);

        SaveTempSettings();

        Time.timeScale = 1f;

        ConfigureLegacyGameOverButtonFallbackIfNeeded();
    }

    private void OnEnable()
    {
        CachePauseAction();
    }

    private void OnDisable()
    {
        // No hay suscripciones persistentes a acciones en este script (se consulta por polling).
    }

    private void Update()
    {
        if (IsGameOverActive())
        {
            if (_isPaused)
                ForceResume();

            return;
        }

        if (WasPausePressedThisFrame())
        {
            if (!_isPaused)
                PauseGame();
            else
                HandleResumeRequest();
        }
    }

    private void CachePauseAction()
    {
        if (_playerInput == null)
            _playerInput = FindObjectOfType<PlayerInput>();

        _pauseAction = null;

        if (_playerInput == null || _playerInput.actions == null)
            return;

        _pauseAction = _playerInput.actions.FindAction(_pauseActionName, throwIfNotFound: false);

        // Carga remapeos guardados (base para rúbrica UX "Bé")
        InputBindingSaveManager.LoadBindingOverrides(_playerInput);
    }

    /// <summary>
    /// Desactiva paneles al iniciar para evitar estados rotos al cargar escena.
    /// </summary>
    private void ForceDeactivateAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (unsavedChangesPanel != null) unsavedChangesPanel.SetActive(false);

        // Ojo: este panel se mantiene oculto aquí; la lógica real de Game Over la lleva GameOverManager.
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private bool WasPausePressedThisFrame()
    {
        if (_pauseAction != null && _pauseAction.enabled && _pauseAction.WasPressedThisFrame())
            return true;

        // Fallbacks usando New Input System (no InputManager antiguo)
        if (_allowKeyboardEscapeFallback && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;

        if (_allowGamepadStartFallback && Gamepad.current != null)
        {
            if (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private bool IsGameOverActive()
    {
        return GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver();
    }

    private void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        // Aquí iría SFX/UI animation de apertura de pausa.
    }

    private void HandleResumeRequest()
    {
        if (HasUnsavedChanges())
        {
            if (unsavedChangesPanel != null)
                unsavedChangesPanel.SetActive(true);

            return;
        }

        ResumeGame();
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (unsavedChangesPanel != null)
            unsavedChangesPanel.SetActive(false);

        // Aquí iría SFX/UI animation de cierre de pausa.
    }

    private void ForceResume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (unsavedChangesPanel != null) unsavedChangesPanel.SetActive(false);
    }

    public bool IsPaused()
    {
        return _isPaused;
    }

    // =========================================================
    // FALLBACK LEGACY PARA BOTONES DE GAME OVER (si faltara manager)
    // =========================================================

    private void ConfigureLegacyGameOverButtonFallbackIfNeeded()
    {
        // Si existe GameOverManager, él configura sus botones y no añadimos listeners duplicados.
        if (GameOverManager.Instance != null)
            return;

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartGameFallback);
            restartButton.onClick.AddListener(OnRestartGameFallback);
            _legacyGameOverFallbackListenersAdded = true;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnGoToMainMenuFallback);
            mainMenuButton.onClick.AddListener(OnGoToMainMenuFallback);
            _legacyGameOverFallbackListenersAdded = true;
        }
    }

    private void OnRestartGameFallback()
    {
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ForceRestart();
            return;
        }

        RestartGameDirectly();
    }

    private void OnGoToMainMenuFallback()
    {
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.GoToMainMenu();
            return;
        }

        GoToMainMenuDirectly();
    }

    private void RestartGameDirectly()
    {
        Time.timeScale = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetGameOver();
            ResourceManager.Instance.ResetAllResources();
        }

        SceneFlowManager.EnsureInstance().RestartCurrentScene();
    }

    private void GoToMainMenuDirectly()
    {
        Time.timeScale = 1f;
        SceneFlowManager.EnsureInstance().LoadMainMenu();
    }

    // =========================================================
    // SETTINGS (se mantienen métodos públicos para botones UI)
    // =========================================================

    public void ApplySettings()
    {
        GameSettingsService.SettingsSnapshot currentSettings = GameSettingsService.ReadFromUI(
            musicSlider,
            sfxSlider,
            resolutionDropdown,
            fullscreenToggle);

        GameSettingsService.ApplyRuntime(currentSettings, _resolutions);
        GameSettingsService.SaveToPrefs(currentSettings);

        SaveTempSettings();

        if (unsavedChangesPanel != null)
            unsavedChangesPanel.SetActive(false);

        ResumeGame();
    }

    public void DefaultSettings()
    {
        GameSettingsService.SettingsSnapshot defaults = GameSettingsService.GetDefaultSettings();

        GameSettingsService.WriteToUI(
            musicSlider,
            sfxSlider,
            resolutionDropdown,
            fullscreenToggle,
            defaults,
            _resolutions);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetupResolutionOptions()
    {
        _resolutions = GameSettingsService.PopulateResolutionDropdown(resolutionDropdown);
    }

    private bool HasUnsavedChanges()
    {
        GameSettingsService.SettingsSnapshot currentSettings = GameSettingsService.ReadFromUI(
            musicSlider,
            sfxSlider,
            resolutionDropdown,
            fullscreenToggle);

        return GameSettingsService.HasChanges(currentSettings, _tempSettings);
    }

    private void SaveTempSettings()
    {
        _tempSettings = GameSettingsService.ReadFromUI(
            musicSlider,
            sfxSlider,
            resolutionDropdown,
            fullscreenToggle);
    }

    public void OnUnsavedYes()
    {
        ApplySettings();
    }

    public void OnUnsavedNo()
    {
        GameSettingsService.WriteToUI(
            musicSlider,
            sfxSlider,
            resolutionDropdown,
            fullscreenToggle,
            _tempSettings,
            _resolutions);

        if (unsavedChangesPanel != null)
            unsavedChangesPanel.SetActive(false);

        ResumeGame();
    }

    [ContextMenu("Debug Estado PauseManager")]
    public void DebugPauseStatus()
    {
        Debug.Log("=== PAUSE MANAGER STATUS ===");
        Debug.Log($"Pausado: {_isPaused}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"PausePanel activo: {pausePanel != null && pausePanel.activeInHierarchy}");
        Debug.Log($"UnsavedPanel activo: {unsavedChangesPanel != null && unsavedChangesPanel.activeInHierarchy}");
        Debug.Log($"GameOver activo (manager): {IsGameOverActive()}");
        Debug.Log($"PauseAction encontrada: {_pauseAction != null}");
        Debug.Log($"PlayerInput encontrado: {_playerInput != null}");
    }

    private void OnDestroy()
    {
        if (_legacyGameOverFallbackListenersAdded)
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartGameFallback);

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnGoToMainMenuFallback);
        }

        if (Instance == this)
            Instance = null;
    }
}