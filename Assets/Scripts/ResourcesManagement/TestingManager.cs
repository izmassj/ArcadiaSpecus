using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        string _text = "DEBUG INFO\n";
        _text += $"Alive NPCs: {_alive}\n";
        _text += $"Dead NPCs: {_dead}\n";
        _text += $"Critical NPCs: {_critical}\n";

        if (ResourceManager.Instance != null)
        {
            _text += $"Total Deaths: {ResourceManager.Instance.GetDeadNPCCount()}\n";
            _text += $"Game Over Limit: {ResourceManager.Instance.GetMaxAllowedDeaths()}\n";
            _text += $"Game Over: {(ResourceManager.Instance.IsGameOverTriggered() ? "ACTIVE" : "INACTIVE")}";
        }

        debugText.text = _text;
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
