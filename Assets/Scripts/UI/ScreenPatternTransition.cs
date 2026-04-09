using System;
using System.Collections;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ScreenPatternTransition : MonoBehaviour
{
    [Header("UIEffect Reference")]
    [SerializeField] private UIEffect _uiEffect;
    [SerializeField] private Graphic _graphic;

    [Header("Transition")]
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private Ease _ease = Ease.InOutQuad;

    [Header("Pattern Offset")]
    [SerializeField] private Vector2 _coverStartOffset;
    [SerializeField] private Vector2 _coverEndOffset;
    [SerializeField] private Vector2 _revealStartOffset;
    [SerializeField] private Vector2 _revealEndOffset;

    [Header("Transition Range")]
    [SerializeField] public Vector2 coverStartRange = new Vector2(0f, 0f);
    [SerializeField] public Vector2 coverEndRange = new Vector2(0f, 1f);
    [SerializeField] public Vector2 revealStartRange = new Vector2(0f, 1f);
    [SerializeField] public Vector2 revealEndRange = new Vector2(1f, 1f);

    private Tween _rangeTween;
    private Tween _offsetTween;

    public bool IsPlaying => (_rangeTween != null && _rangeTween.IsActive()) || (_offsetTween != null && _offsetTween.IsActive());
    public float Duration => _duration;

    private void Reset()
    {
        _graphic = GetComponent<Graphic>();
        _uiEffect = GetComponent<UIEffect>();
    }

    private void Awake()
    {
        if (_graphic == null)
            _graphic = GetComponent<Graphic>();

        if (_uiEffect == null)
            _uiEffect = GetComponent<UIEffect>();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void SetCoveredInstant()
    {
        KillTweens();
        SetTransitionRange(coverEndRange);
        RefreshGraphic();
    }

    public void SetRevealedInstant()
    {
        KillTweens();
        SetTransitionRange(revealEndRange);
        RefreshGraphic();
    }

    public void PlayCover(Action onComplete = null)
    {
        Play(coverStartRange, coverEndRange, _coverStartOffset, _coverEndOffset, onComplete);
    }

    public void PlayReveal(Action onComplete = null)
    {
        Play(revealStartRange, revealEndRange, _revealStartOffset, _revealEndOffset, onComplete);
    }

    public IEnumerator PlayCoverRoutine()
    {
        bool completed = false;
        PlayCover(() => completed = true);
        yield return new WaitUntil(() => completed);
    }

    public IEnumerator PlayRevealRoutine()
    {
        bool completed = false;
        PlayReveal(() => completed = true);
        yield return new WaitUntil(() => completed);
    }

    private void Play(Vector2 startRange, Vector2 endRange, Vector2 startOffset, Vector2 endOffset, Action onComplete)
    {
        KillTweens();

        SetTransitionRange(startRange);
        RefreshGraphic();

        bool rangeCompleted = false;
        bool offsetCompleted = false;

        Vector2 currentRange = startRange;
        _rangeTween = DOTween.To(
                () => currentRange,
                value =>
                {
                    currentRange = value;
                    SetTransitionRange(currentRange);
                    RefreshGraphic();
                },
                endRange,
                _duration)
            .SetEase(_ease)
            .OnComplete(() =>
            {
                rangeCompleted = true;
                TryComplete();
            });

        Vector2 currentOffset = startOffset;
        _offsetTween = DOTween.To(
                () => currentOffset,
                value =>
                {
                    currentOffset = value;
                    RefreshGraphic();
                },
                endOffset,
                _duration)
            .SetEase(_ease)
            .OnComplete(() =>
            {
                offsetCompleted = true;
                TryComplete();
            });

        void TryComplete()
        {
            if (rangeCompleted && offsetCompleted)
                onComplete?.Invoke();
        }
    }

    private void KillTweens()
    {
        _rangeTween?.Kill();
        _offsetTween?.Kill();
        _rangeTween = null;
        _offsetTween = null;
    }

    private void SetTransitionRange(Vector2 value)
    {
        if (_uiEffect == null)
            return;

        var range = _uiEffect.transitionRange;
        range.min = value.x;
        range.max = value.y;
        _uiEffect.transitionRange = range;
    }

    private Vector2 GetTransitionRange()
    {
        if (_uiEffect == null)
            return Vector2.zero;

        var range = _uiEffect.transitionRange;
        return new Vector2(range.min, range.max);
    }

    private void RefreshGraphic()
    {
        if (_graphic == null)
            return;

        _graphic.SetMaterialDirty();
        _graphic.SetVerticesDirty();
        LayoutRebuilder.MarkLayoutForRebuild(_graphic.rectTransform);
    }
}