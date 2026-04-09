using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyboardOnlyRebindPanelUI : MonoBehaviour
{
    private const string PlayerPrefsPrefix = "KeyboardRebinds.";

    [Header("Panel")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _resetAllButton;
    [SerializeField] private TMP_Text _listeningText;

    [Header("Bindings")]
    [SerializeField] private InputActionAsset[] _inputActionAssets;
    [SerializeField] private Transform _listRoot;
    [SerializeField] private KeyboardOnlyRebindEntryUI _entryPrefab;

    private readonly List<KeyboardOnlyRebindEntryUI> _spawnedEntries = new List<KeyboardOnlyRebindEntryUI>();
    private InputActionRebindingExtensions.RebindingOperation _currentRebind;

    private void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(ClosePanel);

        if (_resetAllButton != null)
            _resetAllButton.onClick.AddListener(ResetAllBindings);

        LoadAllOverrides();

        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        SetListeningText(string.Empty);
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(ClosePanel);

        if (_resetAllButton != null)
            _resetAllButton.onClick.RemoveListener(ResetAllBindings);

        DisposeCurrentRebind();
    }

    public void OpenPanel()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        RefreshList();
    }

    public void ClosePanel()
    {
        DisposeCurrentRebind();
        SetListeningText(string.Empty);

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    [ContextMenu("Refresh Binding List")]
    public void RefreshList()
    {
        ClearList();

        if (_listRoot == null || _entryPrefab == null || _inputActionAssets == null)
            return;

        for (int assetIndex = 0; assetIndex < _inputActionAssets.Length; assetIndex++)
        {
            InputActionAsset asset = _inputActionAssets[assetIndex];
            if (asset == null)
                continue;

            for (int mapIndex = 0; mapIndex < asset.actionMaps.Count; mapIndex++)
            {
                InputActionMap map = asset.actionMaps[mapIndex];
                for (int actionIndex = 0; actionIndex < map.actions.Count; actionIndex++)
                {
                    InputAction action = map.actions[actionIndex];

                    for (int bindingIndex = 0; bindingIndex < action.bindings.Count; bindingIndex++)
                    {
                        InputBinding binding = action.bindings[bindingIndex];

                        if (binding.isComposite)
                            continue;

                        if (!IsKeyboardBinding(binding))
                            continue;

                        KeyboardOnlyRebindEntryUI entry = Instantiate(_entryPrefab, _listRoot);

                        string actionLabel = BuildActionLabel(map, action, binding);
                        string bindingLabel = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);

                        entry.Setup(
                            actionLabel,
                            string.IsNullOrWhiteSpace(bindingLabel) ? "Sin asignar" : bindingLabel,
                            () => StartKeyboardRebind(asset, action, bindingIndex),
                            () => ClearBindingOverride(asset, action, bindingIndex));

                        _spawnedEntries.Add(entry);
                    }
                }
            }
        }
    }

    private void StartKeyboardRebind(InputActionAsset asset, InputAction action, int bindingIndex)
    {
        DisposeCurrentRebind();
        SetListeningText("Pulsa una tecla... ESC para cancelar.");

        bool wasEnabled = action.enabled;
        action.Disable();

        _currentRebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsExcluding("<Mouse>")
            .WithControlsExcluding("<Gamepad>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation =>
            {
                operation.Dispose();
                _currentRebind = null;
                if (wasEnabled)
                    action.Enable();
                SetListeningText(string.Empty);
                RefreshList();
            })
            .OnComplete(operation =>
            {
                operation.Dispose();
                _currentRebind = null;
                if (wasEnabled)
                    action.Enable();
                SaveOverrides(asset);
                SetListeningText(string.Empty);
                RefreshList();
            });

        _currentRebind.Start();
    }

    private void ClearBindingOverride(InputActionAsset asset, InputAction action, int bindingIndex)
    {
        action.RemoveBindingOverride(bindingIndex);
        SaveOverrides(asset);
        RefreshList();
    }

    private void ResetAllBindings()
    {
        DisposeCurrentRebind();

        if (_inputActionAssets == null)
            return;

        for (int i = 0; i < _inputActionAssets.Length; i++)
        {
            InputActionAsset asset = _inputActionAssets[i];
            if (asset == null)
                continue;

            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(GetPlayerPrefsKey(asset));
        }

        PlayerPrefs.Save();
        SetListeningText(string.Empty);
        RefreshList();
    }

    private void LoadAllOverrides()
    {
        if (_inputActionAssets == null)
            return;

        for (int i = 0; i < _inputActionAssets.Length; i++)
        {
            InputActionAsset asset = _inputActionAssets[i];
            if (asset == null)
                continue;

            string key = GetPlayerPrefsKey(asset);
            if (!PlayerPrefs.HasKey(key))
                continue;

            string json = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
                asset.LoadBindingOverridesFromJson(json);
        }
    }

    private void SaveOverrides(InputActionAsset asset)
    {
        if (asset == null)
            return;

        string key = GetPlayerPrefsKey(asset);
        string json = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    private void ClearList()
    {
        for (int i = 0; i < _spawnedEntries.Count; i++)
        {
            if (_spawnedEntries[i] != null)
                Destroy(_spawnedEntries[i].gameObject);
        }

        _spawnedEntries.Clear();
    }

    private void DisposeCurrentRebind()
    {
        if (_currentRebind == null)
            return;

        _currentRebind.Cancel();
        _currentRebind.Dispose();
        _currentRebind = null;
    }

    private void SetListeningText(string value)
    {
        if (_listeningText == null)
            return;

        _listeningText.text = value;
        _listeningText.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
    }

    private string BuildActionLabel(InputActionMap map, InputAction action, InputBinding binding)
    {
        if (binding.isPartOfComposite)
            return map.name + " / " + action.name + " / " + binding.name;

        return map.name + " / " + action.name;
    }

    private bool IsKeyboardBinding(InputBinding binding)
    {
        string path = string.IsNullOrWhiteSpace(binding.overridePath) ? binding.path : binding.overridePath;
        return !string.IsNullOrWhiteSpace(path) && path.Contains("<Keyboard>");
    }

    private string GetPlayerPrefsKey(InputActionAsset asset)
    {
        return PlayerPrefsPrefix + asset.name;
    }
}
