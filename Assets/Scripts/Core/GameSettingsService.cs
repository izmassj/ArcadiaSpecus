using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Servicio estático para gestionar settings globales del juego.
/// Centraliza PlayerPrefs, resolución, fullscreen y utilidades de UI.
/// </summary>
public static class GameSettingsService
{
    public const string MusicVolKey = "MusicVol";
    public const string SfxVolKey = "SFXVol";
    public const string ResIndexKey = "ResIndex";
    public const string FullscreenKey = "Fullscreen";

    public const float DefaultMusicVolume = 0.7f;
    public const float DefaultSfxVolume = 0.7f;
    public const int DefaultResolutionIndex = 0;
    public const bool DefaultFullscreen = true;

    public struct SettingsSnapshot
    {
        public float musicVolume;
        public float sfxVolume;
        public int resolutionIndex;
        public bool fullscreen;
    }

    public static SettingsSnapshot GetDefaultSettings()
    {
        return new SettingsSnapshot
        {
            musicVolume = DefaultMusicVolume,
            sfxVolume = DefaultSfxVolume,
            resolutionIndex = DefaultResolutionIndex,
            fullscreen = DefaultFullscreen
        };
    }

    public static SettingsSnapshot LoadFromPrefs()
    {
        return new SettingsSnapshot
        {
            musicVolume = PlayerPrefs.GetFloat(MusicVolKey, DefaultMusicVolume),
            sfxVolume = PlayerPrefs.GetFloat(SfxVolKey, DefaultSfxVolume),
            resolutionIndex = PlayerPrefs.GetInt(ResIndexKey, DefaultResolutionIndex),
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, DefaultFullscreen ? 1 : 0) == 1
        };
    }

    public static void SaveToPrefs(SettingsSnapshot settings)
    {
        PlayerPrefs.SetFloat(MusicVolKey, settings.musicVolume);
        PlayerPrefs.SetFloat(SfxVolKey, settings.sfxVolume);
        PlayerPrefs.SetInt(ResIndexKey, settings.resolutionIndex);
        PlayerPrefs.SetInt(FullscreenKey, settings.fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void WriteToUI(
        Slider musicSlider,
        Slider sfxSlider,
        TMP_Dropdown resolutionDropdown,
        Toggle fullscreenToggle,
        SettingsSnapshot settings,
        Resolution[] availableResolutions)
    {
        if (musicSlider != null)
            musicSlider.value = settings.musicVolume;

        if (sfxSlider != null)
            sfxSlider.value = settings.sfxVolume;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = settings.fullscreen;

        if (resolutionDropdown != null && availableResolutions != null && availableResolutions.Length > 0)
        {
            int clampedIndex = Mathf.Clamp(settings.resolutionIndex, 0, availableResolutions.Length - 1);
            resolutionDropdown.value = clampedIndex;
            resolutionDropdown.RefreshShownValue();
        }
    }

    public static SettingsSnapshot ReadFromUI(
        Slider musicSlider,
        Slider sfxSlider,
        TMP_Dropdown resolutionDropdown,
        Toggle fullscreenToggle)
    {
        SettingsSnapshot defaults = GetDefaultSettings();

        return new SettingsSnapshot
        {
            musicVolume = musicSlider != null ? musicSlider.value : defaults.musicVolume,
            sfxVolume = sfxSlider != null ? sfxSlider.value : defaults.sfxVolume,
            resolutionIndex = resolutionDropdown != null ? resolutionDropdown.value : defaults.resolutionIndex,
            fullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : defaults.fullscreen
        };
    }

    public static bool HasChanges(SettingsSnapshot a, SettingsSnapshot b, float floatTolerance = 0.0001f)
    {
        return Mathf.Abs(a.musicVolume - b.musicVolume) > floatTolerance
               || Mathf.Abs(a.sfxVolume - b.sfxVolume) > floatTolerance
               || a.resolutionIndex != b.resolutionIndex
               || a.fullscreen != b.fullscreen;
    }

    /// <summary>
    /// Rellena el TMP_Dropdown con resoluciones únicas (por ancho x alto).
    /// Devuelve el array final de resoluciones a usar luego al aplicar.
    /// </summary>
    public static Resolution[] PopulateResolutionDropdown(TMP_Dropdown dropdown)
    {
        Resolution[] rawResolutions = Screen.resolutions;
        if (rawResolutions == null || rawResolutions.Length == 0)
        {
            if (dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(new List<string> { $"{Screen.currentResolution.width}x{Screen.currentResolution.height}" });
            }

            return new[] { Screen.currentResolution };
        }

        List<Resolution> uniqueResolutions = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < rawResolutions.Length; i++)
        {
            Resolution res = rawResolutions[i];
            string key = $"{res.width}x{res.height}";

            if (seen.Contains(key))
                continue;

            seen.Add(key);
            uniqueResolutions.Add(res);
        }

        if (dropdown != null)
        {
            dropdown.ClearOptions();

            List<string> options = new List<string>(uniqueResolutions.Count);
            for (int i = 0; i < uniqueResolutions.Count; i++)
            {
                Resolution res = uniqueResolutions[i];
                options.Add($"{res.width}x{res.height}");
            }

            dropdown.AddOptions(options);
        }

        return uniqueResolutions.ToArray();
    }

    public static void ApplyResolution(Resolution[] availableResolutions, int resolutionIndex, bool fullscreen)
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            // Fallback seguro si algo falla con la lista de resoluciones
            Screen.fullScreen = fullscreen;
            return;
        }

        int clampedIndex = Mathf.Clamp(resolutionIndex, 0, availableResolutions.Length - 1);
        Resolution selected = availableResolutions[clampedIndex];

        Screen.SetResolution(selected.width, selected.height, fullscreen);
    }

    /// <summary>
    /// Aplica settings técnicos al runtime (pantalla). Audio real lo conectaréis vosotros luego.
    /// </summary>
    public static void ApplyRuntime(SettingsSnapshot settings, Resolution[] availableResolutions)
    {
        ApplyResolution(availableResolutions, settings.resolutionIndex, settings.fullscreen);

        // Aquí iría la aplicación de volumen real (AudioMixer/FM0D) cuando lo integréis.
        // Ejemplo futuro:
        // mixer.SetFloat("MusicVolume", ConvertSliderToDb(settings.musicVolume));
        // mixer.SetFloat("SFXVolume", ConvertSliderToDb(settings.sfxVolume));
    }
}