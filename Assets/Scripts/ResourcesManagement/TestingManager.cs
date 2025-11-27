using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeTestingUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ToggleTestingPanel();
        }

        UpdateDebugInfo();

        if (Input.GetKeyDown(KeyCode.F4))
        {
            KillAllNPCs();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            ForceGameOverWithDiagnostic();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            ReviveAllNPCs();
        }
    }

    /// <summary>
    /// Initializes the testing UI components and button listeners
    /// </summary>
    private void InitializeTestingUI()
    {
        if (testingPanel != null)
        {
            testingPanel.SetActive(false);
        }

        if (killAllNPCsButton != null)
        {
            killAllNPCsButton.onClick.RemoveAllListeners();
            killAllNPCsButton.onClick.AddListener(KillAllNPCs);

            TextMeshProUGUI buttonText = killAllNPCsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "KILL ALL NPCS";

            Debug.Log("Kill All button configured");
        }

        if (accelerateNeedsButton != null)
        {
            accelerateNeedsButton.onClick.RemoveAllListeners();
            accelerateNeedsButton.onClick.AddListener(AccelerateAllNPCsNeeds);

            TextMeshProUGUI buttonText = accelerateNeedsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "ACCELERATE NEEDS";

            Debug.Log("Accelerate Needs button configured");
        }

        if (forceGameOverButton != null)
        {
            forceGameOverButton.onClick.RemoveAllListeners();
            forceGameOverButton.onClick.AddListener(ForceGameOverWithDiagnostic);

            TextMeshProUGUI buttonText = forceGameOverButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "FORCE GAME OVER";

            Debug.Log("Force Game Over button configured");
        }

        if (reviveAllNPCsButton != null)
        {
            reviveAllNPCsButton.onClick.RemoveAllListeners();
            reviveAllNPCsButton.onClick.AddListener(ReviveAllNPCs);

            TextMeshProUGUI buttonText = reviveAllNPCsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "REVIVE ALL NPCS";

            Debug.Log("Revive All button configured");
        }

        Debug.Log("TestingManager initialized - Press F3 to show testing panel");
        Debug.Log("Shortcuts: F4=Kill All, F5=Game Over, F6=Revive All");
    }

    /// <summary>
    /// Toggles the testing panel visibility
    /// </summary>
    private void ToggleTestingPanel()
    {
        if (testingPanel != null)
        {
            bool newState = !testingPanel.activeInHierarchy;
            testingPanel.SetActive(newState);
            Debug.Log($"Testing Panel {(newState ? "ACTIVATED" : "DEACTIVATED")}");
        }
    }

    /// <summary>
    /// Kills all NPCs in the scene for testing purposes
    /// </summary>
    public void KillAllNPCs()
    {
        Debug.LogWarning("KILLING ALL NPCS...");

        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();
        int killedCount = 0;

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc != null && !npc.IsDead)
            {
                npc.KillInstantly();
                killedCount++;
            }
        }

        Debug.Log($"{killedCount} NPCs eliminated of {allNPCs.Length} total");

        CheckForGameOver();
    }

    /// <summary>
    /// Checks if game over condition has been met after killing NPCs
    /// </summary>
    private void CheckForGameOver()
    {
        if (ResourceManager.Instance != null)
        {
            int deadCount = ResourceManager.Instance.GetDeadNPCCount();
            int maxDeaths = ResourceManager.Instance.GetMaxAllowedDeaths();

            Debug.Log($"Current deaths: {deadCount}/{maxDeaths}");

            if (deadCount >= maxDeaths)
            {
                Debug.Log("Game Over condition reached - Should activate automatically");
            }
            else
            {
                Debug.Log($"Not enough deaths for Game Over yet ({deadCount}/{maxDeaths})");
            }
        }
    }

    /// <summary>
    /// Revives all dead NPCs in the scene
    /// </summary>
    public void ReviveAllNPCs()
    {
        Debug.Log("REVIVING ALL NPCS...");

        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();
        int revivedCount = 0;

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc != null && npc.IsDead)
            {
                npc.Revive();
                revivedCount++;
            }
        }

        Debug.Log($"{revivedCount} NPCs revived of {allNPCs.Length} total");

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

    /// <summary>
    /// Accelerates the needs of all NPCs for testing
    /// </summary>
    public void AccelerateAllNPCsNeeds()
    {
        Debug.Log("ACCELERATING NEEDS OF ALL NPCS...");

        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();
        int acceleratedCount = 0;

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc != null && !npc.IsDead)
            {
                if (npc.needs != null)
                {
                    npc.needs.AccelerateNeedsForTesting();
                    acceleratedCount++;

                    Debug.Log($"{npc.dwellerName} - H:{(int)npc.needs.hunger} T:{(int)npc.needs.thirst} F:{(int)npc.needs.fatigue}");
                }
            }
        }

        Debug.Log($"Needs accelerated for {acceleratedCount} NPCs");

        Invoke("CheckForDeathsAfterAcceleration", 1f);
    }

    /// <summary>
    /// Checks for deaths after accelerating NPC needs
    /// </summary>
    private void CheckForDeathsAfterAcceleration()
    {
        Debug.Log("Checking deaths after acceleration...");
        CheckForGameOver();
    }

    /// <summary>
    /// Forces game over with detailed diagnostic information
    /// </summary>
    public void ForceGameOverWithDiagnostic()
    {
        Debug.LogWarning("=== FORCED GAME OVER DIAGNOSTIC ===");

        // 1. Check managers
        Debug.Log($"1. GameOverManager: {GameOverManager.Instance != null}");
        Debug.Log($"2. ResourceManager: {ResourceManager.Instance != null}");

        // 2. Check current state
        if (GameOverManager.Instance != null)
        {
            Debug.Log($"3. isGameOver state: {GameOverManager.Instance.IsGameOver()}");
            GameOverManager.Instance.DebugStatus();
        }

        // 3. Force Game Over
        Debug.Log("4. Forcing Game Over...");

        if (GameOverManager.Instance != null)
        {
            Debug.Log("Using GameOverManager.ForceGameOver()");
            GameOverManager.Instance.ForceGameOver();
        }
        else if (ResourceManager.Instance != null)
        {
            Debug.Log("Using ResourceManager.TriggerGameOver()");
            ResourceManager.Instance.TriggerGameOver();
        }
        else
        {
            Debug.LogError("No managers available for Game Over");
        }

        Debug.Log("=== END DIAGNOSTIC ===");
    }

    /// <summary>
    /// Public method to force game over
    /// </summary>
    public void ForceGameOver()
    {
        ForceGameOverWithDiagnostic();
    }

    /// <summary>
    /// Updates the debug information display
    /// </summary>
    private void UpdateDebugInfo()
    {
        if (debugText != null && testingPanel != null && testingPanel.activeInHierarchy)
        {
            DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();
            int aliveCount = 0;
            int deadCount = 0;
            int criticalCount = 0;

            foreach (DwellerNPC npc in allNPCs)
            {
                if (npc.IsDead)
                    deadCount++;
                else
                {
                    aliveCount++;
                    if (npc.needs != null && npc.needs.IsCritical())
                        criticalCount++;
                }
            }

            string debugInfo = "DEBUG INFO:\n";
            debugInfo += $"Alive NPCs: {aliveCount}\n";
            debugInfo += $"Dead NPCs: {deadCount}\n";
            debugInfo += $"Critical NPCs: {criticalCount}\n";

            if (ResourceManager.Instance != null)
            {
                debugInfo += $"Total Deaths: {ResourceManager.Instance.GetDeadNPCCount()}\n";
                debugInfo += $"Game Over Limit: {ResourceManager.Instance.GetMaxAllowedDeaths()}\n";
                debugInfo += $"Game Over: {(ResourceManager.Instance.IsGameOverTriggered() ? "ACTIVE" : "INACTIVE")}";
            }

            debugText.text = debugInfo;
        }
    }

    /// <summary>
    /// Kills a specific NPC by name
    /// </summary>
    /// <param name="npcName">Name of the NPC to kill</param>
    public void KillNPCByName(string npcName)
    {
        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc != null && npc.dwellerName == npcName && !npc.IsDead)
            {
                npc.KillInstantly();
                Debug.Log($"{npcName} eliminated");
                CheckForGameOver();
                return;
            }
        }

        Debug.LogWarning($"NPC {npcName} not found or already dead");
    }

    /// <summary>
    /// Revives a specific NPC by name
    /// </summary>
    /// <param name="npcName">Name of the NPC to revive</param>
    public void ReviveNPCByName(string npcName)
    {
        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc != null && npc.dwellerName == npcName && npc.IsDead)
            {
                npc.Revive();
                Debug.Log($"{npcName} revived");
                return;
            }
        }

        Debug.LogWarning($"NPC {npcName} not found or already alive");
    }

    /// <summary>
    /// Performs a complete system debug
    /// </summary>
    [ContextMenu("Complete System Debug")]
    public void FullSystemDebug()
    {
        Debug.Log("=== COMPLETE SYSTEM DEBUG ===");

        Debug.Log($"GameOverManager: {GameOverManager.Instance != null}");
        Debug.Log($"ResourceManager: {ResourceManager.Instance != null}");
        Debug.Log($"TestingManager: {Instance != null}");

        if (GameOverManager.Instance != null)
        {
            Debug.Log($"GameOver active: {GameOverManager.Instance.IsGameOver()}");
            Debug.Log($"GameOverPanel active: {GameOverManager.Instance.gameOverPanel != null && GameOverManager.Instance.gameOverPanel.activeInHierarchy}");
        }

        if (ResourceManager.Instance != null)
        {
            Debug.Log($"Registered deaths: {ResourceManager.Instance.GetDeadNPCCount()}");
            Debug.Log($"GameOver Limit: {ResourceManager.Instance.GetMaxAllowedDeaths()}");
        }

        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();
        Debug.Log($"Total NPCs in scene: {allNPCs.Length}");

        foreach (DwellerNPC npc in allNPCs)
        {
            string state = npc.IsDead ? "DEAD" : "ALIVE";
            string needs = npc.needs != null ? npc.needs.GetNeedsStatus() : "NO NEEDS";
            Debug.Log($"- {npc.dwellerName}: {state} | {needs}");
        }

        Debug.Log($"Time.timeScale: {Time.timeScale}");

        Debug.Log("=== END DEBUG ===");
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