using UnityEngine;

/// <summary>
/// Singleton persistente genérico para objetos globales de la escena principal.
/// Mantiene la API original (instance) para no romper referencias existentes.
/// </summary>
public class PersistentManager : MonoBehaviour
{
    public static PersistentManager instance;

    [SerializeField] private bool _dontDestroyOnLoad = true;
    [SerializeField] private bool _destroyDuplicates = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (_destroyDuplicates)
            {
                Destroy(gameObject);
            }
            return;
        }

        instance = this;

        if (_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
