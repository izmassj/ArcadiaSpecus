// ResourceIndicator.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

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

    private int currentAmount;
    private int maxAmount;
    private bool isAlertActive = false;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    void Start()
    {
        originalScale = transform.localScale;

        if (alertIcon != null)
            alertIcon.SetActive(false);

        ConfigureFillComponent();

        // Suscribirse a eventos del ResourceManager
        ResourceManager.OnResourceChanged += OnResourceChanged;
        ResourceManager.OnResourceCritical += OnResourceCritical;
        ResourceManager.OnResourceSafe += OnResourceSafe;

        if (ResourceManager.Instance != null)
        {
            currentAmount = ResourceManager.Instance.GetResourceAmount(resourceType);
            maxAmount = CalculateMaxAmount();
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Configura el componente de llenado para mostrar correctamente los valores
    /// </summary>
    void ConfigureFillComponent()
    {
        resourceFill.type = Image.Type.Filled;
        resourceFill.fillMethod = Image.FillMethod.Vertical;
        resourceFill.fillOrigin = (int)Image.OriginVertical.Bottom;

        var rt = resourceFill.rectTransform;
        rt.localScale = new Vector3(1, 1, 1);
        rt.localRotation = Quaternion.identity;

        // Asegurar que no haya escalas negativas en la jerarquía
        Transform t = resourceFill.transform;
        while (t.parent != null)
        {
            if (t.parent.localScale.y < 0)
                t.parent.localScale = new Vector3(t.parent.localScale.x, Mathf.Abs(t.parent.localScale.y), t.parent.localScale.z);

            t = t.parent;
        }
    }

    /// <summary>
    /// Maneja el evento cuando cambia un recurso
    /// </summary>
    void OnResourceChanged(ResourceType type, int amount)
    {
        if (type == resourceType)
        {
            currentAmount = amount;
            UpdateDisplay();
            PlayChangeAnimation();
            CheckAlertStatus();
        }
    }

    /// <summary>
    /// Maneja el evento cuando un recurso está crítico
    /// </summary>
    void OnResourceCritical(ResourceType type)
    {
        if (type == resourceType)
        {
            isAlertActive = true;
            ShowAlert(true);
            PlayCriticalAnimation();
        }
    }

    /// <summary>
    /// Maneja el evento cuando un recurso vuelve a nivel seguro
    /// </summary>
    void OnResourceSafe(ResourceType type)
    {
        if (type == resourceType)
        {
            isAlertActive = false;
            ShowAlert(false);
            StopCriticalAnimation();
        }
    }

    /// <summary>
    /// Actualiza la visualización del indicador
    /// </summary>
    void UpdateDisplay()
    {
        if (amountText != null)
            amountText.text = currentAmount.ToString();

        float fill = Mathf.Clamp01((float)currentAmount / maxAmount);
        resourceFill.fillAmount = fill;

        // Cambiar color basado en el nivel de llenado
        if (fill > 0.6f) resourceFill.color = fullColor;
        else if (fill > 0.3f) resourceFill.color = mediumColor;
        else resourceFill.color = lowColor;

        // Ajustar máximo si es necesario
        if (currentAmount > maxAmount)
            maxAmount = currentAmount;
    }

    /// <summary>
    /// Verifica el estado de alerta del recurso
    /// </summary>
    void CheckAlertStatus()
    {
        int minimum = ResourceManager.Instance.GetMinimumLevel(resourceType);

        if (currentAmount <= minimum && !isAlertActive)
        {
            isAlertActive = true;
            ShowAlert(true);
            PlayCriticalAnimation();
        }
        else if (currentAmount > minimum && isAlertActive)
        {
            isAlertActive = false;
            ShowAlert(false);
            StopCriticalAnimation();
        }
    }

    /// <summary>
    /// Calcula el máximo amount para escalado
    /// </summary>
    int CalculateMaxAmount()
    {
        int warningLevel = ResourceManager.Instance.GetWarningLevel(resourceType);
        return Mathf.Max(currentAmount, warningLevel * 3);
    }

    /// <summary>
    /// Muestra u oculta el icono de alerta
    /// </summary>
    void ShowAlert(bool show)
    {
        if (alertIcon != null)
            alertIcon.SetActive(show);
    }

    /// <summary>
    /// Reproduce animación de cambio de valor
    /// </summary>
    void PlayChangeAnimation()
    {
        if (!enableHoverEffects) return;

        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale * 1.05f, 0.1f));
    }

    /// <summary>
    /// Reproduce animación de estado crítico
    /// </summary>
    void PlayCriticalAnimation()
    {
        if (!enableHoverEffects) return;

        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(PulseAnimation());
    }

    /// <summary>
    /// Detiene la animación de estado crítico
    /// </summary>
    void StopCriticalAnimation()
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        transform.localScale = originalScale;
    }

    /// <summary>
    /// Animación de escala genérica
    /// </summary>
    IEnumerator ScaleAnimation(Vector3 targetScale, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, targetScale, t / duration);
            yield return null;
        }

        t = 0f;
        start = transform.localScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, originalScale, t / duration);
            yield return null;
        }
    }

    /// <summary>
    /// Animación de pulso para estado crítico
    /// </summary>
    IEnumerator PulseAnimation()
    {
        while (isAlertActive)
        {
            yield return StartCoroutine(ScaleAnimation(originalScale * 1.15f, 0.3f));
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Maneja el evento de entrar el puntero
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableHoverEffects) return;

        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(HoverAnimation(originalScale * hoverScale, animationDuration));
    }

    /// <summary>
    /// Maneja el evento de salir el puntero
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableHoverEffects) return;

        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(HoverAnimation(originalScale, animationDuration));
    }

    /// <summary>
    /// Animación de hover suave
    /// </summary>
    IEnumerator HoverAnimation(Vector3 targetScale, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, targetScale, t / duration);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    /// <summary>
    /// Actualización de emergencia para forzar sincronización
    /// </summary>
    [ContextMenu("EMERGENCY UPDATE")]
    public void EmergencyUpdate()
    {
        Debug.Log($"ACTUALIZACIÓN DE EMERGENCIA PARA {gameObject.name}");

        if (ResourceManager.Instance != null)
        {
            currentAmount = ResourceManager.Instance.GetResourceAmount(resourceType);
            maxAmount = CalculateMaxAmount();
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Sincronización de emergencia con configuración
    /// </summary>
    [ContextMenu("SINCRONIZAR CON CONFIGURACIÓN")]
    public void EmergencySync()
    {
        EmergencyUpdate();
    }

    /// <summary>
    /// Limpia las suscripciones a eventos al destruir el objeto
    /// </summary>
    void OnDestroy()
    {
        ResourceManager.OnResourceChanged -= OnResourceChanged;
        ResourceManager.OnResourceCritical -= OnResourceCritical;
        ResourceManager.OnResourceSafe -= OnResourceSafe;
    }
}