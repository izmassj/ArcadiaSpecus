using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject unsavedChangesPanel;

    [Header("Settings UI")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;

    private float tempMusic;
    private float tempSFX;
    private int tempResolution;
    private bool tempFullscreen;

    void Start()
    {
        settingsPanel.SetActive(false);
        unsavedChangesPanel.SetActive(false);

        LoadSettings();
        SetupResolutionOptions();
        SaveTempSettings();
    }

    // ---------------------------
    //      MENÚ PRINCIPAL
    // ---------------------------

    public void PlayGame()
    {
        SceneManager.LoadScene("ResourcesManagement"); // Cambia al nombre real de tu escena
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        SaveTempSettings();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // ---------------------------
    //      PANEL DE SETTINGS
    // ---------------------------

    public void CloseSettings()
    {
        if (HasUnsavedChanges())
        {
            unsavedChangesPanel.SetActive(true);
        }
        else
        {
            settingsPanel.SetActive(false);
        }
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
        settingsPanel.SetActive(false);
    }

    public void DefaultSettings()
    {
        musicSlider.value = 0.7f;
        sfxSlider.value = 0.7f;
        fullscreenToggle.isOn = true;
        resolutionDropdown.value = 0;
    }

    // ---------------------------
    //   UNSAVED CHANGES PANEL
    // ---------------------------

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
        settingsPanel.SetActive(false);
    }

    // ---------------------------
    //   SISTEMA DE PREFERENCIAS
    // ---------------------------

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
}
