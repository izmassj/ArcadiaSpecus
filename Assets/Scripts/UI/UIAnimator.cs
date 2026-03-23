using DG.Tweening;
using UnityEngine;

public class UIAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Default Settings")]
    [SerializeField] private float _defaultDuration;
    [SerializeField] private Ease _defaultEase;
    [SerializeField] private bool _ignoreTimeScale;

    [Header("Saved States")]
    [SerializeField] private Vector2 _savedAnchorPos;
    [SerializeField] private Vector3 _savedScale;
    [SerializeField] private Vector3 _savedRotation;
    [SerializeField] private float _savedAlpha;

    [Header("Shake Settings")]
    [SerializeField] private float _shakeMultiplier = 1f;

    private Tween _moveTween;
    private Tween _scaleTween;
    private Tween _rotateTween;
    private Tween _fadeTween;

    private void Reset()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        SaveCurrentState();
    }

    public void SaveCurrentState()
    {
        _savedAnchorPos = _rectTransform.anchoredPosition;
        _savedScale = _rectTransform.localScale;
        _savedRotation = _rectTransform.localEulerAngles;

        if (_canvasGroup != null)
            _savedAlpha = _canvasGroup.alpha;
    }

    public void KillAllTweens()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();
        _rotateTween?.Kill();
        _fadeTween?.Kill();
    }

    public void CompleteAllTweens()
    {
        _moveTween?.Complete();
        _scaleTween?.Complete();
        _rotateTween?.Complete();
        _fadeTween?.Complete();
    }

    public void MoveTo(Vector2 target)
    {
        MoveTo(target, _defaultDuration);
    }

    public void MoveTo(Vector2 target, float duration)
    {
        _moveTween?.Kill();
        _moveTween = _rectTransform
            .DOAnchorPos(target, duration)
            .SetEase(_defaultEase)
            .SetUpdate(_ignoreTimeScale);
    }

    public void MoveX(float x)
    {
        MoveX(x, _defaultDuration);
    }

    public void MoveX(float x, float duration)
    {
        _moveTween?.Kill();
        _moveTween = _rectTransform
            .DOAnchorPosX(x, duration)
            .SetEase(_defaultEase)
            .SetUpdate(_ignoreTimeScale);
    }

    public void MoveY(float y)
    {
        MoveY(y, _defaultDuration);
    }

    public void MoveY(float y, float duration)
    {
        _moveTween?.Kill();
        _moveTween = _rectTransform
            .DOAnchorPosY(y, duration)
            .SetEase(_defaultEase)
            .SetUpdate(_ignoreTimeScale);
    }

    public void MoveToSavedPosition()
    {
        MoveTo(_savedAnchorPos, _defaultDuration);
    }

    public void SetPositionInstant(Vector2 target)
    {
        _moveTween?.Kill();
        _rectTransform.anchoredPosition = target;
    }

    public void ScaleTo(Vector3 target)
    {
        ScaleTo(target, _defaultDuration);
    }

    public void ScaleTo(Vector3 target, float duration)
    {
        _scaleTween?.Kill();
        _scaleTween = _rectTransform
            .DOScale(target, duration)
            .SetEase(_defaultEase)
            .SetUpdate(_ignoreTimeScale);
    }

    public void ScaleTo(float uniformScale)
    {
        ScaleTo(Vector3.one * uniformScale, _defaultDuration);
    }

    public void ScaleToSaved()
    {
        ScaleTo(_savedScale, _defaultDuration);
    }

    public void SetScaleInstant(Vector3 target)
    {
        _scaleTween?.Kill();
        _rectTransform.localScale = target;
    }

    public void RotateTo(Vector3 targetEuler)
    {
        RotateTo(targetEuler, _defaultDuration);
    }

    public void RotateTo(Vector3 targetEuler, float duration)
    {
        _rotateTween?.Kill();
        _rotateTween = _rectTransform
            .DOLocalRotate(targetEuler, duration)
            .SetEase(_defaultEase)
            .SetUpdate(_ignoreTimeScale);
    }

    public void RotateToSaved()
    {
        RotateTo(_savedRotation, _defaultDuration);
    }

    public void SetRotationInstant(Vector3 targetEuler)
    {
        _rotateTween?.Kill();
        _rectTransform.localEulerAngles = targetEuler;
    }

    public void RotateXTo(float x)
    {
        RotateXTo(x, _defaultDuration);
    }

    public void RotateXTo(float x, float duration)
    {
        Vector3 target = _rectTransform.localEulerAngles;
        target.x = x;
        RotateTo(target, duration);
    }

    public void RotateYTo(float y)
    {
        RotateYTo(y, _defaultDuration);
    }

    public void RotateYTo(float y, float duration)
    {
        Vector3 target = _rectTransform.localEulerAngles;
        target.y = y;
        RotateTo(target, duration);
    }

    public void RotateZTo(float z)
    {
        RotateZTo(z, _defaultDuration);
    }

    public void RotateZTo(float z, float duration)
    {
        Vector3 target = _rectTransform.localEulerAngles;
        target.z = z;
        RotateTo(target, duration);
    }

    public void FadeTo(float alpha)
    {
        FadeTo(alpha, _defaultDuration);
    }

    public void FadeTo(float alpha, float duration)
    {
        if (_canvasGroup == null)
            return;

        _fadeTween?.Kill();
        _fadeTween = _canvasGroup
            .DOFade(alpha, duration)
            .SetEase(_defaultEase)
            .SetUpdate(_ignoreTimeScale);
    }

    public void FadeIn()
    {
        FadeTo(1f, _defaultDuration);
    }

    public void FadeOut()
    {
        FadeTo(0f, _defaultDuration);
    }

    public void FadeToSaved()
    {
        FadeTo(_savedAlpha, _defaultDuration);
    }

    public void SetAlphaInstant(float alpha)
    {
        if (_canvasGroup == null)
            return;

        _fadeTween?.Kill();
        _canvasGroup.alpha = alpha;
    }

    public void PunchScale(Vector3 strength)
    {
        PunchScale(strength, 0.25f, 8, 0.8f);
    }

    public void PunchScale(Vector3 strength, float duration, int vibrato, float elasticity)
    {
        _scaleTween?.Kill();
        _scaleTween = _rectTransform
            .DOPunchScale(strength, duration, vibrato, elasticity)
            .SetUpdate(_ignoreTimeScale);
    }

    public void SetShakeMultiplier(float multiplier)
    {
        _shakeMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ShakePosition()
    {
        ShakePosition(0.3f, 20f, 10, 90f, false);
    }

    public void ShakePosition(float duration, float strength, int vibrato, float randomness, bool fadeOut = false)
    {
        _moveTween?.Kill();
        _moveTween = _rectTransform
            .DOShakeAnchorPos(duration, strength * _shakeMultiplier, vibrato, randomness, false, fadeOut)
            .SetUpdate(_ignoreTimeScale);
    }

    public void ShakeRotation()
    {
        ShakeRotation(0.3f, new Vector3(0f, 0f, 15f), 10, 90f, false);
    }

    public void ShakeRotation(float duration, Vector3 strength, int vibrato, float randomness, bool fadeOut = false)
    {
        _rotateTween?.Kill();
        _rotateTween = _rectTransform
            .DOShakeRotation(duration, strength * _shakeMultiplier, vibrato, randomness, fadeOut)
            .SetUpdate(_ignoreTimeScale);
    }


    public void ShakePositionAndRotation()
    {
        ShakePositionAndRotation(0.3f, 20f, new Vector3(0f, 0f, 15f), 10, 90f, false);
    }

    public void ShakePositionAndRotation(float duration, float positionStrength, Vector3 rotationStrength, int vibrato, float randomness, bool fadeOut = false)
    {
        ShakePosition(duration, positionStrength, vibrato, randomness, fadeOut);
        ShakeRotation(duration, rotationStrength, vibrato, randomness, fadeOut);
    }

    public void BouncePosition(Vector2 offset)
    {
        BouncePosition(offset, 0.4f);
    }

    public void BouncePosition(Vector2 offset, float duration)
    {
        _moveTween?.Kill();
        _moveTween = _rectTransform
            .DOAnchorPos(_savedAnchorPos + offset, duration * 0.5f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(_ignoreTimeScale)
            .OnComplete(() =>
            {
                _moveTween = _rectTransform
                    .DOAnchorPos(_savedAnchorPos, duration * 0.5f)
                    .SetEase(Ease.OutBounce)
                    .SetUpdate(_ignoreTimeScale);
            });
    }

    public void BounceX(float offset)
    {
        BouncePosition(new Vector2(offset, 0f), 0.4f);
    }

    public void BounceY(float offset)
    {
        BouncePosition(new Vector2(0f, offset), 0.4f);
    }

    public void AnimateInFromLeft(float offset)
    {
        Vector2 target = _savedAnchorPos;
        Vector2 start = new Vector2(_savedAnchorPos.x - offset, _savedAnchorPos.y);

        SetPositionInstant(start);
        MoveTo(target, _defaultDuration);
    }

    public void AnimateInFromRight(float offset)
    {
        Vector2 target = _savedAnchorPos;
        Vector2 start = new Vector2(_savedAnchorPos.x + offset, _savedAnchorPos.y);

        SetPositionInstant(start);
        MoveTo(target, _defaultDuration);
    }

    public void AnimateInFromTop(float offset)
    {
        Vector2 target = _savedAnchorPos;
        Vector2 start = new Vector2(_savedAnchorPos.x, _savedAnchorPos.y + offset);

        SetPositionInstant(start);
        MoveTo(target, _defaultDuration);
    }

    public void AnimateInFromBottom(float offset)
    {
        Vector2 target = _savedAnchorPos;
        Vector2 start = new Vector2(_savedAnchorPos.x, _savedAnchorPos.y - offset);

        SetPositionInstant(start);
        MoveTo(target, _defaultDuration);
    }

    public void PopIn(float startScale = 0.8f)
    {
        SetScaleInstant(Vector3.one * startScale);
        ScaleToSaved();
    }

    public void PopOut(float targetScale = 0.8f)
    {
        ScaleTo(Vector3.one * targetScale, _defaultDuration);
    }
}