using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra un texto temporal (tipo toast) en pantalla.
/// Se usa para enseñar recompensas al volver de minijuegos.
/// </summary>
public class RewardToastManager : MonoBehaviour
{
    public static RewardToastManager Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float _visibleSeconds = 2.5f;
    [SerializeField] private float _fadeSeconds = 0.6f;

    [Header("Layout")]
    [SerializeField] private Vector2 _anchoredPosition = new Vector2(0f, -60f);
    [SerializeField] private float _maxWidth = 900f;
    [SerializeField] private int _fontSize = 30;

    private Canvas _canvas;
    private TextMeshProUGUI _text;
    private Coroutine _routine;

    public static RewardToastManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        RewardToastManager found = FindObjectOfType<RewardToastManager>(true);
        if (found != null)
        {
            Instance = found;
            DontDestroyOnLoad(found.gameObject);
            found.EnsureUI();
            return found;
        }

        GameObject go = new GameObject("[RewardToastManager]");
        RewardToastManager created = go.AddComponent<RewardToastManager>();
        DontDestroyOnLoad(go);
        created.EnsureUI();
        return created;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUI();
    }

    private void EnsureUI()
    {
        if (_canvas != null && _text != null)
            return;

        // Canvas overlay
        GameObject canvasGO = new GameObject("RewardToastCanvas");
        canvasGO.transform.SetParent(transform, false);

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Texto
        GameObject textGO = new GameObject("RewardToastText");
        textGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = _anchoredPosition;
        rt.sizeDelta = new Vector2(_maxWidth, 200f);

        _text = textGO.AddComponent<TextMeshProUGUI>();
        _text.fontSize = _fontSize;
        _text.alignment = TextAlignmentOptions.Top;
        _text.enableWordWrapping = true;
        _text.raycastTarget = false;
        _text.text = string.Empty;
        _text.color = new Color(0.2f, 1f, 0.35f, 0f); // verde con alpha 0
    }

    public void ShowGreenRewards(System.Collections.Generic.Dictionary<ResourceType, int> gains)
    {
        if (gains == null || gains.Count == 0)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("RECOMPENSA");
        foreach (var pair in gains)
        {
            sb.Append('+');
            sb.Append(pair.Value);
            sb.Append(' ');
            sb.Append(pair.Key);
            sb.AppendLine();
        }

        Show(sb.ToString().TrimEnd(), new Color(0.2f, 1f, 0.35f, 1f));
    }

    public void Show(string message, Color color)
    {
        EnsureUI();

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(ShowRoutine(message, color));
    }

    private IEnumerator ShowRoutine(string message, Color color)
    {
        if (_text == null)
            yield break;

        _text.text = message;

        // Fade in instant
        color.a = 1f;
        _text.color = color;

        float visible = Mathf.Max(0.1f, _visibleSeconds);
        yield return new WaitForSecondsRealtime(visible);

        float fade = Mathf.Max(0.05f, _fadeSeconds);
        float t = 0f;
        Color start = _text.color;

        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fade);
            _text.color = new Color(start.r, start.g, start.b, a);
            yield return null;
        }

        _text.color = new Color(start.r, start.g, start.b, 0f);
        _text.text = string.Empty;
        _routine = null;
    }
}
