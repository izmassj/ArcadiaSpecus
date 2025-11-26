using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject ballObject;          // Referencia a la pelota

    [Header("UI References")]
    public Canvas pauseCanvas;             // Canvas del menú de pausa
    public Slider musicSlider;             // Control deslizante para música
    public Slider sfxSlider;               // Control deslizante para efectos
    public TMP_Dropdown resolutionDropdown;// Lista de resoluciones
    public Toggle fullscreenToggle;        // Toggle para pantalla completa

    [Header("Audio")]
    public AudioSource musicSource;        // Fuente de audio para música
    public AudioSource sfxSource;          // Fuente de audio para efectos

    [Header("Dialog")]
    public GameObject unsavedChangesDialog;// Diálogo de cambios no guardados
    public Button unsavedYesButton;        // Botón Sí del diálogo
    public Button unsavedNoButton;         // Botón No del diálogo

    private bool isPaused = false;         // Estado de pausa del juego
    private List<Resolution> filteredResolutions; // Lista de resoluciones disponibles
    private BouncingBall bouncingBall;     // Script de la pelota

    // Variables temporales para guardar configuraciones
    private float tempMusicVolume;
    private float tempSFXVolume;
    private int tempResolutionIndex;
    private bool tempFullscreen;

    // Se ejecuta al iniciar el juego
    void Start()
    {
        FindMissingReferences();   // Busca referencias faltantes
        SetupUI();                 // Configura la interfaz de usuario
        SetupDialogButtons();      // Configura botones del diálogo
        LoadSettings();            // Carga configuraciones guardadas
        SaveTempSettings();        // Guarda configuraciones actuales como temporales
        HideMenuImmediately();     // Oculta el menú de pausa
    }

    // Busca automáticamente objetos si no están asignados
    void FindMissingReferences()
    {
        // Busca la pelota por nombre si no está asignada
        if (ballObject == null)
            ballObject = GameObject.Find("Ball");

        // Obtiene el script BouncingBall de la pelota
        if (ballObject != null && bouncingBall == null)
            bouncingBall = ballObject.GetComponent<BouncingBall>();

        // Busca el canvas de pausa por nombre
        if (pauseCanvas == null)
        {
            GameObject canvasObj = GameObject.Find("PauseCanvas");
            if (canvasObj != null)
                pauseCanvas = canvasObj.GetComponent<Canvas>();
        }

        // Busca el diálogo de cambios no guardados
        if (unsavedChangesDialog == null)
            unsavedChangesDialog = GameObject.Find("UnsavedChangesDialog");
    }

    // Configura todos los elementos de la interfaz de usuario
    void SetupUI()
    {
        // Configura el dropdown de resoluciones
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();  // Limpia opciones existentes
            filteredResolutions = new List<Resolution>();
            List<string> options = new List<string>();

            // Recorre todas las resoluciones disponibles
            foreach (Resolution res in Screen.resolutions)
            {
                string option = res.width + " x " + res.height;
                // Evita duplicados
                if (!options.Contains(option))
                {
                    options.Add(option);
                    filteredResolutions.Add(res);
                }
            }

            // Añade las opciones al dropdown
            resolutionDropdown.AddOptions(options);
            SetCurrentResolutionInDropdown();  // Selecciona la resolución actual
        }

        // Asigna funciones a los controles deslizantes
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Configura el toggle de pantalla completa
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;

        // Oculta el diálogo de cambios no guardados
        if (unsavedChangesDialog != null)
            unsavedChangesDialog.SetActive(false);
    }

    // Configura los botones del diálogo de cambios no guardados
    void SetupDialogButtons()
    {
        if (unsavedYesButton != null)
            unsavedYesButton.onClick.AddListener(OnUnsavedChangesYes);

        if (unsavedNoButton != null)
            unsavedNoButton.onClick.AddListener(OnUnsavedChangesNo);
    }

    // Se ejecuta cuando cambia el volumen de música
    void OnMusicVolumeChanged(float value)
    {
        if (musicSource != null)
            musicSource.volume = value;  // Ajusta volumen en tiempo real
    }

    // Se ejecuta cuando cambia el volumen de efectos
    void OnSFXVolumeChanged(float value)
    {
        if (sfxSource != null)
            sfxSource.volume = value;  // Ajusta volumen en tiempo real
    }

    // Se ejecuta en cada frame del juego
    void Update()
    {
        // Si se presiona ESC, pausa o reanuda el juego
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    // Alterna entre pausado y reanudado
    void TogglePause()
    {
        if (!isPaused)
        {
            PauseGame();  // Si no está pausado, pausa
        }
        else
        {
            // Si hay cambios sin guardar, muestra diálogo
            if (HasUnsavedChanges())
            {
                ShowUnsavedChangesDialog();
            }
            else
            {
                ResumeGame();  // Si no hay cambios, reanuda directamente
            }
        }
    }

    // Pausa el juego
    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0;  // Congela el tiempo del juego

        // Pausa la pelota
        if (bouncingBall != null)
            bouncingBall.SetPaused(true);

        SaveTempSettings();  // Guarda configuraciones actuales
        ShowPauseMenu();     // Muestra el menú de pausa
    }

    // Reanuda el juego
    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1;  // Restaura el tiempo normal

        // Reanuda la pelota
        if (bouncingBall != null)
            bouncingBall.SetPaused(false);

        HidePauseMenu();  // Oculta el menú de pausa
    }

    // Muestra el menú de pausa con animación
    void ShowPauseMenu()
    {
        if (pauseCanvas != null)
        {
            pauseCanvas.gameObject.SetActive(true);
            StartCoroutine(FadeInMenu());  // Inicia animación de entrada
        }
    }

    // Oculta el menú de pausa con animación
    void HidePauseMenu()
    {
        if (pauseCanvas != null)
        {
            StartCoroutine(FadeOutMenu());  // Inicia animación de salida
        }
    }

    // Animación de entrada suave del menú
    IEnumerator FadeInMenu()
    {
        // Crea un componente para controlar la transparencia
        CanvasGroup tempCanvasGroup = pauseCanvas.gameObject.AddComponent<CanvasGroup>();
        tempCanvasGroup.alpha = 0;  // Empieza invisible

        float duration = 0.3f;  // Duración de la animación
        float elapsed = 0f;     // Tiempo transcurrido

        // Bucle de animación
        while (elapsed < duration)
        {
            // Interpola suavemente la transparencia
            tempCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;  // Usa tiempo real (no afectado por pausa)
            yield return null;  // Espera al siguiente frame
        }

        tempCanvasGroup.alpha = 1;  // Asegura que termine completamente visible
        Destroy(tempCanvasGroup);   // Limpia el componente temporal
    }

    // Animación de salida suave del menú
    IEnumerator FadeOutMenu()
    {
        CanvasGroup tempCanvasGroup = pauseCanvas.gameObject.AddComponent<CanvasGroup>();
        tempCanvasGroup.alpha = 1;  // Empieza visible

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            tempCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        tempCanvasGroup.alpha = 0;                    // Asegura que termine invisible
        pauseCanvas.gameObject.SetActive(false);      // Desactiva el canvas
        Destroy(tempCanvasGroup);                     // Limpia el componente temporal
    }

    // Reproduce un efecto de sonido aleatorio
    public void PlayRandomSFX()
    {
        if (sfxSource != null)
            sfxSource.Play();
    }

    // Aplica las configuraciones actuales
    public void ApplySettings()
    {
        // Cambia la resolución si hay una seleccionada
        if (resolutionDropdown != null && resolutionDropdown.value < filteredResolutions.Count)
        {
            Resolution selected = filteredResolutions[resolutionDropdown.value];
            Screen.SetResolution(selected.width, selected.height, fullscreenToggle.isOn);
        }

        // Aplica pantalla completa
        if (fullscreenToggle != null)
            Screen.fullScreen = fullscreenToggle.isOn;

        SaveSettings();      // Guarda en PlayerPrefs
        SaveTempSettings();  // Actualiza temporales
    }

    // Restablece configuraciones a valores por defecto
    public void ResetToDefault()
    {
        // Valores por defecto
        if (musicSlider != null) musicSlider.value = 0.7f;
        if (sfxSlider != null) sfxSlider.value = 0.7f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;

        // Busca resolución 1920x1080 por defecto
        if (resolutionDropdown != null)
        {
            for (int i = 0; i < filteredResolutions.Count; i++)
            {
                if (filteredResolutions[i].width == 1920)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
        }

        ApplySettings();  // Aplica los valores por defecto
    }

    // Muestra el diálogo de cambios no guardados
    void ShowUnsavedChangesDialog()
    {
        if (unsavedChangesDialog != null)
            unsavedChangesDialog.SetActive(true);
    }

    // Se ejecuta al presionar "Sí" en el diálogo
    public void OnUnsavedChangesYes()
    {
        ApplySettings();  // Guarda los cambios
        if (unsavedChangesDialog != null)
            unsavedChangesDialog.SetActive(false);  // Cierra diálogo
        ResumeGame();     // Reanuda el juego
    }

    // Se ejecuta al presionar "No" en el diálogo
    public void OnUnsavedChangesNo()
    {
        RevertToTempSettings();  // Descarta los cambios
        if (unsavedChangesDialog != null)
            unsavedChangesDialog.SetActive(false);  // Cierra diálogo
        ResumeGame();            // Reanuda el juego
    }

    // Oculta el menú inmediatamente (sin animación)
    void HideMenuImmediately()
    {
        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);
    }

    // Selecciona la resolución actual en el dropdown
    void SetCurrentResolutionInDropdown()
    {
        if (resolutionDropdown == null) return;

        Resolution current = Screen.currentResolution;
        // Busca la resolución actual en la lista
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            if (filteredResolutions[i].width == current.width &&
                filteredResolutions[i].height == current.height)
            {
                resolutionDropdown.value = i;  // Selecciona la opción
                break;
            }
        }
    }

    // Guarda las configuraciones actuales como temporales
    void SaveTempSettings()
    {
        tempMusicVolume = musicSlider != null ? musicSlider.value : 0.7f;
        tempSFXVolume = sfxSlider != null ? sfxSlider.value : 0.7f;
        tempResolutionIndex = resolutionDropdown != null ? resolutionDropdown.value : 0;
        tempFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : true;
    }

    // Restaura las configuraciones temporales
    void RevertToTempSettings()
    {
        if (musicSlider != null) musicSlider.value = tempMusicVolume;
        if (sfxSlider != null) sfxSlider.value = tempSFXVolume;
        if (resolutionDropdown != null) resolutionDropdown.value = tempResolutionIndex;
        if (fullscreenToggle != null) fullscreenToggle.isOn = tempFullscreen;
    }

    // Verifica si hay cambios sin guardar
    bool HasUnsavedChanges()
    {
        // Compara cada valor actual con su temporal
        return (musicSlider != null && musicSlider.value != tempMusicVolume) ||
               (sfxSlider != null && sfxSlider.value != tempSFXVolume) ||
               (resolutionDropdown != null && resolutionDropdown.value != tempResolutionIndex) ||
               (fullscreenToggle != null && fullscreenToggle.isOn != tempFullscreen);
    }

    // Guarda las configuraciones permanentemente
    void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicSlider != null ? musicSlider.value : 0.7f);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider != null ? sfxSlider.value : 0.7f);
        PlayerPrefs.SetInt("Resolution", resolutionDropdown != null ? resolutionDropdown.value : 0);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle != null && fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();  // Guarda en disco
    }

    // Carga las configuraciones guardadas
    void LoadSettings()
    {
        // Obtiene valores guardados o usa valores por defecto
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        int resIndex = PlayerPrefs.GetInt("Resolution", 0);
        int fullscreen = PlayerPrefs.GetInt("Fullscreen", 1);

        // Aplica los valores cargados
        if (musicSource != null) musicSource.volume = musicVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;
        if (musicSlider != null) musicSlider.value = musicVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;
        if (resolutionDropdown != null && resIndex < filteredResolutions.Count)
            resolutionDropdown.value = resIndex;
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = fullscreen == 1;

        // Aplica resolución y pantalla completa
        if (resIndex < filteredResolutions.Count)
        {
            Resolution res = filteredResolutions[resIndex];
            Screen.SetResolution(res.width, res.height, fullscreen == 1);
        }
    }
}