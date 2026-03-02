using TMPro;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TestingManager : MonoBehaviour
{
    public static TestingManager Instance;

    [Header("UI Testing")]
    public GameObject testingPanel;
    public Button killAllNPCsButton;
    public Button accelerateNeedsButton;
    public Button forceGameOverButton;
    public Button reviveAllNPCsButton;
    public TextMeshProUGUI debugText;

    [Header("Teclas (New Input System)")]
    [SerializeField] private Key _togglePanelKey = Key.F3;
    [SerializeField] private Key _killAllKey = Key.F4;
    [SerializeField] private Key _forceGameOverKey = Key.F5;
    [SerializeField] private Key _reviveAllKey = Key.F6;

    [Header("Exterior debug (opcionales)")]
    [SerializeField] private Key _advanceExterior6hKey = Key.F7;
    [SerializeField] private Key _resetExteriorKey = Key.F8;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeTestingUI();
    }

    private void Update()
    {
        if (WasKeyPressedThisFrame(_togglePanelKey))
        {
            ToggleTestingPanel();
        }

        UpdateDebugInfo();

        if (WasKeyPressedThisFrame(_killAllKey))
        {
            KillAllNPCs();
        }

        if (WasKeyPressedThisFrame(_forceGameOverKey))
        {
            ForceGameOverWithDiagnostic();
        }

        if (WasKeyPressedThisFrame(_reviveAllKey))
        {
            ReviveAllNPCs();
        }

        // Estas teclas no rompen nada si no existe el manager de exterior.
        if (WasKeyPressedThisFrame(_advanceExterior6hKey))
        {
            TryAdvanceExteriorHours(6f);
        }

        if (WasKeyPressedThisFrame(_resetExteriorKey))
        {
            TryResetExteriorState();
        }
    }

    private bool WasKeyPressedThisFrame(Key _key)
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[_key] != null && Keyboard.current[_key].wasPressedThisFrame;
#else
        return false;
