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
        // Comentario audio/UI: aquí se podría disparar popup/sonido de alerta.
    }
}
