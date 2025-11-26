using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject unsavedChangesPanel;
    public GameObject gameOverPanel; // Mantener la referencia pero no usarla para lógica

    [Header("Game Over UI")]
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

    private bool isPaused = false;
    private Resolution[] resolutions;

    private float tempMusic;
    private float tempSFX;
    private int tempResolution;
    private bool tempFullscreen;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("⏸️ Inicializando PauseManager...");

        // Desactivar TODOS los panels al inicio
        ForceDeactivateAllPanels();

        LoadSettings();
        SetupResolutionOptions();
        SaveTempSettings();

        Time.timeScale = 1f;

        // Configurar botones del Game Over (pero la lógica estará en GameOverManager)
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnGoToMainMenu);

        Debug.Log("✅ PauseManager inicializado - Menú de pausa funcional");
    }

    /// <summary>
    /// Fuerza la desactivación de todos los panels al inicio
    /// </summary>
    private void ForceDeactivateAllPanels()
    {
        // PausePanel - debe estar desactivado
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Debug.Log("✅ PausePanel desactivado al inicio");
        }
        else
        {
            Debug.LogError("❌ PausePanel no asignado!");
        }

        // UnsavedChangesPanel - debe estar desactivado
        if (unsavedChangesPanel != null)
        {
            unsavedChangesPanel.SetActive(false);
            Debug.Log("✅ UnsavedChangesPanel desactivado al inicio");
        }

        // GameOverPanel - DEBE estar desactivado (esto soluciona el problema)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🎮 GameOverPanel FORZADO A DESACTIVADO - Solucionado!");
        }
        else
        {
            Debug.LogWarning("⚠️ GameOverPanel no asignado en PauseManager");
        }
    }

    void Update()
    {
        // Verificar si hay Game Over activo (evitar pausa durante Game Over)
        bool isGameOverActive = GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver();

        if (isGameOverActive)
        {
            // Si hay Game Over, no permitir pausa
            if (isPaused)
            {
                // Si estaba pausado, reanudar automáticamente
                ForceResume();
            }
            return;
        }

        // Detección normal de la tecla ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
                PauseGame();
            else
                HandleResume();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Debug.Log("⏸️ Juego pausado - Menú de pausa visible");
        }
    }

    void HandleResume()
    {
        if (HasUnsavedChanges())
        {
            if (unsavedChangesPanel != null)
            {
                unsavedChangesPanel.SetActive(true);
                Debug.Log("💾 Mostrando panel de cambios no guardados");
            }
        }
        else
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Debug.Log("▶️ Juego reanudado desde botón");
        }

        if (unsavedChangesPanel != null)
            unsavedChangesPanel.SetActive(false);
    }

    /// <summary>
    /// Reanudación forzada (sin mostrar UI)
    /// </summary>
    private void ForceResume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (unsavedChangesPanel != null)
            unsavedChangesPanel.SetActive(false);

        Debug.Log("🔄 Reanudación forzada (Game Over activo)");
    }

    /// <summary>
    /// Maneja el botón Reiniciar del Game Over
    /// </summary>
    private void OnRestartGame()
    {
        Debug.Log("🔄 Botón Reiniciar presionado desde PauseManager");

        // Delegar al GameOverManager si existe
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ForceRestart();
        }
        else
        {
            Debug.LogWarning("⚠️ GameOverManager no encontrado, reiniciando directamente...");
            RestartGameDirectly();
        }
    }

    /// <summary>
    /// Maneja el botón Menú Principal del Game Over
    /// </summary>
    private void OnGoToMainMenu()
    {
        Debug.Log("🏠 Botón Menú Principal presionado desde PauseManager");

        // Delegar al GameOverManager si existe
        if (GameOverManager.Instance != null)
        {
            // GameOverManager manejará esto
        }
        else
        {
            GoToMainMenuDirectly();
        }
    }

    /// <summary>
    /// Reinicio directo (fallback)
    /// </summary>
    private void RestartGameDirectly()
    {
        Time.timeScale = 1f;

        // Desactivar GameOverPanel si está activo
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Reiniciar recursos
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetGameOver();
            ResourceManager.Instance.ResetAllResources();
        }

        Debug.Log("🔄 Reinicio directo ejecutado");
    }

    /// <summary>
    /// Ir al menú principal directamente (fallback)
    /// </summary>
    private void GoToMainMenuDirectly()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ========== MÉTODOS DE CONFIGURACIÓN (MANTENER) ==========

    public void ApplySettings()
    {
        if (resolutionDropdown != null && fullscreenToggle != null)
        {
            Screen.SetResolution(
                resolutions[resolutionDropdown.value].width,
                resolutions[resolutionDropdown.value].height,
                fullscreenToggle.isOn
            );
        }

        PlayerPrefs.SetFloat("MusicVol", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVol", sfxSlider.value);

        if (resolutionDropdown != null)
            PlayerPrefs.SetInt("ResIndex", resolutionDropdown.value);

        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
        SaveTempSettings();

        if (unsavedChangesPanel != null)
            unsavedChangesPanel.SetActive(false);

        ResumeGame();
    }

    public void DefaultSettings()
    {
        if (musicSlider != null) musicSlider.value = 0.7f;
        if (sfxSlider != null) sfxSlider.value = 0.7f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (resolutionDropdown != null) resolutionDropdown.value = 0;
    }

    public void ExitGame()
    {
        Debug.Log("🚪 Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void LoadSettings()
    {
        if (musicSlider != null)
            musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 0.7f);

        if (sfxSlider != null)
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 0.7f);

        if (resolutionDropdown != null)
            resolutionDropdown.value = PlayerPrefs.GetInt("ResIndex", 0);

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }

    void SetupResolutionOptions()
    {
        resolutions = Screen.resolutions;
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            foreach (var res in resolutions)
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(res.width + "x" + res.height));
        }
    }

    bool HasUnsavedChanges()
    {
        if (musicSlider == null || sfxSlider == null || resolutionDropdown == null || fullscreenToggle == null)
            return false;

        return musicSlider.value != tempMusic ||
               sfxSlider.value != tempSFX ||
               resolutionDropdown.value != tempResolution ||
               fullscreenToggle.isOn != tempFullscreen;
    }

    void SaveTempSettings()
    {
        if (musicSlider != null) tempMusic = musicSlider.value;
        if (sfxSlider != null) tempSFX = sfxSlider.value;
        if (resolutionDropdown != null) tempResolution = resolutionDropdown.value;
        if (fullscreenToggle != null) tempFullscreen = fullscreenToggle.isOn;
    }

    public void OnUnsavedYes()
    {
        ApplySettings();
    }

    public void OnUnsavedNo()
    {
        if (musicSlider != null) musicSlider.value = tempMusic;
        if (sfxSlider != null) sfxSlider.value = tempSFX;
        if (resolutionDropdown != null) resolutionDropdown.value = tempResolution;
        if (fullscreenToggle != null) fullscreenToggle.isOn = tempFullscreen;

        if (unsavedChangesPanel != null)
            unsavedChangesPanel.SetActive(false);

        ResumeGame();
    }

    /// <summary>
    /// Verifica el estado del PauseManager
    /// </summary>
    [ContextMenu("📊 Debug Estado PauseManager")]
    public void DebugPauseStatus()
    {
        Debug.Log("=== PAUSE MANAGER STATUS ===");
        Debug.Log($"¿Juego pausado?: {isPaused}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"PausePanel activo: {pausePanel != null && pausePanel.activeInHierarchy}");
        Debug.Log($"GameOverPanel activo: {gameOverPanel != null && gameOverPanel.activeInHierarchy}");
        Debug.Log($"GameOverManager existe: {GameOverManager.Instance != null}");

        if (GameOverManager.Instance != null)
        {
            Debug.Log($"¿Game Over activo?: {GameOverManager.Instance.IsGameOver()}");
        }
    }

    void OnDestroy()
    {
        Debug.Log("🗑️ PauseManager destruido");
    }
}