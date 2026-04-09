using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BunkerDayCycleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BunkerResourceManager _resourceManager;
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private BunkerDailySummaryPanelUI _summaryPanel;
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _dayText;

    [Header("Day Settings")]
    [SerializeField] private int _startHour = 6;
    [SerializeField] private int _endHour = 24;
    [SerializeField] private float _realSecondsPerDay = 900f;
    [SerializeField] private int _startDay = 1;

    [Header("Debug")]
    [SerializeField] private int _currentDay;
    [SerializeField] private float _currentDayMinutesElapsed;
    [SerializeField] private bool _isPaused;
    [SerializeField] private bool _isWaitingForNextDay;
    [SerializeField] private BunkerDailySummaryData _pendingSummaryData;
    [SerializeField] private int[] _todayCollectedAmounts;

    private bool _advanceRequested;
    private Coroutine _endDayRoutine;

    public int CurrentDay => _currentDay;
    public bool IsPaused => _isPaused;

    private float DayDurationMinutes => Mathf.Max(1f, (_endHour - _startHour) * 60f);
    private float GameMinutesPerSecond => DayDurationMinutes / Mathf.Max(0.01f, _realSecondsPerDay);

    private void Awake()
    {
        _currentDay = Mathf.Max(1, _startDay);
        EnsureArrays();
        RefreshClockUI();

        if (_summaryPanel != null)
            _summaryPanel.SetVisibleImmediate(false);
    }

    private void OnEnable()
    {
        if (_resourceManager != null)
            _resourceManager.ResourcesAdded += HandleResourcesAdded;
    }

    private void OnDisable()
    {
        if (_resourceManager != null)
            _resourceManager.ResourcesAdded -= HandleResourcesAdded;
    }

    private void Update()
    {
        if (_isPaused || _isWaitingForNextDay)
            return;

        _currentDayMinutesElapsed += GameMinutesPerSecond * Time.deltaTime;

        if (_currentDayMinutesElapsed >= DayDurationMinutes)
        {
            _currentDayMinutesElapsed = DayDurationMinutes;
            RefreshClockUI();

            if (_endDayRoutine == null)
                _endDayRoutine = StartCoroutine(EndDayRoutine());

            return;
        }

        RefreshClockUI();
    }

    [ContextMenu("Force End Day")]
    public void ForceEndDay()
    {
        if (_isWaitingForNextDay)
            return;

        _currentDayMinutesElapsed = DayDurationMinutes;
        RefreshClockUI();

        if (_endDayRoutine == null)
            _endDayRoutine = StartCoroutine(EndDayRoutine());
    }

    public BunkerDayCycleSaveData GetSaveData()
    {
        EnsureArrays();

        BunkerDayCycleSaveData data = new BunkerDayCycleSaveData();
        data.currentDay = _currentDay;
        data.currentDayMinutesElapsed = _currentDayMinutesElapsed;
        data.isPaused = _isPaused;
        data.isWaitingForNextDay = _isWaitingForNextDay;
        data.todayCollectedAmounts = (int[])_todayCollectedAmounts.Clone();
        data.pendingSummaryData = _pendingSummaryData != null
            ? new BunkerDailySummaryData(_pendingSummaryData.dayNumber, _pendingSummaryData.collectedAmounts)
            : null;
        return data;
    }

    public void LoadFromData(BunkerDayCycleSaveData data)
    {
        if (data == null)
            return;

        _currentDay = Mathf.Max(1, data.currentDay);
        _currentDayMinutesElapsed = Mathf.Clamp(data.currentDayMinutesElapsed, 0f, DayDurationMinutes);
        _isPaused = data.isPaused;
        _isWaitingForNextDay = data.isWaitingForNextDay;
        _todayCollectedAmounts = data.todayCollectedAmounts != null ? (int[])data.todayCollectedAmounts.Clone() : CreateCollectedArray();
        _pendingSummaryData = data.pendingSummaryData != null
            ? new BunkerDailySummaryData(data.pendingSummaryData.dayNumber, data.pendingSummaryData.collectedAmounts)
            : null;

        RefreshClockUI();

        if (_summaryPanel != null)
        {
            if (_isWaitingForNextDay && _pendingSummaryData != null)
            {
                _summaryPanel.gameObject.SetActive(true);
                StartCoroutine(_summaryPanel.ShowRoutine(_pendingSummaryData, RequestAdvanceToNextDay));
            }
            else
            {
                _summaryPanel.SetVisibleImmediate(false);
            }
        }
    }

    private IEnumerator EndDayRoutine()
    {
        _isPaused = true;
        _isWaitingForNextDay = true;
        _pendingSummaryData = new BunkerDailySummaryData(_currentDay, _todayCollectedAmounts);

        if (_transition != null)
            yield return _transition.PlayCoverRoutine();

        Time.timeScale = 0f;

        if (_summaryPanel != null)
            yield return _summaryPanel.ShowRoutine(_pendingSummaryData, RequestAdvanceToNextDay);

        yield return new WaitUntil(() => _advanceRequested);
        _advanceRequested = false;

        if (_summaryPanel != null)
            yield return _summaryPanel.HideRoutine();

        StartNextDayInternal();

        Time.timeScale = 1f;

        if (_transition != null)
            yield return _transition.PlayRevealRoutine();

        _isPaused = false;
        _isWaitingForNextDay = false;
        _pendingSummaryData = null;
        _endDayRoutine = null;
    }

    private void StartNextDayInternal()
    {
        _currentDay++;
        _currentDayMinutesElapsed = 0f;
        ClearTodayCollectedAmounts();
        RefreshClockUI();
    }

    private void HandleResourcesAdded(BunkerResourceType resourceType, int amount)
    {
        if (amount <= 0)
            return;

        EnsureArrays();
        int index = (int)resourceType;
        if (index < 0 || index >= _todayCollectedAmounts.Length)
            return;

        _todayCollectedAmounts[index] += amount;
    }

    private void RequestAdvanceToNextDay()
    {
        _advanceRequested = true;
    }

    private void RefreshClockUI()
    {
        int totalMinutes = (_startHour * 60) + Mathf.RoundToInt(_currentDayMinutesElapsed);
        int displayedHours = Mathf.Clamp(totalMinutes / 60, _startHour, _endHour);
        int displayedMinutes = displayedHours >= _endHour ? 0 : Mathf.Clamp(totalMinutes % 60, 0, 59);

        if (_timeText != null)
            _timeText.text = $"{displayedHours:00}:{displayedMinutes:00}";

        if (_dayText != null)
            _dayText.text = $"Day {_currentDay:00}";
    }

    private void EnsureArrays()
    {
        int resourceCount = Enum.GetValues(typeof(BunkerResourceType)).Length;

        if (_todayCollectedAmounts == null || _todayCollectedAmounts.Length != resourceCount)
            _todayCollectedAmounts = CreateCollectedArray();
    }

    private int[] CreateCollectedArray()
    {
        return new int[Enum.GetValues(typeof(BunkerResourceType)).Length];
    }

    private void ClearTodayCollectedAmounts()
    {
        EnsureArrays();

        for (int i = 0; i < _todayCollectedAmounts.Length; i++)
            _todayCollectedAmounts[i] = 0;
    }
}

[Serializable]
public class BunkerDayCycleSaveData
{
    public int currentDay;
    public float currentDayMinutesElapsed;
    public bool isPaused;
    public bool isWaitingForNextDay;
    public int[] todayCollectedAmounts;
    public BunkerDailySummaryData pendingSummaryData;
}
