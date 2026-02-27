using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Indicador vertical para hambre/sed/fatiga del NPC.
/// Muestra alerta cuando el valor es alto (más crítico).
/// </summary>
public class NPCStatIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image statFill;
    public Image background;
    public TextMeshProUGUI statLetter;
    public GameObject alertIcon;

    [Header("Colores")]
    public Color fullColor = Color.green;     // Valor bajo (bien)
    public Color mediumColor = Color.yellow;  // Valor medio
    public Color lowColor = Color.red;        // Valor alto (mal)

    [Header("Hover")]
    public bool enableHoverEffects = true;
    public float hoverScale = 1.1f;
    public float animationDuration = 0.2f;

    [Header("Configuración")]
    public float criticalThreshold = 70f;
    [SerializeField] private float _maxValue = 100f;

    private float _currentValue;
    private bool _isAlert;
    private Vector3 _originalScale;
    private Coroutine _scaleCoroutine;

    private void Awake()
    {
        _originalScale = transform.localScale;
        ConfigureFill();

        if (alertIcon != null)
        {
            alertIcon.SetActive(false);
        }
    }

    private void OnEnable()
    {
        UpdateDisplay();
    }

    private void OnDisable()
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }

        transform.localScale = _originalScale;
    }

    private void ConfigureFill()
    {
        if (statFill == null)
        {
            return;
        }

        statFill.type = Image.Type.Filled;
        statFill.fillMethod = Image.FillMethod.Vertical;
        statFill.fillOrigin = (int)Image.OriginVertical.Bottom;

        statFill.rectTransform.localScale = Vector3.one;
        statFill.rectTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Establece el valor actual del indicador (0..100 por defecto).
    /// </summary>
    public void SetValue(float _value)
    {
        _currentValue = Mathf.Clamp(_value, 0f, Mathf.Max(1f, _maxValue));
        UpdateDisplay();
    }

    public void SetMaxValue(float _value)
    {
        _maxValue = Mathf.Max(1f, _value);
        _currentValue = Mathf.Clamp(_currentValue, 0f, _maxValue);
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (statFill == null)
        {
            return;
        }

        float _fill = Mathf.Clamp01(_currentValue / Mathf.Max(1f, _maxValue));
        statFill.fillAmount = _fill;

        // Valores altos = peor estado (se mantiene la lógica visual que ya usabais).
        if (_fill < 0.3f)
        {
            statFill.color = fullColor;
        }
        else if (_fill < 0.6f)
        {
            statFill.color = mediumColor;
        }
        else
        {
            statFill.color = lowColor;
        }

        bool _shouldAlert = _currentValue >= criticalThreshold;
        if (_shouldAlert != _isAlert)
        {
            _isAlert = _shouldAlert;
            if (alertIcon != null)
            {
                alertIcon.SetActive(_isAlert);
            }

            if (_isAlert)
            {
                PlayCriticalAnimation();
            }
            else
            {
                StopCriticalAnimation();
            }
        }
    }

    private void PlayCriticalAnimation()
    {
        if (!enableHoverEffects || !isActiveAndEnabled)
        {
            return;
        }

        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(PulseAnimation());
    }

    private void StopCriticalAnimation()
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }

        transform.localScale = _originalScale;
    }

    private IEnumerator PulseAnimation()
    {
        while (_isAlert && isActiveAndEnabled)
        {
            yield return StartCoroutine(ScaleAnimation(_originalScale * 1.15f, 0.25f));
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    private IEnumerator ScaleAnimation(Vector3 _target, float _duration)
    {
        Vector3 _start = transform.localScale;
        float _t = 0f;
        float _safeDuration = Mathf.Max(0.01f, _duration);

        while (_t < _safeDuration)
        {
            _t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(_start, _target, _t / _safeDuration);
            yield return null;
        }

        _t = 0f;
        _start = transform.localScale;

        while (_t < _safeDuration)
        {
            _t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(_start, _originalScale, _t / _safeDuration);
            yield return null;
        }

        transform.localScale = _originalScale;
    }

    public void OnPointerEnter(PointerEventData _eventData)
    {
        if (!enableHoverEffects || !isActiveAndEnabled)
        {
            return;
        }

        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(HoverAnimation(_originalScale * hoverScale, animationDuration));
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        if (!enableHoverEffects || !isActiveAndEnabled)
        {
            return;
        }

        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(HoverAnimation(_originalScale, animationDuration));
    }

    private IEnumerator HoverAnimation(Vector3 _targetScale, float _duration)
    {
        Vector3 _start = transform.localScale;
        float _t = 0f;
        float _safeDuration = Mathf.Max(0.01f, _duration);

        while (_t < _safeDuration)
        {
            _t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(_start, _targetScale, _t / _safeDuration);
            yield return null;
        }

        transform.localScale = _targetScale;
        _scaleCoroutine = null;
    }
}
