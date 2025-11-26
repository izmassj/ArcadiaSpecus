// NPCNeeds.cs
using UnityEngine;

[System.Serializable]
public class NPCNeeds
{
    [Header("Niveles Actuales")]
    [Range(0, 100)] public float hunger = 0f;
    [Range(0, 100)] public float thirst = 0f;
    [Range(0, 100)] public float fatigue = 0f;

    [Header("Umbrales Críticos")]
    public float criticalHunger = 70f;      // REDUCIDO
    public float criticalThirst = 60f;      // REDUCIDO significativamente
    public float criticalFatigue = 50f;     // REDUCIDO para que descansen antes

    [Header("Ratios de Degradación")]
    public float hungerRate = 1.0f;         // REDUCIDO
    public float thirstRate = 1.5f;         // REDUCIDO  
    public float fatigueRate = 1.8f;        // AUMENTADO para priorizar descanso

    [Header("Ratios de Recuperación")]
    public float eatingRecovery = 50f;      // AUMENTADO significativamente
    public float drinkingRecovery = 60f;    // AUMENTADO significativamente
    public float restingRecovery = 40f;     // AUMENTADO significativamente

    /// <summary>
    /// Verifica si alguna necesidad está en nivel crítico
    /// </summary>
    public bool IsCritical()
    {
        return hunger >= criticalHunger ||
               thirst >= criticalThirst ||
               fatigue >= criticalFatigue;
    }

    /// <summary>
    /// Obtiene la necesidad más crítica del NPC
    /// </summary>
    public ResourceType GetMostCriticalNeed()
    {
        if (thirst >= criticalThirst) return ResourceType.Water;
        if (fatigue >= criticalFatigue) return ResourceType.Energy;
        if (hunger >= criticalHunger) return ResourceType.Food;
        return ResourceType.Materials; // Default
    }

    /// <summary>
    /// Actualiza las necesidades del NPC con el tiempo
    /// </summary>
    public void UpdateNeeds(float deltaTime)
    {
        hunger = Mathf.Clamp(hunger + (hungerRate * deltaTime), 0, 100);
        thirst = Mathf.Clamp(thirst + (thirstRate * deltaTime), 0, 100);
        fatigue = Mathf.Clamp(fatigue + (fatigueRate * deltaTime), 0, 100);
    }

    /// <summary>
    /// Aplica recuperación por comer
    /// </summary>
    public void Eat(float deltaTime)
    {
        hunger = Mathf.Clamp(hunger - (eatingRecovery * deltaTime), 0, 100);
    }

    /// <summary>
    /// Aplica recuperación por beber
    /// </summary>
    public void Drink(float deltaTime)
    {
        thirst = Mathf.Clamp(thirst - (drinkingRecovery * deltaTime), 0, 100);
    }

    /// <summary>
    /// Aplica recuperación por descansar
    /// </summary>
    public void Rest(float deltaTime)
    {
        fatigue = Mathf.Clamp(fatigue - (restingRecovery * deltaTime), 0, 100);
    }

    // Métodos de verificación de estado
    public bool IsFullyRested() => fatigue <= 5f;
    public bool IsFull() => hunger <= 10f;
    public bool IsHydrated() => thirst <= 10f;

    /// <summary>
    /// Obtiene el estado actual de las necesidades como string
    /// </summary>
    public string GetNeedsStatus()
    {
        return $"H:{(int)hunger} S:{(int)thirst} F:{(int)fatigue}";
    }
}