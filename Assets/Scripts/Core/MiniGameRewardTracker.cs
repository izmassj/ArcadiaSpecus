using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda un snapshot de recursos al entrar en un minijuego y permite calcular
/// lo que se ha ganado (delta positivo) al volver a la base.
///
/// No depende de UI. El texto lo muestra RewardToastManager.
/// </summary>
public class MiniGameRewardTracker : MonoBehaviour
{
    public static MiniGameRewardTracker Instance { get; private set; }

    private bool _inSession;
    private string _sessionScene;
    private readonly Dictionary<ResourceType, int> _startSnapshot = new Dictionary<ResourceType, int>();

    public static MiniGameRewardTracker EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        MiniGameRewardTracker found = FindObjectOfType<MiniGameRewardTracker>(true);
        if (found != null)
        {
            Instance = found;
            DontDestroyOnLoad(found.gameObject);
            return found;
        }

        GameObject go = new GameObject("[MiniGameRewardTracker]");
        MiniGameRewardTracker created = go.AddComponent<MiniGameRewardTracker>();
        DontDestroyOnLoad(go);
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
    }

    public void BeginSession(string miniGameSceneName)
    {
        _inSession = true;
        _sessionScene = miniGameSceneName;
        _startSnapshot.Clear();

        // Snapshot de recursos (si no existe ResourceManager, deja todo en 0).
        ResourceManager rm = ResourceManager.Instance;

        Array values = Enum.GetValues(typeof(ResourceType));
        for (int i = 0; i < values.Length; i++)
        {
            ResourceType type = (ResourceType)values.GetValue(i);
            _startSnapshot[type] = rm != null ? rm.GetResourceAmount(type) : 0;
        }
    }

    /// <summary>
    /// Finaliza la sesión y devuelve un diccionario con deltas positivos (ganancias).
    /// Si win == false, devuelve vacío y limpia sesión.
    /// </summary>
    public Dictionary<ResourceType, int> EndSession(bool win)
    {
        Dictionary<ResourceType, int> gains = new Dictionary<ResourceType, int>();

        if (_inSession && win)
        {
            ResourceManager rm = ResourceManager.Instance;
            if (rm != null)
            {
                foreach (KeyValuePair<ResourceType, int> pair in _startSnapshot)
                {
                    int current = rm.GetResourceAmount(pair.Key);
                    int delta = current - pair.Value;
                    if (delta > 0)
                        gains[pair.Key] = delta;
                }
            }
        }

        _inSession = false;
        _sessionScene = null;
        _startSnapshot.Clear();
        return gains;
    }
}
