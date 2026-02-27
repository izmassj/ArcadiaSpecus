using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Indicador visual de recurso (cantidad + barra + alerta).
/// Refactorizado para evitar fugas de eventos y null refs.
/// </summary>
public class ResourceIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias UI")]
    public Image resourceFill;
    public Image indicatorBackground;
    public Image resourceIcon;
    public TextMeshProUGUI amountText;
    public GameObject alertIcon;

    [Header("Configuración")]
    public ResourceType resourceType;
    public Color fullColor = Color.green;
    public Color mediumColor = Color.yellow;
    public Color lowColor = Color.red;

    [Header("Hover")]
    public bool enableHoverEffects = true;
    public float hoverScale = 1.1f;
    public float animationDuration = 0.2f;

    [Header("Escalado barra")]
    [SerializeField] private int _fallbackMaxAmount = 100;
    [SerializeField] private bool _autoGrowMaxAmount = true;

    private int _currentAmount;
    private int _maxAmount;
    private bool _isAlertActive;
    private Vector3 _originalScale;
    private Coroutine _scaleCoroutine;
    private bool _isSubscribed;

    private void Awake()
    {
        _originalScale = transform.localScale;
        ConfigureFillComponent();

        if (alertIcon != null)
        {
            alertIcon.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SubscribeEvents();
        EmergencyUpdate();
    }

    private void Start()
    {
        // Segunda sincronización por si ResourceManager se crea en Start de otro objeto.
        EmergencyUpdate();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopAllIndicatorAnimations();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        ResourceManager.OnResourceChanged += OnResourceChanged;
        ResourceManager.OnResourceCritical += OnResourceCritical;
        ResourceManager.OnResourceSafe += OnResourceSafe;
        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        ResourceManager.OnResourceChanged -= OnResourceChanged;
        ResourceManager.OnResourceCritical -= OnResourceCritical;
        ResourceManager.OnResourceSafe -= OnResourceSafe;
        _isSubscribed = false;
    }

    private void ConfigureFillComponent()
    {
        if (resourceFill == null)
        {
            return;
        }

        resourceFill.type = Image.Type.Filled;
        resourceFill.fillMethod = Image.FillMethod.Vertical;
        resourceFill.fillOrigin = (int)Image.OriginVertical.Bottom;

        RectTransform _rt = resourceFill.rectTransform;
        _rt.localRotation = Quaternion.identity;

        // Normalizamos escala local del fill para evitar artefactos de jerarquías invertidas.
        Vector3 _scale = _rt.localScale;
        _rt.localScale = new Vector3(Mathf.Abs(_scale.x), Mathf.Abs(_scale.y), Mathf.Abs(_scale.z));
    }

    private void OnResourceChanged(ResourceType _type, int _amount)
    {
        if (_type != resourceType)
        {
            return;
        }

        _currentAmount = Mathf.Max(0, _amount);

        if (_autoGrowMaxAmount)
        {
            _maxAmount = Mathf.Max(_maxAmount, _currentAmount);
        }

        UpdateDisplay();
        PlayChangeAnimation();
        CheckAlertStatus();
    }

    private void OnResourceCritical(ResourceType _type)
    {
        if (_type != resourceType)
        {
            return;
        }

        _isAlertActive = true;
        ShowAlert(true);
        PlayCriticalAnimation();
    }

    private void OnResourceSafe(ResourceType _type)
    {
        if (_type != resourceType)
        {
            return;
        }

        _isAlertActive = false;
        ShowAlert(false);
        StopCriticalAnimation();
    }

    private void UpdateDisplay()
    {
        if (amountText != null)
        {
            amountText.text = _currentAmount.ToString();
        }

        if (resourceFill == null)
        {
            return;
        }

        int _safeMax = Mathf.Max(1, _maxAmount);
        float _fill = Mathf.Clamp01((float)_currentAmount / _safeMax);
        resourceFill.fillAmount = _fill;

        if (_fill > 0.6f)
        {
            resourceFill.color = fullColor;
        }
        else if (_fill > 0.3f)
        {
            resourceFill.color = mediumColor;
        }
        else
        {
            resourceFill.color = lowColor;
        }
    }

    private void CheckAlertStatus()
    {
        if (ResourceManager.Instance == null)
        {
            return;
        }

        int _minimum = ResourceManager.Instance.GetMinimumLevel(resourceType);

        if (_currentAmount <= _minimum && !_isAlertActive)
        {
            _isAlertActive = true;
            ShowAlert(true);
            PlayCriticalAnimation();
        }
        else if (_currentAmount > _minimum && _isAlertActive)
        {
            _isAlertActive = false;
            ShowAlert(false);
            StopCriticalAnimation();
        }
    }

    private int CalculateMaxAmount()
    {
        if (ResourceManager.Instance == null)
        {
            return Mathf.Max(1, _fallbackMaxAmount, _currentAmount);
        }

        int _warningLevel = ResourceManager.Instance.GetWarningLevel(resourceType);
        return Mathf.Max(1, _currentAmount, _fallbackMaxAmount, _warningLevel * 3);
    }

    private void ShowAlert(bool _show)
    {
        if (alertIcon != null)
        {
            alertIcon.SetActive(_show);
        }
    }

    private void PlayChangeAnimation()
    {
        if (!enableHoverEffects || !isActiveAndEnabled)
        {
            return;
        }

        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(ScaleAnimation(_originalScale * 1.05f, 0.1f));
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

    private void StopAllIndicatorAnimations()
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }

        transform.localScale = _originalScale;
    }

    private IEnumerator ScaleAnimation(Vector3 _targetScale, float _duration)
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

    private IEnumerator PulseAnimation()
    {
        while (_isAlertActive && isActiveAndEnabled)
        {
            yield return StartCoroutine(ScaleAnimation(_originalScale * 1.15f, 0.3f));
            yield return new WaitForSecondsRealtime(0.1f);
        }
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

    [ContextMenu("EMERGENCY UPDATE")]
    public void EmergencyUpdate()
    {
        if (ResourceManager.Instance == null)
        {
            _currentAmount = 0;
            _maxAmount = Mathf.Max(1, _fallbackMaxAmount);
            UpdateDisplay();
            ShowAlert(false);
            return;
        }

        _currentAmount = Mathf.Max(0, ResourceManager.Instance.GetResourceAmount(resourceType));
        _maxAmount = CalculateMaxAmount();
        _isAlertActive = _currentAmount <= ResourceManager.Instance.GetMinimumLevel(resourceType);

        UpdateDisplay();
        ShowAlert(_isAlertActive);

        if (_isAlertActive)
        {
            PlayCriticalAnimation();
        }
        else
        {
            StopCriticalAnimation();
        }
    }
}
