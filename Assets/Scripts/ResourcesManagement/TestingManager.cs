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
            if (buttonText != null) buttonText.text = "MATAR TODOS LOS NPCS";

            Debug.Log("✅ Botón Matar Todos configurado");
        }

        if (accelerateNeedsButton != null)
        {
            accelerateNeedsButton.onClick.RemoveAllListeners();
            accelerateNeedsButton.onClick.AddListener(AccelerateAllNPCsNeeds);

            TextMeshProUGUI buttonText = accelerateNeedsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "ACELERAR NECESIDADES";

            Debug.Log("✅ Botón Acelerar Necesidades configurado");
        }

        if (forceGameOverButton != null)
        {
            forceGameOverButton.onClick.RemoveAllListeners();
            forceGameOverButton.onClick.AddListener(ForceGameOverWithDiagnostic);

            TextMeshProUGUI buttonText = forceGameOverButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "FORZAR GAME OVER";

            Debug.Log("✅ Botón Forzar Game Over configurado");
        }

        if (reviveAllNPCsButton != null)
        {
            reviveAllNPCsButton.onClick.RemoveAllListeners();
            reviveAllNPCsButton.onClick.AddListener(ReviveAllNPCs);

            TextMeshProUGUI buttonText = reviveAllNPCsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "REVIVIR TODOS LOS NPCS";

            Debug.Log("✅ Botón Revivir Todos configurado");
        }

        Debug.Log("🧪 TestingManager inicializado - Presiona F3 para mostrar panel de testing");
        Debug.Log("🎮 Atajos: F4=Matar Todos, F5=Game Over, F6=Revivir Todos");
    }

    private void ToggleTestingPanel()
    {
        if (testingPanel != null)
        {
            bool newState = !testingPanel.activeInHierarchy;
            testingPanel.SetActive(newState);
            Debug.Log($"🧪 Panel de Testing {(newState ? "ACTIVADO" : "DESACTIVADO")}");
        }
    }

    public void KillAllNPCs()
    {
        Debug.LogWarning("💀 MATANDO A TODOS LOS NPCS...");

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

        Debug.Log($"✅ {killedCount} NPCs eliminados de {allNPCs.Length} totales");

        CheckForGameOver();
    }

    private void CheckForGameOver()
    {
        if (ResourceManager.Instance != null)
        {
            int deadCount = ResourceManager.Instance.GetDeadNPCCount();
            int maxDeaths = ResourceManager.Instance.GetMaxAllowedDeaths();

            Debug.Log($"📊 Muertes actuales: {deadCount}/{maxDeaths}");

            if (deadCount >= maxDeaths)
            {
                Debug.Log("✅ Condición de Game Over alcanzada - Debería activarse automáticamente");
            }
            else
            {
                Debug.Log($"⚠️ Aún no hay suficientes muertes para Game Over ({deadCount}/{maxDeaths})");
            }
        }
    }

    public void ReviveAllNPCs()
    {
        Debug.Log("🔁 REVIVIENDO A TODOS LOS NPCS...");

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

        Debug.Log($"✅ {revivedCount} NPCs revividos de {allNPCs.Length} totales");

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
        Debug.Log("⚡ ACELERANDO NECESIDADES DE TODOS LOS NPCS...");

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

                    Debug.Log($"📊 {npc.dwellerName} - H:{(int)npc.needs.hunger} S:{(int)npc.needs.thirst} F:{(int)npc.needs.fatigue}");
                }
            }
        }

        Debug.Log($"✅ Necesidades aceleradas para {acceleratedCount} NPCs");

        Invoke("CheckForDeathsAfterAcceleration", 1f);
    }

    private void CheckForDeathsAfterAcceleration()
    {
        Debug.Log("🔍 Verificando muertes después de aceleración...");
        CheckForGameOver();
    }

    public void ForceGameOverWithDiagnostic()
    {
        Debug.LogWarning("🎮 === DIAGNÓSTICO FORZADO DE GAME OVER ===");

        // 1. Verificar managers
        Debug.Log($"1. GameOverManager: {GameOverManager.Instance != null}");
        Debug.Log($"2. ResourceManager: {ResourceManager.Instance != null}");

        // 2. Verificar estado actual
        if (GameOverManager.Instance != null)
        {
            Debug.Log($"3. Estado isGameOver: {GameOverManager.Instance.IsGameOver()}");
            GameOverManager.Instance.DebugStatus();
        }

        // 3. Forzar Game Over
        Debug.Log("4. Forzando Game Over...");

        if (GameOverManager.Instance != null)
        {
            Debug.Log("✅ Usando GameOverManager.ForceGameOver()");
            GameOverManager.Instance.ForceGameOver();
        }
        else if (ResourceManager.Instance != null)
        {
            Debug.Log("✅ Usando ResourceManager.TriggerGameOver()");
            ResourceManager.Instance.TriggerGameOver();
        }
        else
        {
            Debug.LogError("❌ No hay managers disponibles para Game Over");
        }

        Debug.Log("🎮 === FIN DIAGNÓSTICO ===");
    }

    public void ForceGameOver()
    {
        ForceGameOverWithDiagnostic();
    }

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
            debugInfo += $"NPCs Vivos: {aliveCount}\n";
            debugInfo += $"NPCs Muertos: {deadCount}\n";
            debugInfo += $"NPCs Críticos: {criticalCount}\n";

            if (ResourceManager.Instance != null)
            {
                debugInfo += $"Muertes Totales: {ResourceManager.Instance.GetDeadNPCCount()}\n";
                debugInfo += $"Límite Game Over: {ResourceManager.Instance.GetMaxAllowedDeaths()}\n";
                debugInfo += $"Game Over: {(ResourceManager.Instance.IsGameOverTriggered() ? "ACTIVO" : "INACTIVO")}";
            }

            debugText.text = debugInfo;
        }
    }

    public void KillNPCByName(string npcName)
    {
        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc != null && npc.dwellerName == npcName && !npc.IsDead)
            {
                npc.KillInstantly();
                Debug.Log($"{npcName} eliminado");
                CheckForGameOver();
                return;
            }
        }

        Debug.LogWarning($"NPC {npcName} no encontrado o ya está muerto");
    }

    public void ReviveNPCByName(string npcName)
    {
        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();

        foreach (DwellerNPC npc in allNPCs)
        {
            if (npc != null && npc.dwellerName == npcName && npc.IsDead)
            {
                npc.Revive();
                Debug.Log($"{npcName} revivido");
                return;
            }
        }

        Debug.LogWarning($"NPC {npcName} no encontrado o ya está vivo");
    }

    [ContextMenu("🔍 DEBUG COMPLETO DEL SISTEMA")]
    public void FullSystemDebug()
    {
        Debug.Log("=== DEBUG COMPLETO DEL SISTEMA ===");

        Debug.Log($"GameOverManager: {GameOverManager.Instance != null}");
        Debug.Log($"ResourceManager: {ResourceManager.Instance != null}");
        Debug.Log($"TestingManager: {Instance != null}");

        if (GameOverManager.Instance != null)
        {
            Debug.Log($"GameOver activo: {GameOverManager.Instance.IsGameOver()}");
            Debug.Log($"GameOverPanel activo: {GameOverManager.Instance.gameOverPanel != null && GameOverManager.Instance.gameOverPanel.activeInHierarchy}");
        }

        if (ResourceManager.Instance != null)
        {
            Debug.Log($"Muertes registradas: {ResourceManager.Instance.GetDeadNPCCount()}");
            Debug.Log($"Límite GameOver: {ResourceManager.Instance.GetMaxAllowedDeaths()}");
        }

        DwellerNPC[] allNPCs = FindObjectsOfType<DwellerNPC>();
        Debug.Log($"Total NPCs en escena: {allNPCs.Length}");

        foreach (DwellerNPC npc in allNPCs)
        {
            string state = npc.IsDead ? "MUERTO" : "VIVO";
            string needs = npc.needs != null ? npc.needs.GetNeedsStatus() : "SIN NECESIDADES";
            Debug.Log($"- {npc.dwellerName}: {state} | {needs}");
        }

        Debug.Log($"Time.timeScale: {Time.timeScale}");

        Debug.Log("=== FIN DEBUG ===");
    }

    [ContextMenu("🧪 Mostrar Panel Testing")]
    public void ShowTestingPanel()
    {
        if (testingPanel != null)
        {
            testingPanel.SetActive(true);
        }
    }

    [ContextMenu("🧪 Ocultar Panel Testing")]
    public void HideTestingPanel()
    {
        if (testingPanel != null)
        {
            testingPanel.SetActive(false);
        }
    }

    [ContextMenu("💀 Matar Todos los NPCs")]
    public void ContextKillAll() => KillAllNPCs();

    [ContextMenu("🔁 Revivir Todos los NPCs")]
    public void ContextReviveAll() => ReviveAllNPCs();

    [ContextMenu("🎮 Forzar Game Over")]
    public void ContextForceGameOver() => ForceGameOverWithDiagnostic();
}