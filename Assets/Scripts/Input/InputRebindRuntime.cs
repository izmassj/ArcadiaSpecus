using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime UI muy simple para remapear inputs (teclado/ratón y mando) usando el New Input System.
/// Cumple la rúbrica de "User experience: remapping de inputs (mando + teclado)".
/// 
/// No depende de prefabs: crea el Canvas por código y guarda overrides en PlayerPrefs
/// usando la misma key que InputBindingSaveManager ("InputBindingOverrides").
/// </summary>
public class InputRebindRuntime : MonoBehaviour
{
    public static InputRebindRuntime Instance { get; private set; }

    private const string OverridesKey = "InputBindingOverrides";

    [Header("Resources (InputActionAsset)")]
    [SerializeField] private string _playerAssetResourcesPath = "Input/IA_PlayerInputs";

    [Header("UI")]
    [SerializeField] private Key _toggleKey = Key.F9;

    private InputActionAsset _uiAsset;
    private Canvas _canvas;
    private GameObject _panelRoot;
    private TextMeshProUGUI _statusText;

    private readonly List<Row> _rows = new List<Row>();
    private InputActionRebindingExtensions.RebindingOperation _activeRebind;

    private class Row
    {
        public string ActionName;
        public InputAction Action;
        public int KeyboardBindingIndex = -1;
        public int GamepadBindingIndex = -1;

        public TextMeshProUGUI KeyboardValue;
        public Button KeyboardButton;

