using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú de remapping dentro del Main Menu, basado en botones.
/// - Añade un botón "Controls" clonando el estilo de botones existentes.
/// - Abre un panel overlay con filas por acción (KB/M y Gamepad) y botones Rebind.
/// - Guarda overrides en PlayerPrefs (InputBindingOverrides) para que PlayerManager/PauseManager los apliquen.
/// </summary>
public class MainMenuRemapMenu : MonoBehaviour
{
    private const string OverridesKey = "InputBindingOverrides";

    // Acciones que vamos a exponer para rúbrica "Bé" (teclado + mando)
    private readonly string _mapName = "Gameplay";
    private readonly string[] _actions = new[] { "Pause", "Drag" };

    private Canvas _canvas;
    private GameObject _overlayRoot;
    private TMP_Text _statusText;

    private InputActionAsset _runtimeAsset;
    private readonly Dictionary<string, InputAction> _actionByName = new Dictionary<string, InputAction>();

    private InputActionRebindingExtensions.RebindingOperation _activeRebind;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CancelActiveRebind();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reintentar en cada load por si el menú se recarga.
        TryInstallButton();
        // Si el usuario deja el menú abierto y cambia escena, cerrar overlay.
        CloseOverlay();
    }

    private void Start()
    {
        TryInstallButton();
    }

    private void TryInstallButton()
    {
        MainMenuManager menu = FindObjectOfType<MainMenuManager>(true);
        if (menu == null) return;

        // Encontrar canvas del menú
        _canvas = FindAnyMenuCanvas();
        if (_canvas == null) return;

        // Si ya existe el botón, no duplicar
        Transform existing = FindInSceneByName("ControlsBtn");
        if (existing != null) return;

        // Buscar botones conocidos y clonar (mismo look)
        GameObject template = GameObject.Find("HelpBtn");
        if (template == null) template = GameObject.Find("SettingsBtn");
        if (template == null) template = GameObject.Find("PlayBtn");
        if (template == null) return;

        Transform parent = template.transform.parent;
        if (parent == null) return;

        GameObject clone = Instantiate(template, parent);
        clone.name = "ControlsBtn";

        // Ponerlo debajo de Settings si existe
        Transform settings = FindInParent(parent, "SettingsBtn");
        if (settings != null)
            clone.transform.SetSiblingIndex(settings.GetSiblingIndex() + 1);

        SetButtonLabel(clone, "Controls");
        WireButtonClick(clone, OpenOverlay);
    }

    private static Transform FindInSceneByName(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }

    private static Transform FindInParent(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c != null && c.name == childName) return c;
        }
        return null;
    }

    private static void WireButtonClick(GameObject buttonGo, Action onClick)
    {
        Button b = buttonGo.GetComponent<Button>();
        if (b == null) return;

        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() => onClick?.Invoke());
    }

    private static void SetButtonLabel(GameObject buttonGo, string text)
    {
        // TMP
        TMP_Text tmp = buttonGo.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = text; return; }

        // Legacy
        Text t = buttonGo.GetComponentInChildren<Text>(true);
        if (t != null) t.text = text;
    }

    private Canvas FindAnyMenuCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c == null) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;
            return c;
        }
        return null;
    }

    private void OpenOverlay()
    {
        if (_overlayRoot != null) return;

        EnsureRuntimeAsset();
        ApplySavedOverridesToRuntimeAsset();

        _canvas = FindAnyMenuCanvas();
        if (_canvas == null) return;

        // Root full screen
        _overlayRoot = new GameObject("RemapOverlay");
        _overlayRoot.transform.SetParent(_canvas.transform, false);

        RectTransform rootRt = _overlayRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image bg = _overlayRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.70f);
        bg.raycastTarget = true;

        // Panel centered
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(_overlayRoot.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(820, 520);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.10f, 0.10f, 0.10f, 0.95f);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 18, 18);
        vlg.spacing = 10;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Title
        CreateTMP(panel.transform, "Controls Remapping", 34, FontStyles.Bold);

        // Help text
        CreateTMP(panel.transform, "Rebind Keyboard/Mouse and Gamepad. Press Esc to cancel a rebind.", 20, FontStyles.Normal);

        // Status text
        _statusText = CreateTMP(panel.transform, "", 20, FontStyles.Italic);
        _statusText.color = new Color(0.7f, 1f, 0.7f, 1f);

        // Scroll
        GameObject scroll = CreateScroll(panel.transform, out RectTransform contentRt);

        // Rows per action
        foreach (string actionName in _actions)
        {
            if (!_actionByName.TryGetValue(actionName, out InputAction action) || action == null)
                continue;

            CreateActionRow(contentRt, actionName, action);
        }

        // Bottom buttons
        GameObject bottom = new GameObject("Bottom");
        bottom.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup hlg = bottom.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        Button resetBtn = CreateButton(bottom.transform, "Reset to default");
        resetBtn.onClick.AddListener(ResetOverrides);

        Button closeBtn = CreateButton(bottom.transform, "Close");
        closeBtn.onClick.AddListener(CloseOverlay);

        RefreshAllRows(contentRt);
    }

    private void CloseOverlay()
    {
        CancelActiveRebind();
        if (_overlayRoot != null)
        {
            Destroy(_overlayRoot);
            _overlayRoot = null;
            _statusText = null;
        }
    }

    private void EnsureRuntimeAsset()
    {
        if (_runtimeAsset != null) return;

        // Opción 1: cargar InputActionAsset desde Resources (recomendado)
        InputActionAsset asset = Resources.Load<InputActionAsset>("Input/IA_PlayerInputs");
        if (asset == null)
        {
            // Fallback: intentar cargar como TextAsset y parsear JSON
            TextAsset txt = Resources.Load<TextAsset>("Input/IA_PlayerInputs");
            if (txt != null)
                asset = InputActionAsset.FromJson(txt.text);
        }

        if (asset == null)
        {
            Debug.LogWarning("[Remap] No se pudo cargar IA_PlayerInputs desde Resources/Input/IA_PlayerInputs");
            return;
        }

        // Instancia propia para UI (no mutar asset importado directamente)
        _runtimeAsset = Instantiate(asset);

        _actionByName.Clear();
        InputActionMap map = _runtimeAsset.FindActionMap(_mapName, throwIfNotFound: false);
        if (map == null)
        {
            // fallback: buscar sin mapa
            foreach (var a in _runtimeAsset) _actionByName[a.name] = a;
            return;
        }

        foreach (var a in map.actions)
            _actionByName[a.name] = a;
    }

    private void ApplySavedOverridesToRuntimeAsset()
    {
        if (_runtimeAsset == null) return;
        if (!PlayerPrefs.HasKey(OverridesKey)) return;

        string json = PlayerPrefs.GetString(OverridesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            _runtimeAsset.LoadBindingOverridesFromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Remap] Error aplicando overrides guardados: {e.Message}");
        }
    }

    private void SaveOverridesFromRuntimeAsset()
    {
        if (_runtimeAsset == null) return;

        try
        {
            string json = _runtimeAsset.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(OverridesKey, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Remap] Error guardando overrides: {e.Message}");
        }
    }

    private void ResetOverrides()
    {
        CancelActiveRebind();

        if (_runtimeAsset != null)
            _runtimeAsset.RemoveAllBindingOverrides();

        PlayerPrefs.DeleteKey(OverridesKey);
        PlayerPrefs.Save();

        if (_overlayRoot != null)
        {
            RectTransform content = _overlayRoot.GetComponentInChildren<ScrollRect>(true)?.content;
            if (content != null)
                RefreshAllRows(content);
        }

        if (_statusText != null)
            _statusText.text = "Overrides reset.";
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

    private void CreateActionRow(RectTransform parent, string actionName, InputAction action)
    {
        GameObject row = new GameObject($"Row_{actionName}");
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;

        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 46;

        // Action label
        TMP_Text actionLabel = CreateTMP(row.transform, actionName, 22, FontStyles.Bold);
        actionLabel.rectTransform.sizeDelta = new Vector2(150, 0);

        // KBM display + button
        TMP_Text kbmLabel = CreateTMP(row.transform, "", 18, FontStyles.Normal);
        kbmLabel.name = "KBM_Label";
        kbmLabel.rectTransform.sizeDelta = new Vector2(220, 0);

        Button kbmBtn = CreateButton(row.transform, "Rebind KB/M");
        kbmBtn.onClick.AddListener(() => StartRebind(action, FindBindingIndexKBM(action), parent));

        // Gamepad display + button
        TMP_Text padLabel = CreateTMP(row.transform, "", 18, FontStyles.Normal);
        padLabel.name = "PAD_Label";
        padLabel.rectTransform.sizeDelta = new Vector2(220, 0);

        Button padBtn = CreateButton(row.transform, "Rebind Pad");
        padBtn.onClick.AddListener(() => StartRebind(action, FindBindingIndexGamepad(action), parent, gamepadOnly: true));

        // Store mapping via components on row (simple)
        row.AddComponent<RemapRowTag>().Init(actionName, kbmLabel, padLabel, action);
    }

    private void RefreshAllRows(RectTransform contentRoot)
    {
        RemapRowTag[] rows = contentRoot.GetComponentsInChildren<RemapRowTag>(true);
        foreach (var r in rows)
            RefreshRow(r);
    }

    private void RefreshRow(RemapRowTag row)
    {
        if (row == null || row.Action == null) return;

        int kbmIndex = FindBindingIndexKBM(row.Action);
        int padIndex = FindBindingIndexGamepad(row.Action);

        row.KbmLabel.text = kbmIndex >= 0 ? GetBindingString(row.Action, kbmIndex) : "(no KB/M binding)";
        row.PadLabel.text = padIndex >= 0 ? GetBindingString(row.Action, padIndex) : "(no Gamepad binding)";
    }

    private static string GetBindingString(InputAction action, int bindingIndex)
    {
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) return "(none)";
        return action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontOmitDevice);
    }

    private static int FindBindingIndexKBM(InputAction action)
    {
        if (action == null) return -1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (string.IsNullOrEmpty(b.path)) continue;
            if (b.path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase) ||
                b.path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static int FindBindingIndexGamepad(InputAction action)
    {
        if (action == null) return -1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (string.IsNullOrEmpty(b.path)) continue;
            if (b.path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private void StartRebind(InputAction action, int bindingIndex, RectTransform rowsRoot, bool gamepadOnly = false)
    {
        if (action == null) return;
        if (bindingIndex < 0)
        {
            if (_statusText != null) _statusText.text = "No binding slot found for that device.";
            return;
        }

        CancelActiveRebind();

        if (_statusText != null)
            _statusText.text = gamepadOnly ? "Waiting for GAMEPAD input... (Esc to cancel)" : "Waiting for KEY/MOUSE input... (Esc to cancel)";

        action.Disable();

        _activeRebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.08f);

        if (gamepadOnly)
        {
            _activeRebind.WithControlsHavingToMatchPath("<Gamepad>");
        }
        else
        {
            // KB/M: excluimos gamepad para evitar capturar sticks por accidente
            _activeRebind.WithControlsExcluding("<Gamepad>");
        }

        _activeRebind.OnCancel(op =>
        {
            action.Enable();
            if (_statusText != null) _statusText.text = "Rebind cancelled.";
            CleanupRebind();
            RefreshAllRows(rowsRoot);
        });

        _activeRebind.OnComplete(op =>
        {
            action.Enable();
            SaveOverridesFromRuntimeAsset();

            if (_statusText != null) _statusText.text = "Rebind saved.";
            CleanupRebind();
            RefreshAllRows(rowsRoot);
        });

        _activeRebind.Start();
    }

    private void CleanupRebind()
    {
        if (_activeRebind != null)
        {
            try { _activeRebind.Dispose(); } catch { }
            _activeRebind = null;
        }
    }

    // ---------------- UI helpers ----------------

    private TMP_Text CreateTMP(Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject("TMP");
        go.transform.SetParent(parent, false);

        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = Color.white;

        RectTransform rt = t.rectTransform;
        rt.sizeDelta = new Vector2(0, 0);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 10;

        return t;
    }

    private Button CreateButton(Transform parent, string label)
    {
        // Crear botón con look básico (no depende del template)
        GameObject go = new GameObject("Button_" + label);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        Button btn = go.AddComponent<Button>();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 40);

        GameObject txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        TMP_Text txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 18;
        txt.color = Color.white;

        RectTransform txtRt = txt.rectTransform;
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 40;
        le.preferredWidth = 180;

        return btn;
    }

    private GameObject CreateScroll(Transform parent, out RectTransform contentRt)
    {
        GameObject scrollRoot = new GameObject("Scroll");
        scrollRoot.transform.SetParent(parent, false);

        RectTransform rt = scrollRoot.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 340);

        Image img = scrollRoot.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        // viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollRoot.transform, false);
        RectTransform vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(10, 10);
        vpRt.offsetMax = new Vector2(-10, -10);

        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRt;
        scroll.content = contentRt;

        return scrollRoot;
    }
}
