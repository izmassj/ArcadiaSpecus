using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class TMPButtonTweenAuto : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField] private Transform root;              // si null, usa este transform
    [SerializeField] private bool includeInactive = true; // también en panels ocultos

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float hoverDuration = 0.10f;
    [SerializeField] private Ease hoverEase = Ease.OutBack;

    [Header("Click")]
    [SerializeField] private float punchStrength = 0.10f; // escala relativa (0.10 = 10%)
    [SerializeField] private float punchDuration = 0.14f;
    [SerializeField] private int punchVibrato = 10;
    [SerializeField, Range(0f, 1f)] private float punchElasticity = 0.8f;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true; // para que funcione con Time.timeScale=0 (pausa)

    private void Awake()
    {
        Bind();
    }

    // Llamable si spawneas botones en runtime
    public void Bind()
    {
        Transform r = root != null ? root : transform;
        var buttons = r.GetComponentsInChildren<Button>(includeInactive);

        foreach (var b in buttons)
        {
            if (b == null) continue;

            // Solo “botones TMP”: tienen un TMP_Text (TextMeshProUGUI) en hijos
            if (b.GetComponentInChildren<TMP_Text>(includeInactive) == null)
                continue;

            var fx = b.GetComponent<TMPButtonTweenFX>();
            if (fx == null) fx = b.gameObject.AddComponent<TMPButtonTweenFX>();

            fx.Configure(
                hoverScale, hoverDuration, hoverEase,
                punchStrength, punchDuration, punchVibrato, punchElasticity,
                useUnscaledTime
            );
        }
    }
}

public class TMPButtonTweenFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Button _button;
    private RectTransform _rt;

    private Vector3 _baseScale;
    private bool _hovering;

    private Tween _scaleTween;
    private Tween _punchTween;

    // settings
    private float _hoverScale = 1.06f;
    private float _hoverDuration = 0.10f;
    private Ease _hoverEase = Ease.OutBack;

    private float _punchStrength = 0.10f;
    private float _punchDuration = 0.14f;
    private int _punchVibrato = 10;
    private float _punchElasticity = 0.8f;

    private bool _useUnscaledTime = true;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rt = transform as RectTransform;
        _baseScale = transform.localScale;
    }

    public void Configure(
        float hoverScale, float hoverDuration, Ease hoverEase,
        float punchStrength, float punchDuration, int punchVibrato, float punchElasticity,
        bool useUnscaledTime
    )
    {
        _hoverScale = hoverScale;
        _hoverDuration = hoverDuration;
        _hoverEase = hoverEase;

        _punchStrength = punchStrength;
        _punchDuration = punchDuration;
        _punchVibrato = punchVibrato;
        _punchElasticity = punchElasticity;

        _useUnscaledTime = useUnscaledTime;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        _hovering = true;
        TweenTo(GetHoverTargetScale());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        TweenTo(_baseScale);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        _punchTween?.Kill(false);

        // Punch relativo (se aplica sobre la escala actual)
        Vector3 punch = new Vector3(_punchStrength, _punchStrength, 0f);

        _punchTween = _rt.DOPunchScale(punch, _punchDuration, _punchVibrato, _punchElasticity)
            .SetUpdate(_useUnscaledTime)
            .OnKill(ForcePostState)
            .OnComplete(ForcePostState);
    }

    private bool IsInteractable()
    {
        return _button == null || _button.IsInteractable();
    }

    private Vector3 GetHoverTargetScale()
    {
        return _baseScale * _hoverScale;
    }

    private void TweenTo(Vector3 target)
    {
        _scaleTween?.Kill(false);

        _scaleTween = _rt.DOScale(target, _hoverDuration)
            .SetEase(_hoverEase)
            .SetUpdate(_useUnscaledTime);
    }

    private void ForcePostState()
    {
        // Evita “drift” si matas tweens a mitad
        transform.localScale = _hovering ? GetHoverTargetScale() : _baseScale;
    }

    private void OnDisable()
    {
        _scaleTween?.Kill(false);
        _punchTween?.Kill(false);
        transform.localScale = _baseScale;
        _hovering = false;
    }
}