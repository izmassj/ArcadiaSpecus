using UnityEngine;

public class AlertManager : MonoBehaviour
{
    public static AlertManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        ResourceManager.OnResourceCritical += OnResourceCritical;
    }

    private void OnDisable()
    {
        ResourceManager.OnResourceCritical -= OnResourceCritical;
    }

    private void OnResourceCritical(ResourceType _resourceType)
    {
        Debug.Log($"ALERTA: {_resourceType} en nivel crítico");
        // SFX de alerta (rúbrica - feedback audiovisual).
        if (PersistentMusic.instance != null)
            PersistentMusic.instance.PlayAlert();


        VFXManager.EnsureInstance();
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayOnCamera(VFXKind.ResourceCritical);
        }
    }
}
