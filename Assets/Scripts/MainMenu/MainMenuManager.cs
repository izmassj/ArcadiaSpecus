using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal + settings.
/// Mantiene métodos públicos usados por botones del inspector.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject unsavedChangesPanel;
    public GameObject helpPanel;
    public GameObject creditsPanel;

    [Header("Settings UI")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] _resolutions;
    private GameSettingsService.SettingsSnapshot _tempSettings;

    private void Awake()
    {
        SceneFlowManager.EnsureInstance();
    }

    private void Start()
    {
        SafeSetActive(settingsPanel, false);
        SafeSetActive(unsavedChangesPanel, false);
        SafeSetActive(helpPanel, false);
        SafeSetActive(creditsPanel, false);

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
    }

    // ---------------------------
    //      MENÚ PRINCIPAL
    // ---------------------------

    public void PlayGame()
    {
        // Aquí iría un SFX de botón al integrarlo.
        SceneFlowManager.EnsureInstance().LoadBunker();
    }

    public void OpenSettings()
    {
        SaveTempSettings();
        SafeSetActive(unsavedChangesPanel, false);
        SafeSetActive(settingsPanel, true);
    }

    public void QuitGame()
    {
        // Aquí iría un SFX de botón al integrarlo.
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------------------------
    //      HELP / ABOUT (rúbrica)
    // ---------------------------

    public void OpenHelp()
    {
        SafeSetActive(helpPanel, true);
    }

    public void CloseHelp()
    {
        SafeSetActive(helpPanel, false);
    }

    public void OpenCredits()
    {
        SafeSetActive(creditsPanel, true);
    }

    public void CloseCredits()
    {
        SafeSetActive(creditsPanel, false);
    }

    // ---------------------------
    //      PANEL DE SETTINGS
    // ---------------------------

    public void CloseSettings()
    {
        if (HasUnsavedChanges())
        {
            SafeSetActive(unsavedChangesPanel, true);
            return;
        }

        SafeSetActive(unsavedChangesPanel, false);
        SafeSetActive(settingsPanel, false);
    }

    public void ApplySettings()
    {
        GameSettingsService.SettingsSnapshot currentSettings = GameSettingsService.ReadFromUI(
            musicSlider,
            sfxSlider,
            resolutionDropdown,
            fullscreenToggle);

        GameSettingsService.ApplyRuntime(currentSettings, _resolutions);
        GameSettingsService.SaveToPrefs(currentSettings);

        // Aquí iría la aplicación de volúmenes reales a AudioMixer/FM0D.
        // Por ahora solo se guardan los valores y se aplica resolución/fullscreen.

        SaveTempSettings();
        SafeSetActive(unsavedChangesPanel, false);
        SafeSetActive(settingsPanel, false);
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

    // ---------------------------
    //   UNSAVED CHANGES PANEL
    // ---------------------------

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

        SafeSetActive(unsavedChangesPanel, false);
        SafeSetActive(settingsPanel, false);
    }

    // ---------------------------
    //   SISTEMA DE PREFERENCIAS
    // ---------------------------

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

    private void SafeSetActive(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }
}