        public TextMeshProUGUI GamepadValue;
        public Button GamepadButton;
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        InputRebindRuntime existing = FindObjectOfType<InputRebindRuntime>(true);
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("InputRebindRuntime");
        Instance = go.AddComponent<InputRebindRuntime>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadAssets();
        ApplySavedOverridesToAssets();
        ApplyOverridesToAllPlayerInputs();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-aplica overrides a cualquier PlayerInput de la escena.
        ApplyOverridesToAllPlayerInputs();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void TryInstallHelpPanelButton(GameObject helpPanel)
    {
        if (helpPanel == null) return;

        // Evitar duplicados
        Transform existing = helpPanel.transform.Find("Btn_Remapping");
        if (existing != null) return;

        // Parent: si hay un layout group en algún hijo, lo usamos. Si no, lo cuelgo del root.
        Transform parent = FindBestLayoutParent(helpPanel.transform) ?? helpPanel.transform;

        GameObject btnGO = new GameObject("Btn_Remapping", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360f, 48f);

        Image img = btnGO.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.65f);

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Show);

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(btnGO.transform, false);

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 6f);
        trt.offsetMax = new Vector2(-12f, -6f);

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = "Remap controls (Keyboard / Gamepad)";
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private Transform FindBestLayoutParent(Transform root)
    {
        // Busca un contenedor típico con VerticalLayoutGroup.
        VerticalLayoutGroup v = root.GetComponentInChildren<VerticalLayoutGroup>(true);
        if (v != null) return v.transform;

        // Si no, el primer RectTransform hijo.
        foreach (Transform child in root)
        {
            if (child is RectTransform) return child;
        }

        return root;
    }

    public void Toggle()
    {
        if (_panelRoot == null || !_panelRoot.activeSelf)
            Show();
        else
            Hide();
    }

    public void Show()
    {
        if (_panelRoot == null)
            BuildUI();

        _panelRoot.SetActive(true);
        RefreshAllRows();
        SetStatus("Select a binding to rebind. Press ESC to cancel.");
    }

    public void Hide()
    {
        CancelActiveRebind();

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private void LoadAssets()
    {
        if (_uiAsset != null) return;

        _uiAsset = Resources.Load<InputActionAsset>(_playerAssetResourcesPath);

        // Fallback: si no está en Resources, usamos el primer PlayerInput activo.
        if (_uiAsset == null)
        {
            PlayerInput pi = FindObjectOfType<PlayerInput>();
            if (pi != null)
                _uiAsset = pi.actions;
        }
    }

    private void ApplySavedOverridesToAssets()
    {
        if (_uiAsset == null) return;
        if (!PlayerPrefs.HasKey(OverridesKey)) return;

        string json = PlayerPrefs.GetString(OverridesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return;

        _uiAsset.LoadBindingOverridesFromJson(json);
    }

    private void ApplyOverridesToAllPlayerInputs()
    {
        if (!PlayerPrefs.HasKey(OverridesKey)) return;

        string json = PlayerPrefs.GetString(OverridesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return;

        PlayerInput[] inputs = FindObjectsOfType<PlayerInput>(true);
        foreach (PlayerInput pi in inputs)
        {
            if (pi == null || pi.actions == null) continue;
            pi.actions.LoadBindingOverridesFromJson(json);
        }
    }

    private void SaveOverridesFromAsset()
    {
        if (_uiAsset == null) return;

        string json = _uiAsset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(OverridesKey, json);
        PlayerPrefs.Save();

        ApplyOverridesToAllPlayerInputs();
    }

    private void ResetOverrides()
    {
        CancelActiveRebind();

        if (_uiAsset != null)
            _uiAsset.RemoveAllBindingOverrides();

        PlayerPrefs.DeleteKey(OverridesKey);
        PlayerPrefs.Save();

        // Quitar overrides también de PlayerInput activos
        PlayerInput[] inputs = FindObjectsOfType<PlayerInput>(true);
        foreach (PlayerInput pi in inputs)
        {
            if (pi == null || pi.actions == null) continue;
            pi.actions.RemoveAllBindingOverrides();
        }

        RefreshAllRows();
        SetStatus("Overrides reset to default.");
    }

    private void BuildUI()
    {
        LoadAssets();
        ApplySavedOverridesToAssets();

        GameObject canvasGO = new GameObject("RebindCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGO);
        _canvas = canvasGO.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _panelRoot = new GameObject("RebindPanel", typeof(RectTransform), typeof(Image));
        _panelRoot.transform.SetParent(canvasGO.transform, false);

        RectTransform prt = _panelRoot.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        Image bg = _panelRoot.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.78f);

        // Content container
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(_panelRoot.transform, false);

        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(900f, 520f);

        VerticalLayoutGroup v = content.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 24, 24);
        v.spacing = 12f;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        TextMeshProUGUI title = CreateTMP(content.transform, "Controls remapping", 34, TextAlignmentOptions.MidlineLeft);
        title.margin = new Vector4(0, 0, 0, 6);

        // Rows
        _rows.Clear();
        AddRow(content.transform, "Pause", "Gameplay", "Pause", keyboardPathPrefix: "<Keyboard>", gamepadPrefix: "<Gamepad>");
        AddRow(content.transform, "Drag", "Gameplay", "Drag", keyboardPathPrefix: "<Mouse>", gamepadPrefix: "<Gamepad>");

        // Status text
        _statusText = CreateTMP(content.transform, "", 18, TextAlignmentOptions.MidlineLeft);
        _statusText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Buttons row
        GameObject buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttons.transform.SetParent(content.transform, false);

        HorizontalLayoutGroup h = buttons.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        Button resetBtn = CreateButton(buttons.transform, "Reset to default", 220f, 44f);
        resetBtn.onClick.AddListener(ResetOverrides);

        Button closeBtn = CreateButton(buttons.transform, "Close", 140f, 44f);
        closeBtn.onClick.AddListener(Hide);

        _panelRoot.SetActive(false);
    }

    private void AddRow(Transform parent, string displayName, string mapName, string actionName, string keyboardPathPrefix, string gamepadPrefix)
    {
        InputAction action = FindAction(mapName, actionName);
        if (action == null)
            return;

        Row row = new Row();
        row.ActionName = displayName;
        row.Action = action;
        row.KeyboardBindingIndex = FindFirstBindingIndex(action, keyboardPathPrefix);
        row.GamepadBindingIndex = FindFirstBindingIndex(action, gamepadPrefix);

        GameObject rowGO = new GameObject($"Row_{displayName}", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rowGO.transform.SetParent(parent, false);

        VerticalLayoutGroup v = rowGO.GetComponent<VerticalLayoutGroup>();
        v.spacing = 6f;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;

        CreateTMP(rowGO.transform, displayName, 24, TextAlignmentOptions.MidlineLeft);

        // Keyboard line
        GameObject kb = CreateBindingLine(rowGO.transform, "Keyboard/Mouse", out row.KeyboardValue, out row.KeyboardButton);
        row.KeyboardButton.onClick.AddListener(() => StartRebind(row, isGamepad: false));

        // Gamepad line
        GameObject gp = CreateBindingLine(rowGO.transform, "Gamepad", out row.GamepadValue, out row.GamepadButton);
        row.GamepadButton.onClick.AddListener(() => StartRebind(row, isGamepad: true));

        _rows.Add(row);
    }

    private InputAction FindAction(string mapName, string actionName)
    {
        if (_uiAsset == null) return null;

        InputActionMap map = _uiAsset.FindActionMap(mapName, throwIfNotFound: false);
        if (map != null)
        {
            InputAction a = map.FindAction(actionName, throwIfNotFound: false);
            if (a != null) return a;
        }

        return _uiAsset.FindAction(actionName, throwIfNotFound: false);
    }

    private int FindFirstBindingIndex(InputAction action, string pathPrefix)
    {
        if (action == null) return -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;

            string p = b.effectivePath;
            if (string.IsNullOrWhiteSpace(p)) p = b.path;

            if (!string.IsNullOrWhiteSpace(p) && p.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private GameObject CreateBindingLine(Transform parent, string label, out TextMeshProUGUI valueText, out Button rebindButton)
    {
        GameObject line = new GameObject($"Line_{label}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        line.transform.SetParent(parent, false);

        HorizontalLayoutGroup h = line.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        TextMeshProUGUI lab = CreateTMP(line.transform, label + ":", 18, TextAlignmentOptions.MidlineLeft);
        lab.rectTransform.sizeDelta = new Vector2(170f, 32f);

        rebindButton = CreateButton(line.transform, "Rebind", 120f, 36f);

        valueText = CreateTMP(line.transform, "—", 18, TextAlignmentOptions.MidlineLeft);
        valueText.rectTransform.sizeDelta = new Vector2(560f, 36f);

        return line;
    }

    private Button CreateButton(Transform parent, string text, float width, float height)
    {
        GameObject go = new GameObject($"Btn_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        Button btn = go.GetComponent<Button>();

        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(go.transform, false);

        RectTransform trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 4f);
        trt.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;

        return tmp;
    }

    private void RefreshAllRows()
    {
        foreach (Row r in _rows)
        {
            RefreshRow(r);
        }
    }

    private void RefreshRow(Row r)
    {
        if (r == null || r.Action == null) return;

        r.KeyboardValue.text = GetBindingDisplayString(r.Action, r.KeyboardBindingIndex);
        r.GamepadValue.text = GetBindingDisplayString(r.Action, r.GamepadBindingIndex);

        r.KeyboardButton.interactable = r.KeyboardBindingIndex >= 0 && _activeRebind == null;
        r.GamepadButton.interactable = r.GamepadBindingIndex >= 0 && _activeRebind == null;
    }

    private string GetBindingDisplayString(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return "Not set";

        string path = action.bindings[bindingIndex].effectivePath;
        if (string.IsNullOrWhiteSpace(path))
            path = action.bindings[bindingIndex].path;

        if (string.IsNullOrWhiteSpace(path))
            return "Not set";

        return InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    private void StartRebind(Row row, bool isGamepad)
    {
        if (row == null || row.Action == null) return;

        CancelActiveRebind();

        int bindingIndex = isGamepad ? row.GamepadBindingIndex : row.KeyboardBindingIndex;
        if (bindingIndex < 0) return;

        // Desactiva los botones mientras rebind.
        foreach (Row r in _rows)
        {
            if (r.KeyboardButton != null) r.KeyboardButton.interactable = false;
            if (r.GamepadButton != null) r.GamepadButton.interactable = false;
        }

        string target = isGamepad ? "Gamepad" : "Keyboard/Mouse";
        SetStatus($"Rebinding {row.ActionName} ({target}). Press a control… (ESC cancels)");

        row.Action.Disable();

        _activeRebind = row.Action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f);

        if (isGamepad)
        {
            _activeRebind.WithControlsHavingToMatchPath("<Gamepad>");
        }
        else
        {
            // Para Pause queremos teclado. Para Drag dejamos mouse.
            if (row.ActionName.Equals("Pause", StringComparison.OrdinalIgnoreCase))
                _activeRebind.WithControlsHavingToMatchPath("<Keyboard>");
            else
                _activeRebind.WithControlsHavingToMatchPath("<Mouse>");
        }

        _activeRebind.OnCancel(op =>
        {
            row.Action.Enable();
            CancelActiveRebind();
            SetStatus("Canceled.");
            RefreshAllRows();
        });

        _activeRebind.OnComplete(op =>
        {
            row.Action.Enable();
            CancelActiveRebind();
            SaveOverridesFromAsset();
            RefreshAllRows();
            SetStatus("Saved.");
        });

        _activeRebind.Start();
    }

    private void CancelActiveRebind()
    {
        if (_activeRebind != null)
        {
            try { _activeRebind.Cancel(); } catch { }
            try { _activeRebind.Dispose(); } catch { }
            _activeRebind = null;
        }
    }

    private void SetStatus(string msg)
    {
        if (_statusText != null)
            _statusText.text = msg;
    }
}