#endif
    }

    private void InitializeTestingUI()
    {
        if (testingPanel != null)
        {
            testingPanel.SetActive(false);
        }

        BindButton(killAllNPCsButton, KillAllNPCs, "KILL ALL NPCS");
        BindButton(accelerateNeedsButton, AccelerateAllNPCsNeeds, "ACCELERATE NEEDS");
        BindButton(forceGameOverButton, ForceGameOverWithDiagnostic, "FORCE GAME OVER");
        BindButton(reviveAllNPCsButton, ReviveAllNPCs, "REVIVE ALL NPCS");
    }

    private void BindButton(Button _button, UnityEngine.Events.UnityAction _action, string _label)
    {
        if (_button == null)
        {
            return;
        }

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(_action);

        TextMeshProUGUI _buttonText = _button.GetComponentInChildren<TextMeshProUGUI>();
        if (_buttonText != null)
        {
            _buttonText.text = _label;
        }
    }

    private void ToggleTestingPanel()
    {
        if (testingPanel != null)
        {
            testingPanel.SetActive(!testingPanel.activeSelf);
        }
    }

    public void KillAllNPCs()
    {
        DwellerNPC[] _allNPCs = FindObjectsOfType<DwellerNPC>(true);
        for (int i = 0; i < _allNPCs.Length; i++)
        {
            if (_allNPCs[i] != null && !_allNPCs[i].IsDead)
            {
                _allNPCs[i].KillInstantly();
            }
        }

        CheckForGameOver();
    }

    public void ReviveAllNPCs()
    {
        DwellerNPC[] _allNPCs = FindObjectsOfType<DwellerNPC>(true);
        for (int i = 0; i < _allNPCs.Length; i++)
        {
            if (_allNPCs[i] != null && _allNPCs[i].IsDead)
            {
                _allNPCs[i].Revive();
            }
        }

        if (AssignmentManager.Instance != null)
        {
            AssignmentManager.Instance.ResetAllAssignments();
            AssignmentManager.Instance.AutoAssignAll();
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetGameOver();
        }
    }

    public void AccelerateAllNPCsNeeds()
    {
        DwellerNPC[] _allNPCs = FindObjectsOfType<DwellerNPC>(true);
        for (int i = 0; i < _allNPCs.Length; i++)
        {
            if (_allNPCs[i] != null && !_allNPCs[i].IsDead && _allNPCs[i].needs != null)
            {
                _allNPCs[i].needs.AccelerateNeedsWithoutKilling();
            }
        }

        Invoke(nameof(CheckForGameOver), 0.25f);
    }

    private void CheckForGameOver()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ForceCheckGameOver();
        }
    }

    public void ForceGameOverWithDiagnostic()
    {
        Debug.Log("=== TESTING: FORCE GAME OVER ===");
        Debug.Log($"GameOverManager: {GameOverManager.Instance != null}");
        Debug.Log($"ResourceManager: {ResourceManager.Instance != null}");

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ForceGameOver();
        }
        else if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.TriggerGameOver();
        }
    }

    public void ForceGameOver()
    {
        ForceGameOverWithDiagnostic();
    }

    private void UpdateDebugInfo()
    {
        if (debugText == null || testingPanel == null || !testingPanel.activeInHierarchy)
        {
            return;
        }

        DwellerNPC[] _allNPCs = FindObjectsOfType<DwellerNPC>(true);
        int _alive = 0;
        int _dead = 0;
        int _critical = 0;

        for (int i = 0; i < _allNPCs.Length; i++)
        {
            DwellerNPC _npc = _allNPCs[i];
            if (_npc == null)
            {
                continue;
            }

            if (_npc.IsDead)
            {
                _dead++;
            }
            else
            {
                _alive++;
                if (_npc.needs != null && _npc.needs.IsCritical())
                {
                    _critical++;
                }
            }
        }

        StringBuilder _sb = new StringBuilder(768);
        _sb.AppendLine("DEBUG INFO");

        // Contexto rápido (ayuda mucho a entender por qué algo "no sale")
        try
        {
            _sb.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
        }
        catch { /* ignore */ }
        _sb.AppendLine($"TimeScale: {Time.timeScale:0.00}");
        _sb.AppendLine();

        _sb.AppendLine($"Alive NPCs: {_alive}");
        _sb.AppendLine($"Dead NPCs: {_dead}");
        _sb.AppendLine($"Critical NPCs: {_critical}");

        if (ResourceManager.Instance != null)
        {
            _sb.AppendLine($"Total Deaths: {ResourceManager.Instance.GetDeadNPCCount()}");
            _sb.AppendLine($"Game Over Limit: {ResourceManager.Instance.GetMaxAllowedDeaths()}");
            _sb.AppendLine($"Game Over: {(ResourceManager.Instance.IsGameOverTriggered() ? "ACTIVE" : "INACTIVE")}");


            _sb.AppendLine();
            _sb.AppendLine("RESOURCES");

            try
            {
                Array _values = Enum.GetValues(typeof(ResourceType));
                for (int i = 0; i < _values.Length; i++)
                {
                    ResourceType _rt = (ResourceType)_values.GetValue(i);
                    int _amount = ResourceManager.Instance.GetResourceAmount(_rt);
                    int _warn = ResourceManager.Instance.GetWarningLevel(_rt);
                    int _min = ResourceManager.Instance.GetMinimumLevel(_rt);
                    string _state = _amount <= _min ? "CRIT" : (_amount <= _warn ? "WARN" : "OK");
                    _sb.AppendLine($"{_rt}: {_amount}  (warn {_warn}, min {_min})  [{_state}]");
                }

                _sb.AppendLine($"ProdMult: {ResourceManager.Instance.GetGlobalProductionMultiplier():0.00}");
            }
            catch (Exception _ex)
            {
                _sb.AppendLine($"[RESOURCES] Error leyendo recursos: {_ex.GetType().Name}");
            }
        }

        // Exterior (solo si existe un manager en tu proyecto; funciona por reflection)
        AppendExteriorDebug(_sb);

        debugText.text = _sb.ToString();
    }

    private void AppendExteriorDebug(StringBuilder _sb)
    {
        Type _t = FindType("ExteriorWorldManager");
        if (_t == null)
        {
            return;
        }

        UnityEngine.Object _mgr = FindFirstObjectOfTypeAll(_t);
        _sb.AppendLine();
        _sb.AppendLine("EXTERIOR");
        _sb.AppendLine(_mgr != null ? "ExteriorWorldManager: FOUND" : "ExteriorWorldManager: NOT FOUND IN SCENE");
        _sb.AppendLine("F7: advance 6h | F8: reset");
    }

    private void TryAdvanceExteriorHours(float _hours)
    {
        Type _t = FindType("ExteriorWorldManager");
        if (_t == null)
        {
            return;
        }

        UnityEngine.Object _mgr = FindFirstObjectOfTypeAll(_t);
        if (_mgr == null)
        {
            return;
        }

        // Intentar varios nombres por si cambia la implementación.
        string[] _names = new string[] { "DebugAdvanceHours", "AdvanceHours", "SimulateHours", "FastForwardHours" };
        for (int i = 0; i < _names.Length; i++)
        {
            MethodInfo _mi = _t.GetMethod(_names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_mi == null)
            {
                continue;
            }

            ParameterInfo[] _p = _mi.GetParameters();
            try
            {
                if (_p.Length == 1 && _p[0].ParameterType == typeof(float))
                {
                    _mi.Invoke(_mgr, new object[] { _hours });
                    Debug.Log($"[Testing] Exterior advanced {_hours}h via {_names[i]}(float)");
                    return;
                }
                if (_p.Length == 1 && _p[0].ParameterType == typeof(int))
                {
                    _mi.Invoke(_mgr, new object[] { Mathf.RoundToInt(_hours) });
                    Debug.Log($"[Testing] Exterior advanced {(int)_hours}h via {_names[i]}(int)");
                    return;
                }
            }
            catch (Exception _ex)
            {
                Debug.LogWarning($"[Testing] Exterior advance failed on {_names[i]}: {_ex.GetType().Name}");
            }
        }
    }

    private void TryResetExteriorState()
    {
        Type _t = FindType("ExteriorWorldManager");
        if (_t == null)
        {
            return;
        }

        UnityEngine.Object _mgr = FindFirstObjectOfTypeAll(_t);
        if (_mgr == null)
        {
            return;
        }

        string[] _names = new string[] { "ResetWorldState", "DebugResetWorldState", "ResetState", "DebugReset" };
        for (int i = 0; i < _names.Length; i++)
        {
            MethodInfo _mi = _t.GetMethod(_names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_mi == null)
            {
                continue;
            }

            try
            {
                if (_mi.GetParameters().Length == 0)
                {
                    _mi.Invoke(_mgr, null);
                    Debug.Log($"[Testing] Exterior reset via {_names[i]}()");
                    return;
                }
            }
            catch (Exception _ex)
            {
                Debug.LogWarning($"[Testing] Exterior reset failed on {_names[i]}: {_ex.GetType().Name}");
            }
        }
    }

    private static Type FindType(string _typeName)
    {
        if (string.IsNullOrWhiteSpace(_typeName))
        {
            return null;
        }

        Assembly[] _assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < _assemblies.Length; i++)
        {
            Type _t = _assemblies[i].GetType(_typeName);
            if (_t != null)
            {
                return _t;
            }

            try
            {
                Type[] _types = _assemblies[i].GetTypes();
                for (int j = 0; j < _types.Length; j++)
                {
                    if (_types[j] != null && _types[j].Name == _typeName)
                    {
                        return _types[j];
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        return null;
    }

    private static UnityEngine.Object FindFirstObjectOfTypeAll(Type _t)
    {
        UnityEngine.Object[] _all = Resources.FindObjectsOfTypeAll(_t);
        if (_all == null || _all.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < _all.Length; i++)
        {
            if (_all[i] is Component _c && _c != null && _c.gameObject != null && _c.gameObject.scene.IsValid())
            {
                return _c;
            }
        }

        return _all[0];
    }

    public void KillNPCByName(string _npcName)
    {
        if (string.IsNullOrWhiteSpace(_npcName))
        {
            return;
        }

        DwellerNPC[] _allNPCs = FindObjectsOfType<DwellerNPC>(true);
        for (int i = 0; i < _allNPCs.Length; i++)
        {
            if (_allNPCs[i] != null && _allNPCs[i].dwellerName == _npcName && !_allNPCs[i].IsDead)
            {
                _allNPCs[i].KillInstantly();
                CheckForGameOver();
                return;
            }
        }
    }

    public void ReviveNPCByName(string _npcName)
    {
        if (string.IsNullOrWhiteSpace(_npcName))
        {
            return;
        }

        DwellerNPC[] _allNPCs = FindObjectsOfType<DwellerNPC>(true);
        for (int i = 0; i < _allNPCs.Length; i++)
        {
            if (_allNPCs[i] != null && _allNPCs[i].dwellerName == _npcName && _allNPCs[i].IsDead)
            {
                _allNPCs[i].Revive();
                return;
            }
        }
    }

    [ContextMenu("Complete System Debug")]
    public void FullSystemDebug()
    {
        Debug.Log("=== COMPLETE SYSTEM DEBUG ===");
        Debug.Log($"GameOverManager: {GameOverManager.Instance != null}");
        Debug.Log($"ResourceManager: {ResourceManager.Instance != null}");
        Debug.Log($"TestingManager: {Instance != null}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");

        DwellerNPC[] _allNPCs = FindObjectsOfType<DwellerNPC>(true);
        Debug.Log($"Total NPCs: {_allNPCs.Length}");
        for (int i = 0; i < _allNPCs.Length; i++)
        {
            DwellerNPC _npc = _allNPCs[i];
            if (_npc == null)
            {
                continue;
            }
            Debug.Log($"- {_npc.dwellerName} | {(_npc.IsDead ? "DEAD" : "ALIVE")} | {(_npc.needs != null ? _npc.needs.GetNeedsStatus() : "NO NEEDS")}");
        }
    }

    [ContextMenu("Show Testing Panel")]
    public void ShowTestingPanel()
    {
        if (testingPanel != null)
        {
            testingPanel.SetActive(true);
        }
    }

    [ContextMenu("Hide Testing Panel")]
    public void HideTestingPanel()
    {
        if (testingPanel != null)
        {
            testingPanel.SetActive(false);
        }
    }

    [ContextMenu("Kill All NPCs")]
    public void ContextKillAll() => KillAllNPCs();

    [ContextMenu("Revive All NPCs")]
    public void ContextReviveAll() => ReviveAllNPCs();

    [ContextMenu("Force Game Over")]
    public void ContextForceGameOver() => ForceGameOverWithDiagnostic();
}
