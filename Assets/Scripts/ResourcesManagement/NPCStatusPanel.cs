using TMPro;
using UnityEngine;

/// <summary>
/// Panel resumen de NPCs (nombre + hambre/sed/fatiga por fila).
/// Refactorizado para ser estable con NPCs que aparecen/desaparecen o mueren.
/// </summary>
public class NPCStatusPanel : MonoBehaviour
{
    [System.Serializable]
    public class NPCRow
    {
        public TextMeshProUGUI nameText;
        public NPCStatIndicator hungerIndicator;
        public NPCStatIndicator thirstIndicator;
        public NPCStatIndicator fatigueIndicator;
    }

    [Header("UI General")]
    public TextMeshProUGUI totalDwellersText;
    [SerializeField] private TextMeshProUGUI _deadDwellersText;

    [Header("Filas de NPC")]
    public NPCRow[] npcRows;

    [Header("Actualización")]
    [SerializeField] private float _refreshInterval = 0.5f;
    [SerializeField] private bool _sortByName = true;

    private float _nextRefreshTime;
    private readonly System.Collections.Generic.List<DwellerNPC> _cache = new System.Collections.Generic.List<DwellerNPC>(32);

    private void OnEnable()
    {
        UpdateUI();
        _nextRefreshTime = Time.unscaledTime + _refreshInterval;
    }

    private void Update()
    {
        if (_refreshInterval <= 0f)
        {
            return;
        }

        if (Time.unscaledTime >= _nextRefreshTime)
        {
            UpdateUI();
            _nextRefreshTime = Time.unscaledTime + _refreshInterval;
        }
    }

    /// <summary>
    /// Actualiza toda la interfaz del panel.
    /// </summary>
    public void UpdateUI()
    {
        RebuildNpcCache();

        int _aliveCount = 0;
        int _deadCount = 0;

        for (int i = 0; i < _cache.Count; i++)
        {
            if (_cache[i] == null)
            {
                continue;
            }

            if (_cache[i].IsDead) _deadCount++;
            else _aliveCount++;
        }

        if (totalDwellersText != null)
        {
            totalDwellersText.text = $"HABITANTES: {_cache.Count}";
        }

        if (_deadDwellersText != null)
        {
            _deadDwellersText.text = $"MUERTOS: {_deadCount}";
        }

        if (npcRows == null || npcRows.Length == 0)
        {
            return;
        }

        for (int i = 0; i < npcRows.Length; i++)
        {
            NPCRow _row = npcRows[i];
            if (_row == null)
            {
                continue;
            }

            if (i >= _cache.Count || _cache[i] == null)
            {
                ClearRow(_row);
                continue;
            }

            FillRow(_row, _cache[i]);
        }
    }

    private void RebuildNpcCache()
    {
        _cache.Clear();

        DwellerNPC[] _npcs = FindObjectsOfType<DwellerNPC>(true);
        for (int i = 0; i < _npcs.Length; i++)
        {
            if (_npcs[i] != null)
            {
                _cache.Add(_npcs[i]);
            }
        }

        if (_sortByName)
        {
            _cache.Sort(CompareNpcForPanel);
        }
    }

    private int CompareNpcForPanel(DwellerNPC _a, DwellerNPC _b)
    {
        if (_a == null && _b == null) return 0;
        if (_a == null) return 1;
        if (_b == null) return -1;

        // Vivos primero, muertos al final.
        if (_a.IsDead != _b.IsDead)
        {
            return _a.IsDead ? 1 : -1;
        }

        return string.Compare(_a.dwellerName, _b.dwellerName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void FillRow(NPCRow _row, DwellerNPC _npc)
    {
        if (_row.nameText != null)
        {
            string _suffix = _npc.IsDead ? " (MUERTO)" : string.Empty;
            _row.nameText.text = (_npc.dwellerName ?? "NPC") + _suffix;
        }

        float _hunger = 0f;
        float _thirst = 0f;
        float _fatigue = 0f;

        if (_npc.needs != null)
        {
            _hunger = _npc.needs.hunger;
            _thirst = _npc.needs.thirst;
            _fatigue = _npc.needs.fatigue;
        }

        if (_row.hungerIndicator != null) _row.hungerIndicator.SetValue(_hunger);
        if (_row.thirstIndicator != null) _row.thirstIndicator.SetValue(_thirst);
        if (_row.fatigueIndicator != null) _row.fatigueIndicator.SetValue(_fatigue);
    }

    private void ClearRow(NPCRow _row)
    {
        if (_row.nameText != null) _row.nameText.text = "-";
        if (_row.hungerIndicator != null) _row.hungerIndicator.SetValue(0f);
        if (_row.thirstIndicator != null) _row.thirstIndicator.SetValue(0f);
        if (_row.fatigueIndicator != null) _row.fatigueIndicator.SetValue(0f);
    }
}
