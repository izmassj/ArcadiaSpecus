using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

/// <summary>

/// - Spawnea un prefab de ParticleSystem (que tú creas) y emite bursts.
/// - No crea materiales/texturas por código.
/// - Compatible con URP/Built-in porque el look lo define el prefab.
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Prefab source")]
    [Tooltip("Si lo dejas vacío, intentará cargarlo desde Resources.")]
    [SerializeField] private GameObject _baseVfxPrefab;

    [Tooltip("Ruta dentro de Resources (sin .prefab). Default: VFX/VFX_Base")]
    [SerializeField] private string _resourcesPath = "VFX/GeneralParticles";

    [Header("General")]
    [SerializeField] private bool _dontDestroyOnLoad = true;
    [SerializeField] private bool _verboseLogs = false;

    [Header("Camera spawn")]
    [SerializeField] private float _defaultZOffsetFromCamera = 2.0f;

    [Header("Scale")]
    [Tooltip("Escala global multiplicativa del prefab instanciado.")]
    [SerializeField] private float _globalScale = 1.0f;

    [Serializable]
    public class VFXEntry
    {
        public VFXKind kind;

        [Min(1)] public int emitCount = 25;

        public Color tint = Color.white;

        [Tooltip("Multiplica el startSize del prefab (aprox).")]
        [Min(0.01f)] public float sizeMultiplier = 1.0f;

        [Tooltip("Multiplica la escala del GameObject instanciado.")]
        [Min(0.01f)] public float scaleMultiplier = 1.0f;

        [Tooltip("Si 0, se calcula automáticamente con la lifetime del prefab.")]
        [Min(0f)] public float destroyAfterSeconds = 0f;
    }

    // Defaults (se te rellenan si creas el componente nuevo)
    [SerializeField]
    private VFXEntry[] _entries = new VFXEntry[]
    {
        new VFXEntry { kind = VFXKind.ResourceCollect, emitCount = 22, tint = new Color(1f, 0.9f, 0.2f, 1f), sizeMultiplier = 1.2f, scaleMultiplier = 1.0f },
        new VFXEntry { kind = VFXKind.MachinePlaced,   emitCount = 28, tint = new Color(0.2f, 1f, 0.4f, 1f), sizeMultiplier = 1.1f, scaleMultiplier = 1.0f },
        new VFXEntry { kind = VFXKind.ResourceCritical,emitCount = 55, tint = new Color(1f, 0.2f, 0.2f, 1f), sizeMultiplier = 1.3f, scaleMultiplier = 1.0f },
        new VFXEntry { kind = VFXKind.GameOver,        emitCount = 70, tint = new Color(0.7f,0.7f,0.7f, 0.9f), sizeMultiplier = 1.4f, scaleMultiplier = 1.0f },
    };

    private readonly Dictionary<VFXKind, VFXEntry> _map = new Dictionary<VFXKind, VFXEntry>();
    private bool _triedLoadFromResources = false;

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        VFXManager existing = FindObjectOfType<VFXManager>(true);
        if (existing != null)
        {
            Instance = existing;
            if (existing._dontDestroyOnLoad) DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("VFXManager");
        Instance = go.AddComponent<VFXManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (_dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        RebuildMap();
        TryLoadPrefabIfNeeded();
    }

    private void OnValidate()
    {
        RebuildMap();
    }

    private void RebuildMap()
    {
        _map.Clear();
        if (_entries == null) return;

        foreach (var e in _entries)
        {
            if (e == null) continue;
            _map[e.kind] = e;
        }
    }

    private void TryLoadPrefabIfNeeded()
    {
        if (_baseVfxPrefab != null) return;
        if (_triedLoadFromResources) return;

        _triedLoadFromResources = true;

        if (!string.IsNullOrWhiteSpace(_resourcesPath))
        {
            _baseVfxPrefab = Resources.Load<GameObject>(_resourcesPath);
        }

        if (_baseVfxPrefab == null && _verboseLogs)
        {
            Debug.LogWarning($"[VFXManager] No encontro prefab base. Crea: Assets/Resources/{_resourcesPath}.prefab");
        }
    }

    public void Play(VFXKind kind, Vector3 worldPosition)
    {
        Spawn(kind, worldPosition, null);
    }

    public void PlayOnCamera(VFXKind kind)
    {
        Camera cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (cam == null)
        {
            if (_verboseLogs) Debug.LogWarning("[VFXManager] No hay cámara para PlayOnCamera.");
            return;
        }

        Vector3 pos = cam.transform.position + cam.transform.forward * Mathf.Max(0.5f, _defaultZOffsetFromCamera);
        Spawn(kind, pos, null);
    }

    private void Spawn(VFXKind kind, Vector3 worldPosition, Transform parent)
    {
        TryLoadPrefabIfNeeded();

        if (_baseVfxPrefab == null)
        {
            // No spamear la consola cada frame
            if (_verboseLogs) Debug.LogWarning("[VFXManager] Falta el prefab base (Resources/VFX/VFX_Base).");
            return;
        }

        VFXEntry entry = null;
        _map.TryGetValue(kind, out entry);

        GameObject go = Instantiate(_baseVfxPrefab, worldPosition, Quaternion.identity, parent);
        go.name = $"VFX_{kind}";

        float s = Mathf.Max(0.0001f, _globalScale) * (entry != null ? Mathf.Max(0.0001f, entry.scaleMultiplier) : 1f);
        go.transform.localScale = go.transform.localScale * s;

        ParticleSystem ps = go.GetComponentInChildren<ParticleSystem>(true);
        if (ps == null)
        {
            if (_verboseLogs) Debug.LogWarning($"[VFXManager] El prefab base no tiene ParticleSystem: {_baseVfxPrefab.name}");
            Destroy(go);
            return;
        }

        // Aseguramos estado limpio antes de modificar/emitir
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Desactivamos emisión “normal” para que SOLO salga el burst que pedimos.
        var emission = ps.emission;
        emission.enabled = false;

        // Emit burst con color/tamaño por evento
        int count = entry != null ? Mathf.Max(1, entry.emitCount) : 25;

        EmitParams ep = new EmitParams();
        if (entry != null)
        {
            ep.startColor = entry.tint;

            float baseSize = GetCurveMax(main.startSize);
            ep.startSize = Mathf.Max(0.0001f, baseSize) * Mathf.Max(0.01f, entry.sizeMultiplier);
        }

        ps.Play(true);
        ps.Emit(ep, count);

        float destroyAfter = (entry != null && entry.destroyAfterSeconds > 0f)
            ? entry.destroyAfterSeconds
            : EstimateLifetime(ps);

        Destroy(go, destroyAfter);

        if (_verboseLogs)
            Debug.Log($"[VFXManager] Spawn {kind} @ {worldPosition} (Emit {count}, destroy {destroyAfter:0.00}s)");
    }

    private static float EstimateLifetime(ParticleSystem ps)
    {
        try
        {
            var main = ps.main;
            float lifeMax = GetCurveMax(main.startLifetime);
            // + margen
            return Mathf.Clamp(lifeMax + 0.5f, 0.75f, 10f);
        }
        catch
        {
            return 2f;
        }
    }

    private static float GetCurveMax(ParticleSystem.MinMaxCurve c)
    {
        switch (c.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return c.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return c.constantMax;
            case ParticleSystemCurveMode.Curve:
                return c.curveMultiplier; // aprox
            case ParticleSystemCurveMode.TwoCurves:
                return c.curveMultiplier; // aprox
            default:
                return 1f;
        }
    }
}