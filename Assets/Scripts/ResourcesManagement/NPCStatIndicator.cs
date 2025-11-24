// NPCStatIndicator.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class NPCStatIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image statFill;
    public Image background;
    public TextMeshProUGUI statLetter;
    public GameObject alertIcon;

    [Header("Colors")]
    public Color fullColor = Color.green;      // Verde para valor bajo
    public Color mediumColor = Color.yellow;   // Amarillo para valor medio
    public Color lowColor = Color.red;         // Rojo para valor alto

    [Header("Hover Effects")]
    public bool enableHoverEffects = true;
    public float hoverScale = 1.1f;
    public float animationDuration = 0.2f;

    [Header("Config")]
    public float criticalThreshold = 70f;

    private float currentValue = 0f;
    private float maxValue = 100f;

    private bool isAlert = false;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    void Start()
    {
        originalScale = transform.localScale;

        if (alertIcon != null)
            alertIcon.SetActive(false);

        ConfigureFill();
    }

    /// <summary>
    /// Configura el componente de llenado para mostrar correctamente los valores
    /// </summary>
    void ConfigureFill()
    {
        statFill.type = Image.Type.Filled;
        statFill.fillMethod = Image.FillMethod.Vertical;
        statFill.fillOrigin = (int)Image.OriginVertical.Bottom;

        statFill.rectTransform.localScale = Vector3.one;
        statFill.rectTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Establece el valor actual del indicador
    /// </summary>
    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, maxValue);
        UpdateDisplay();
    }

    /// <summary>
    /// Actualiza la visualización del indicador
    /// </summary>
    public void UpdateDisplay()
    {
        float fill = currentValue / maxValue;
        statFill.fillAmount = fill;

        // COLORS INVERTIDOS:
        // VALOR BAJO  → VERDE
        // VALOR MEDIO → AMARILLO
        // VALOR ALTO  → ROJO
        if (fill < 0.3f)
        {
            statFill.color = fullColor; // Verde cuando está bien
        }
        else if (fill < 0.6f)
        {
            statFill.color = mediumColor; // Amarillo cuando está normal
        }
        else
        {
            statFill.color = lowColor; // Rojo cuando es crítico
        }

        // ALERTA CRÍTICA (si supera el umbral)
        if (currentValue >= criticalThreshold)
        {
            if (!isAlert)
            {
                isAlert = true;
                if (alertIcon != null) alertIcon.SetActive(true);
                PlayCriticalAnimation();
            }
        }
        else
        {
            if (isAlert)
            {
                isAlert = false;
                if (alertIcon != null) alertIcon.SetActive(false);
                StopCriticalAnimation();
            }
        }
    }

    /// <summary>
    /// Inicia la animación de estado crítico
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
    /// Animación de pulso para estado crítico
    /// </summary>
    IEnumerator PulseAnimation()
    {
        while (isAlert)
        {
            yield return StartCoroutine(ScaleAnimation(originalScale * 1.15f, 0.25f));
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Animación de escala genérica
    /// </summary>
    IEnumerator ScaleAnimation(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, t / duration);
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
}