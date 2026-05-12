using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonScaleFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("Settings")]
    [SerializeField] private float _hoverScale = 1.06f;
    [SerializeField] private float _pressedScale = 0.94f;
    [SerializeField] private float _hoverDuration = 0.12f;
    [SerializeField] private float _pressedDuration = 0.07f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private bool _ignoreTimeScale = true;

    private RectTransform _rectTransform;
    private Button _button;
    private Vector3 _baseScale;
    private Tween _scaleTween;
    private bool _isHovering;
    private bool _isPressed;
    private bool _initialized;

    private void Awake()
    {
        Cache();
    }

    private void OnEnable()
    {
        Cache();
        _baseScale = _rectTransform.localScale;
        _initialized = true;
    }

    private void OnDisable()
    {
        _scaleTween?.Kill();

        if (_initialized && _rectTransform != null)
            _rectTransform.localScale = _baseScale;

        _isHovering = false;
        _isPressed = false;
    }

    private void Cache()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_button == null)
            _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        _isHovering = true;

        if (!_isPressed)
            ScaleTo(_baseScale * _hoverScale, _hoverDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        _isHovering = false;

        if (!_isPressed)
            ScaleTo(_baseScale, _hoverDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        _isPressed = true;
        ScaleTo(_baseScale * _pressedScale, _pressedDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        _isPressed = false;

        if (_isHovering)
            ScaleTo(_baseScale * _hoverScale, _hoverDuration);
        else
            ScaleTo(_baseScale, _hoverDuration);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!CanAnimate())
            return;

        _isHovering = true;
        ScaleTo(_baseScale * _hoverScale, _hoverDuration);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!CanAnimate())
            return;

        _isHovering = false;
        _isPressed = false;
        ScaleTo(_baseScale, _hoverDuration);
    }

    private bool CanAnimate()
    {
        if (_rectTransform == null)
            return false;

        if (_button != null && !_button.interactable)
            return false;

        return true;
    }

    private void ScaleTo(Vector3 targetScale, float duration)
    {
        _scaleTween?.Kill();
        _scaleTween = _rectTransform
            .DOScale(targetScale, duration)
            .SetEase(_ease)
            .SetUpdate(_ignoreTimeScale);
    }
}