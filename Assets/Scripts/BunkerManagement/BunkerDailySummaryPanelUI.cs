using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BunkerDailySummaryPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _summaryText;
    [SerializeField] private Button _nextDayButton;

    [Header("Animation")]
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private Ease _fadeEase = Ease.OutQuad;

    private Action _onNextDayPressed;
    private Tween _fadeTween;

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_nextDayButton != null)
            _nextDayButton.onClick.AddListener(HandleNextDayPressed);

        SetVisibleImmediate(false);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();

        if (_nextDayButton != null)
            _nextDayButton.onClick.RemoveListener(HandleNextDayPressed);
    }

    public void SetVisibleImmediate(bool visible)
    {
        gameObject.SetActive(true);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        if (_nextDayButton != null)
            _nextDayButton.interactable = visible;

        if (!visible)
            gameObject.SetActive(false);
    }

    public IEnumerator ShowRoutine(BunkerDailySummaryData summaryData, Action onNextDayPressed)
    {
        _onNextDayPressed = onNextDayPressed;
        ApplySummary(summaryData);

        gameObject.SetActive(true);

        if (_canvasGroup == null)
            yield break;

        _fadeTween?.Kill();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        _fadeTween = _canvasGroup
            .DOFade(1f, _fadeDuration)
            .SetEase(_fadeEase)
            .SetUpdate(true);

        yield return _fadeTween.WaitForCompletion();

        _canvasGroup.interactable = true;
        if (_nextDayButton != null)
            _nextDayButton.interactable = true;
    }

    public IEnumerator HideRoutine()
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        _fadeTween?.Kill();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _fadeTween = _canvasGroup
            .DOFade(0f, _fadeDuration)
            .SetEase(_fadeEase)
            .SetUpdate(true);

        yield return _fadeTween.WaitForCompletion();
        gameObject.SetActive(false);
    }

    private void ApplySummary(BunkerDailySummaryData summaryData)
    {
        if (summaryData == null)
            return;

        if (_titleText != null)
            _titleText.text = summaryData.BuildTitle();

        if (_summaryText != null)
            _summaryText.text = summaryData.BuildBody();
    }

    private void HandleNextDayPressed()
    {
        _onNextDayPressed?.Invoke();
    }
}
