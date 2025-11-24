// AlertManager.cs
using UnityEngine;

public class AlertManager : MonoBehaviour
{
    public static AlertManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        // Suscribirse al evento de recursos críticos
        ResourceManager.OnResourceCritical += OnResourceCritical;
    }

    /// <summary>
    /// Maneja el evento cuando un recurso alcanza nivel crítico
    /// </summary>
    /// <param name="resourceType">Tipo de recurso en estado crítico</param>
    void OnResourceCritical(ResourceType resourceType)
    {
        Debug.Log($"ALERTA: {resourceType} está en nivel crítico!");
    }
}