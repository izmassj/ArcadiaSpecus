using System.Collections.Generic;
using UnityEngine;

public class BunkerNpcStatusListUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private BunkerNpcStatusListEntryUI _entryPrefab;

    [Header("Refresh")]
    [SerializeField] private bool _refreshOnEnable = true;
    [SerializeField] private float _scanInterval = 0.5f;
    [SerializeField] private bool _sortAlphabetically = true;

    [Header("Debug")]
    [SerializeField] private List<NPCBunkerWorker> _currentWorkers = new List<NPCBunkerWorker>();

    private readonly Dictionary<NPCBunkerWorker, BunkerNpcStatusListEntryUI> _entriesByWorker = new Dictionary<NPCBunkerWorker, BunkerNpcStatusListEntryUI>();
    private readonly List<NPCBunkerWorker> _scanBuffer = new List<NPCBunkerWorker>();
    private float _scanTimer;

    private void OnEnable()
    {
        if (_refreshOnEnable)
            FullRefresh();
    }

    private void Update()
    {
        _scanTimer += Time.unscaledDeltaTime;

        if (_scanTimer < _scanInterval)
            return;

        _scanTimer = 0f;
        FullRefresh();
    }

    [ContextMenu("Full Refresh")]
    public void FullRefresh()
    {
        if (_contentRoot == null || _entryPrefab == null)
            return;

        ScanWorkers();
        RemoveDeadEntries();
        CreateMissingEntries();
        RefreshAndSortEntries();
    }

    private void ScanWorkers()
    {
        _scanBuffer.Clear();
        _currentWorkers.Clear();

        NPCBunkerWorker[] workers = FindObjectsByType<NPCBunkerWorker>(FindObjectsSortMode.None);
        for (int i = 0; i < workers.Length; i++)
        {
            if (workers[i] == null)
                continue;

            _scanBuffer.Add(workers[i]);
        }

        if (_sortAlphabetically)
        {
            _scanBuffer.Sort(CompareWorkersByName);
        }

        for (int i = 0; i < _scanBuffer.Count; i++)
            _currentWorkers.Add(_scanBuffer[i]);
    }

    private void RemoveDeadEntries()
    {
        List<NPCBunkerWorker> toRemove = null;

        foreach (KeyValuePair<NPCBunkerWorker, BunkerNpcStatusListEntryUI> pair in _entriesByWorker)
        {
            NPCBunkerWorker worker = pair.Key;
            BunkerNpcStatusListEntryUI entry = pair.Value;

            bool workerMissing = worker == null || !_currentWorkers.Contains(worker);
            if (!workerMissing)
                continue;

            if (toRemove == null)
                toRemove = new List<NPCBunkerWorker>();

            toRemove.Add(worker);

            if (entry != null)
                Destroy(entry.gameObject);
        }

        if (toRemove == null)
            return;

        for (int i = 0; i < toRemove.Count; i++)
            _entriesByWorker.Remove(toRemove[i]);
    }

    private void CreateMissingEntries()
    {
        for (int i = 0; i < _currentWorkers.Count; i++)
        {
            NPCBunkerWorker worker = _currentWorkers[i];
            if (worker == null)
                continue;

            if (_entriesByWorker.ContainsKey(worker))
                continue;

            BunkerNpcStatusListEntryUI entry = Instantiate(_entryPrefab, _contentRoot);
            entry.Bind(worker);
            _entriesByWorker.Add(worker, entry);
        }
    }

    private void RefreshAndSortEntries()
    {
        for (int i = 0; i < _currentWorkers.Count; i++)
        {
            NPCBunkerWorker worker = _currentWorkers[i];
            if (worker == null)
                continue;

            if (!_entriesByWorker.TryGetValue(worker, out BunkerNpcStatusListEntryUI entry))
                continue;

            if (entry == null)
                continue;

            entry.transform.SetSiblingIndex(i);
            entry.Refresh();
        }
    }

    private int CompareWorkersByName(NPCBunkerWorker a, NPCBunkerWorker b)
    {
        string aName = a != null ? a.gameObject.name : string.Empty;
        string bName = b != null ? b.gameObject.name : string.Empty;
        return string.Compare(aName, bName, System.StringComparison.OrdinalIgnoreCase);
    }
}
