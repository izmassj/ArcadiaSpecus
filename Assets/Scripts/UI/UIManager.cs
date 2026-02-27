using UnityEngine;

/// <summary>
/// Manager ligero para refrescar widgets/paneles UI de recursos y NPCs.
/// No fuerza estética; solo sincroniza funcionalidad.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles opcionales")]
    [SerializeField] private NPCStatusPanel _npcStatusPanel;
    [SerializeField] private UIVisibilityManager _uiVisibilityManager;

    [Header("Comportamiento")]
    [SerializeField] private bool _autoRefreshOnEnable = true;
    [SerializeField] private bool _listenResourceEvents = true;
    [SerializeField] private bool _verboseLogs = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheOptionalReferences();
    }

    private void OnEnable()
    {
        CacheOptionalReferences();

        if (_listenResourceEvents)
        {
            ResourceManager.OnResourceChanged += HandleResourceChanged;
            ResourceManager.OnNPCDied += HandleNpcDied;
            ResourceManager.OnGameOver += HandleGameOver;
        }

        if (_autoRefreshOnEnable)
        {
            RefreshAllDisplays();
        }
    }

    private void OnDisable()
    {
        if (_listenResourceEvents)
        {
            ResourceManager.OnResourceChanged -= HandleResourceChanged;
            ResourceManager.OnNPCDied -= HandleNpcDied;
            ResourceManager.OnGameOver -= HandleGameOver;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CacheOptionalReferences()
    {
        if (_npcStatusPanel == null)
        {
            _npcStatusPanel = FindObjectOfType<NPCStatusPanel>(true);
        }

        if (_uiVisibilityManager == null)
        {
            _uiVisibilityManager = FindObjectOfType<UIVisibilityManager>(true);
        }
    }

    private void HandleResourceChanged(ResourceType _type, int _amount)
    {
        // Los ResourceIndicator ya se actualizan solos por evento.
        // Aquí solo mantenemos paneles de resumen sincronizados.
        if (_npcStatusPanel != null)
        {
            _npcStatusPanel.UpdateUI();
        }
    }

    private void HandleNpcDied(DwellerNPC _npc)
    {
        if (_verboseLogs && _npc != null)
        {
            Debug.Log($"[UIManager] NPC muerto, refrescando panel: {_npc.dwellerName}");
        }

        if (_npcStatusPanel != null)
        {
            _npcStatusPanel.UpdateUI();
        }
    }

    private void HandleGameOver()
    {
        RefreshAllDisplays();
    }

    /// <summary>
    /// Refresco centralizado para llamar desde botones, guardado/carga o cambios de escena.
    /// </summary>
    public void RefreshAllDisplays()
    {
        CacheOptionalReferences();

        if (_uiVisibilityManager != null)
        {
            _uiVisibilityManager.RefreshFromCurrentState();
        }

        if (_npcStatusPanel != null)
        {
            _npcStatusPanel.UpdateUI();
        }

        ResourceIndicator[] _indicators = FindObjectsOfType<ResourceIndicator>(true);
        for (int i = 0; i < _indicators.Length; i++)
        {
            if (_indicators[i] != null)
            {
                _indicators[i].EmergencyUpdate();
            }
        }
    }
}
