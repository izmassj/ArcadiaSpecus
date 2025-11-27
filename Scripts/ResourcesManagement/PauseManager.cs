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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (unsavedChangesPanel != null) unsavedChangesPanel.SetActive(false);

        LoadSettings();
        SetupResolutionOptions();
        SaveTempSettings();

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused) PauseGame();
            else HandleResume();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    void HandleResume()
    {
        if (HasUnsavedChanges())
            unsavedChangesPanel.SetActive(true);
        else
            ResumeGame();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        unsavedChangesPanel.SetActive(false);
    }

    public void ApplySettings()
    {
        Screen.SetResolution(
            resolutions[resolutionDropdown.value].width,
            resolutions[resolutionDropdown.value].height,
            fullscreenToggle.isOn
        );

        PlayerPrefs.SetFloat("MusicVol", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVol", sfxSlider.value);
        PlayerPrefs.SetInt("ResIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
        SaveTempSettings();
        unsavedChangesPanel.SetActive(false);
        ResumeGame();
    }

    public void DefaultSettings()
    {
        musicSlider.value = 0.7f;
        sfxSlider.value = 0.7f;
        fullscreenToggle.isOn = true;
        resolutionDropdown.value = 0;
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    void LoadSettings()
    {
        musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 0.7f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 0.7f);
        resolutionDropdown.value = PlayerPrefs.GetInt("ResIndex", 0);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }

    void SetupResolutionOptions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        foreach (var res in resolutions)
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(res.width + "x" + res.height));
    }

    bool HasUnsavedChanges()
    {
        return
            musicSlider.value != tempMusic ||
            sfxSlider.value != tempSFX ||
            resolutionDropdown.value != tempResolution ||
            fullscreenToggle.isOn != tempFullscreen;
    }

    void SaveTempSettings()
    {
        tempMusic = musicSlider.value;
        tempSFX = sfxSlider.value;
        tempResolution = resolutionDropdown.value;
        tempFullscreen = fullscreenToggle.isOn;
    }

    public void OnUnsavedYes()
    {
        ApplySettings();
    }

    public void OnUnsavedNo()
    {
        musicSlider.value = tempMusic;
        sfxSlider.value = tempSFX;
        resolutionDropdown.value = tempResolution;
        fullscreenToggle.isOn = tempFullscreen;

        unsavedChangesPanel.SetActive(false);
        ResumeGame();
    }
}
