using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenuOptionsPanelUI : MonoBehaviour
{
    private const string MasterVolumeKey = "Options.MasterVolume";
    private const string FullscreenKey = "Options.Fullscreen";
    private const string ResolutionWidthKey = "Options.ResolutionWidth";
    private const string ResolutionHeightKey = "Options.ResolutionHeight";

    [Header("Panel")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _openKeyboardBindingsButton;
    [SerializeField] private KeyboardOnlyRebindPanelUI _keyboardRebindPanel;

    [Header("Sound")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private TMP_Text _masterVolumeValueText;
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private string _masterVolumeExposedParameter = "MasterVolume";
    [SerializeField] private bool _fallbackToAudioListenerVolume = true;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullscreenToggle;

    [Header("Behaviour")]
    [SerializeField] private bool _hidePanelOnStart = true;

    private readonly List<ResolutionOption> _resolutionOptions = new List<ResolutionOption>();
    private bool _isRefreshingUI;

    private void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(ClosePanel);

        if (_openKeyboardBindingsButton != null)
            _openKeyboardBindingsButton.onClick.AddListener(OpenKeyboardBindings);

        if (_masterVolumeSlider != null)
            _masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);

        if (_resolutionDropdown != null)
            _resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);

        if (_fullscreenToggle != null)
            _fullscreenToggle.onValueChanged.AddListener(HandleFullscreenChanged);

        if (_hidePanelOnStart && _panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private void Start()
    {
        BuildResolutionDropdown();
        LoadAndApplySavedSettings();
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(ClosePanel);

        if (_openKeyboardBindingsButton != null)
            _openKeyboardBindingsButton.onClick.RemoveListener(OpenKeyboardBindings);

        if (_masterVolumeSlider != null)
            _masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);

        if (_resolutionDropdown != null)
            _resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);

        if (_fullscreenToggle != null)
            _fullscreenToggle.onValueChanged.RemoveListener(HandleFullscreenChanged);
    }

    public void OpenPanel()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        RefreshUIFromCurrentState();
    }

    public void ClosePanel()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    public void OpenKeyboardBindings()
    {
        if (_keyboardRebindPanel != null)
            _keyboardRebindPanel.OpenPanel();
    }

    [ContextMenu("Refresh UI From Current State")]
    public void RefreshUIFromCurrentState()
    {
        _isRefreshingUI = true;

        if (_masterVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            _masterVolumeSlider.SetValueWithoutNotify(volume);
            UpdateVolumeText(volume);
        }

        if (_fullscreenToggle != null)
            _fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        if (_resolutionDropdown != null && _resolutionOptions.Count > 0)
            _resolutionDropdown.SetValueWithoutNotify(GetBestCurrentResolutionIndex());

        _isRefreshingUI = false;
    }

    private void LoadAndApplySavedSettings()
    {
        _isRefreshingUI = true;

        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        ApplyMasterVolume(savedVolume);
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.SetValueWithoutNotify(savedVolume);
        UpdateVolumeText(savedVolume);

        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        if (_fullscreenToggle != null)
            _fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.currentResolution.height);
        int targetIndex = FindResolutionIndex(savedWidth, savedHeight);
        if (targetIndex < 0)
            targetIndex = GetBestCurrentResolutionIndex();

        if (_resolutionDropdown != null && targetIndex >= 0 && targetIndex < _resolutionOptions.Count)
            _resolutionDropdown.SetValueWithoutNotify(targetIndex);

        ApplyResolutionByIndex(targetIndex, fullscreen, persist: false);

        _isRefreshingUI = false;
    }

    private void BuildResolutionDropdown()
    {
        if (_resolutionDropdown == null)
            return;

        _resolutionOptions.Clear();

        Resolution[] resolutions = Screen.resolutions;
        Dictionary<string, ResolutionOption> deduplicated = new Dictionary<string, ResolutionOption>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution resolution = resolutions[i];
            string key = $"{resolution.width}x{resolution.height}";

            ResolutionOption option = new ResolutionOption(resolution.width, resolution.height, (float) resolution.refreshRateRatio.value);

            if (!deduplicated.TryGetValue(key, out ResolutionOption existing))
            {
                deduplicated.Add(key, option);
                continue;
            }

            if (option.refreshRate > existing.refreshRate)
                deduplicated[key] = option;
        }

        _resolutionOptions.AddRange(deduplicated.Values);
        _resolutionOptions.Sort((a, b) =>
        {
            int widthCompare = a.width.CompareTo(b.width);
            if (widthCompare != 0)
                return widthCompare;

            return a.height.CompareTo(b.height);
        });

        List<string> optionTexts = new List<string>(_resolutionOptions.Count);
        for (int i = 0; i < _resolutionOptions.Count; i++)
        {
            ResolutionOption option = _resolutionOptions[i];
            optionTexts.Add($"{option.width} x {option.height}");
        }

        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.AddOptions(optionTexts);
    }

    private void HandleMasterVolumeChanged(float value)
    {
        if (_isRefreshingUI)
            return;

        ApplyMasterVolume(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
        UpdateVolumeText(value);
    }

    private void HandleResolutionChanged(int dropdownIndex)
    {
        if (_isRefreshingUI)
            return;

        bool fullscreen = _fullscreenToggle != null ? _fullscreenToggle.isOn : Screen.fullScreen;
        ApplyResolutionByIndex(dropdownIndex, fullscreen, persist: true);
    }

    private void HandleFullscreenChanged(bool isFullscreen)
    {
        if (_isRefreshingUI)
            return;

        int index = _resolutionDropdown != null ? _resolutionDropdown.value : GetBestCurrentResolutionIndex();
        ApplyResolutionByIndex(index, isFullscreen, persist: true);
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyMasterVolume(float value)
    {
        if (_audioMixer != null && !string.IsNullOrWhiteSpace(_masterVolumeExposedParameter))
        {
            float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
            _audioMixer.SetFloat(_masterVolumeExposedParameter, decibels);
            return;
        }

        if (_fallbackToAudioListenerVolume)
            AudioListener.volume = value;
    }

    private void UpdateVolumeText(float value)
    {
        if (_masterVolumeValueText != null)
            _masterVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void ApplyResolutionByIndex(int index, bool fullscreen, bool persist)
    {
        if (_resolutionOptions.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, _resolutionOptions.Count - 1);
        ResolutionOption option = _resolutionOptions[index];
        Screen.SetResolution(option.width, option.height, fullscreen);

        if (!persist)
            return;

        PlayerPrefs.SetInt(ResolutionWidthKey, option.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, option.height);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < _resolutionOptions.Count; i++)
        {
            if (_resolutionOptions[i].width == width && _resolutionOptions[i].height == height)
                return i;
        }

        return -1;
    }

    private int GetBestCurrentResolutionIndex()
    {
        int currentWidth = Screen.width;
        int currentHeight = Screen.height;

        int exact = FindResolutionIndex(currentWidth, currentHeight);
        if (exact >= 0)
            return exact;

        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < _resolutionOptions.Count; i++)
        {
            int dw = Mathf.Abs(_resolutionOptions[i].width - currentWidth);
            int dh = Mathf.Abs(_resolutionOptions[i].height - currentHeight);
            int distance = dw + dh;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    [Serializable]
    private struct ResolutionOption
    {
        public int width;
        public int height;
        public float refreshRate;

        public ResolutionOption(int width, int height, float refreshRate)
        {
            this.width = width;
            this.height = height;
            this.refreshRate = refreshRate;
        }
    }
}
