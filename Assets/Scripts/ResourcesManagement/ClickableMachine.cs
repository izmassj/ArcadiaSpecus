// ClickableMachine.cs
using UnityEngine;

public class ClickableMachine : MonoBehaviour
{
    [Header("Resource Settings")]
    public ResourceType resourceType;
    public int amountPerCycle = 5;

    [Header("Cooldown Settings")]
    public float cycleDuration = 3f;

    private float cycleTimer = 0f;
    private int accumulatedAmount = 0;

    [Header("Visual Feedback")]
    public Renderer machineRenderer;
    private Color originalColor;

    [Header("Audio")]
    public new AudioSource audio;

    void Start()
    {
        if (machineRenderer == null)
            machineRenderer = GetComponentInChildren<Renderer>();

        if (machineRenderer != null)
            originalColor = machineRenderer.material.color;

        cycleTimer = cycleDuration;

        if(audio == null)
            audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        cycleTimer -= Time.deltaTime;

        if (cycleTimer <= 0f)
        {
            accumulatedAmount += amountPerCycle;
            cycleTimer = cycleDuration;

            DarkenColor();   // Indicar que hay recursos acumulados
        }
    }

    /// <summary>
    /// Maneja el clic del mouse para recolectar recursos
    /// </summary>
    public void OnMouseDown()
    {
        if (accumulatedAmount <= 0)
            return;

        ResourceManager.Instance.AddResource(resourceType, accumulatedAmount);

        accumulatedAmount = 0;
        RestoreColor();

        audio.Play();

        Debug.Log("Clickado");
        
    }

    /// <summary>
    /// Oscurece el color para indicar recursos disponibles
    /// </summary>
    void DarkenColor()
    {
        if (machineRenderer == null) return;

        Color c = originalColor * 0.7f;
        machineRenderer.material.color = c;
    }

    /// <summary>
    /// Restaura el color original cuando se recolectan los recursos
    /// </summary>
    void RestoreColor()
    {
        if (machineRenderer == null) return;

        machineRenderer.material.color = originalColor;
    }

    
}