using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using TMPro;

public class MainMenuRebindUI : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Drag IA_PlayerInputs (InputActionAsset). If empty, it will try to auto-find it.")]
    [SerializeField] private InputActionAsset actions;

    [Tooltip("Action Map name to use. In your project it's 'Gameplay'. Leave empty to use first map.")]
    [SerializeField] private string actionMapName = "Gameplay";

    [Tooltip("If true, shows all actions in the map. If false, uses the list below.")]
    [SerializeField] private bool useAllActionsInMap = true;

    [Tooltip("Actions to show if 'useAllActionsInMap' is false.")]
    [SerializeField] private List<string> rebindableActionNames = new List<string> { "Pause", "Drag" };

    [Header("UI")]
    [SerializeField] private Button keyboardMouseTabButton;
    [SerializeField] private Button gamepadTabButton;
    [SerializeField] private Button resetButton;

    [Tooltip("Where rows are instantiated (ScrollView Content, etc.)")]
    [SerializeField] private Transform bindingsListParent;

    [Tooltip("Your row prefab. Must have RebindRow on root.")]
    [SerializeField] private RebindRow rowPrefab;

    [Header("Device filters")]
    [SerializeField] private bool includeMouseInKeyboardTab = true;

    private enum Tab { KeyboardMouse, Gamepad }
    private Tab _currentTab = Tab.KeyboardMouse;

    private const string PlayerPrefsKey = "InputBindingOverrides";
    private readonly List<GameObject> _spawnedRows = new List<GameObject>();
    private InputActionRebindingExtensions.RebindingOperation _activeRebind;

    private void Awake()
    {
        if (keyboardMouseTabButton != null) keyboardMouseTabButton.onClick.AddListener(ShowKeyboardMouse);
        if (gamepadTabButton != null) gamepadTabButton.onClick.AddListener(ShowGamepad);
        if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);

        AutoAssignActionsIfNeeded();
        LoadOverrides();
        actions?.Enable();

        // Default tab
        _currentTab = Tab.KeyboardMouse;
    }

    private void OnEnable()
    {
        RebuildList();
    }

    private void OnDisable()
    {
        CancelActiveRebind();
    }

    public void ShowKeyboardMouse()
    {
        _currentTab = Tab.KeyboardMouse;
        RebuildList();
    }

    public void ShowGamepad()
    {
        _currentTab = Tab.Gamepad;
        RebuildList();
    }

    public void ResetToDefaults()
    {
        CancelActiveRebind();
        if (actions == null) return;

        actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();

        RebuildList();
    }

    private void AutoAssignActionsIfNeeded()
    {
        if (actions != null) return;

        // Try Resources first (recommended if you keep a copy under Resources/Input/)
        actions = Resources.Load<InputActionAsset>("Input/IA_PlayerInputs");
        if (actions != null) return;

        // Editor/runtime fallback: pick loaded assets with this name
        foreach (var a in Resources.FindObjectsOfTypeAll<InputActionAsset>())
        {
            if (a != null && a.name == "IA_PlayerInputs")
            {
                actions = a;
                return;
            }
        }

        Debug.LogError("[MainMenuRebindUI] No InputActionAsset assigned. Assign IA_PlayerInputs in the inspector.");
    }

    private void LoadOverrides()
    {
        if (actions == null) return;

        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, "");
            if (!string.IsNullOrEmpty(json))
                actions.LoadBindingOverridesFromJson(json);
        }
    }

    private void SaveOverrides()
    {
        if (actions == null) return;

        string json = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }

    private InputActionMap GetMap()
    {
        if (actions == null) return null;

        if (!string.IsNullOrWhiteSpace(actionMapName))
        {
            var map = actions.FindActionMap(actionMapName, throwIfNotFound: false);
            if (map != null) return map;
        }

        return actions.actionMaps.Count > 0 ? actions.actionMaps[0] : null;
    }

    private void RebuildList()
    {
        CancelActiveRebind();
        ClearSpawnedRows();

        if (actions == null || bindingsListParent == null || rowPrefab == null)
            return;

        var map = GetMap();
        if (map == null) return;

        List<InputAction> actionsToShow = new List<InputAction>();
        if (useAllActionsInMap)
        {
            foreach (var a in map.actions) actionsToShow.Add(a);
        }
        else
        {
            foreach (string actionName in rebindableActionNames)
            {
                var a = map.FindAction(actionName, throwIfNotFound: false);
                if (a != null) actionsToShow.Add(a);
            }
        }

        int rowsCreated = 0;
        foreach (var action in actionsToShow)
        {
            // One row per binding that matches the selected device tab
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite) continue;

                if (!BindingMatchesCurrentTab(b))
                    continue;

                CreateRowForBinding(action, i);
                rowsCreated++;
            }

            // If no binding exists for this device tab, show a disabled placeholder row
            if (!ActionHasAnyBindingForTab(action))
            {
                CreateNoBindingRow(action);
                rowsCreated++;
            }
        }

        if (rowsCreated == 0)
        {
            // Fallback message in console (so you don't see "nothing" silently)
            Debug.LogWarning("[MainMenuRebindUI] No bindings were listed. Check your InputActions asset and device filters.");
        }
    }

    private bool ActionHasAnyBindingForTab(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite) continue;
            if (BindingMatchesCurrentTab(b)) return true;
        }
        return false;
    }

    private bool BindingMatchesCurrentTab(InputBinding binding)
    {
        string path = !string.IsNullOrEmpty(binding.effectivePath) ? binding.effectivePath : binding.path;
        if (string.IsNullOrEmpty(path)) return false;

        bool isKb = path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase);
        bool isMouse = path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase);
        bool isPad = path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase);

        if (_currentTab == Tab.Gamepad)
            return isPad;

        // Keyboard/Mouse tab
        if (isKb) return true;
        if (includeMouseInKeyboardTab && isMouse) return true;
        return false;
    }

    private void CreateNoBindingRow(InputAction action)
    {
        var go = Instantiate(rowPrefab.gameObject, bindingsListParent);
        _spawnedRows.Add(go);

        var row = go.GetComponent<RebindRow>();
        if (row == null) return;

        row.actionNameText.text = action.name;
        row.bindingText.text = (_currentTab == Tab.Gamepad)
            ? "No Gamepad binding (add one in IA_PlayerInputs)"
            : "No Keyboard/Mouse binding";

        row.rebindButton.interactable = false;
    }

    private void CreateRowForBinding(InputAction action, int bindingIndex)
    {
        var go = Instantiate(rowPrefab.gameObject, bindingsListParent);
        _spawnedRows.Add(go);

        var row = go.GetComponent<RebindRow>();
        if (row == null) return;

        var binding = action.bindings[bindingIndex];

        string label = action.name;
        if (binding.isPartOfComposite && !string.IsNullOrEmpty(binding.name))
            label += $" ({binding.name})";

        row.actionNameText.text = label;
        row.bindingText.text = GetBindingDisplay(action, bindingIndex);

        row.rebindButton.interactable = true;
        row.rebindButton.onClick.RemoveAllListeners();
        row.rebindButton.onClick.AddListener(() =>
        {
            StartRebind(action, bindingIndex, row.bindingText);
        });
    }

    private string GetBindingDisplay(InputAction action, int bindingIndex)
    {
        try
        {
            return action.GetBindingDisplayString(
                bindingIndex,
                out _,
                out _,
                InputBinding.DisplayStringOptions.DontUseShortDisplayNames
            );
        }
        catch
        {
            var b = action.bindings[bindingIndex];
            string p = !string.IsNullOrEmpty(b.effectivePath) ? b.effectivePath : b.path;
            return InputControlPath.ToHumanReadableString(p, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

    private void StartRebind(InputAction action, int bindingIndex, TMP_Text bindingLabel)
    {
        CancelActiveRebind();
        if (action == null) return;

        action.Disable();
        bindingLabel.text = "Press a key/button... (ESC cancels)";

        _activeRebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnCancel(op =>
            {
                action.Enable();
                bindingLabel.text = GetBindingDisplay(action, bindingIndex);
                op.Dispose();
                _activeRebind = null;
            })
            .OnComplete(op =>
            {
                action.Enable();
                bindingLabel.text = GetBindingDisplay(action, bindingIndex);
                op.Dispose();
                _activeRebind = null;

                SaveOverrides();
                RebuildList();
            });

        // Restrict by tab
        if (_currentTab == Tab.KeyboardMouse)
        {
            _activeRebind.WithControlsExcluding("<Gamepad>/*");
        }
        else
        {
            _activeRebind.WithControlsExcluding("<Keyboard>/*");
            _activeRebind.WithControlsExcluding("<Mouse>/*");
        }

        _activeRebind.Start();
    }

    private void CancelActiveRebind()
    {
        if (_activeRebind != null)
        {
            _activeRebind.Cancel();
            _activeRebind.Dispose();
            _activeRebind = null;
        }
    }

    private void ClearSpawnedRows()
    {
        foreach (var go in _spawnedRows)
        {
            if (go != null) Destroy(go);
        }
        _spawnedRows.Clear();
    }
}
