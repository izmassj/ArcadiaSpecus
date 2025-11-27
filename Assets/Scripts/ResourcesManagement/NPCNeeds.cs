using UnityEngine;

[System.Serializable]
public class NPCNeeds
{
    [Header("Niveles Actuales")]
    [Range(0, 100)] public float hunger = 0f;
    [Range(0, 100)] public float thirst = 0f;
    [Range(0, 100)] public float fatigue = 0f;

    [Header("Umbrales Críticos")]
    public float criticalHunger = 70f;
    public float criticalThirst = 60f;
    public float criticalFatigue = 50f;

    [Header("Ratios de Degradación")]
    public float hungerRate = 2.0f;
    public float thirstRate = 2.5f;
    public float fatigueRate = 1.8f;

    [Header("Ratios de Recuperación")]
    public float eatingRecovery = 50f;
    public float drinkingRecovery = 60f;
    public float restingRecovery = 40f;

    public System.Action<DwellerNPC> OnDeath;
    private DwellerNPC currentOwner;

    public void SetOwner(DwellerNPC owner)
    {
        currentOwner = owner;
    }

    public bool IsCritical()
    {
        return hunger >= criticalHunger ||
               thirst >= criticalThirst ||
               fatigue >= criticalFatigue;
    }

    public bool IsDead()
    {
        return hunger >= 100f || thirst >= 100f || fatigue >= 100f;
    }

    public ResourceType GetMostCriticalNeed()
    {
        if (thirst >= criticalThirst) return ResourceType.Water;
        if (fatigue >= criticalFatigue) return ResourceType.Energy;
        if (hunger >= criticalHunger) return ResourceType.Food;
        return ResourceType.Materials;
    }

    public void UpdateNeeds(float deltaTime, DwellerNPC owner = null)
    {
        if (IsDead()) return;

        float oldHunger = hunger;
        float oldThirst = thirst;
        float oldFatigue = fatigue;

        hunger = Mathf.Clamp(hunger + (hungerRate * deltaTime), 0, 100);
        thirst = Mathf.Clamp(thirst + (thirstRate * deltaTime), 0, 100);
        fatigue = Mathf.Clamp(fatigue + (fatigueRate * deltaTime), 0, 100);

        if (owner != null && Time.frameCount % 300 == 0)
        {
            Debug.Log($"{owner.dwellerName} - H:{(int)hunger} S:{(int)thirst} F:{(int)fatigue}");
        }

        if (owner != null)
        {
            bool justDied = (oldHunger < 100f && hunger >= 100f) ||
                           (oldThirst < 100f && thirst >= 100f) ||
                           (oldFatigue < 100f && fatigue >= 100f);

            if (justDied)
            {
                string cause = "";
                if (hunger >= 100f) cause = "Hambre";
                else if (thirst >= 100f) cause = "Sed";
                else if (fatigue >= 100f) cause = "Fatiga";

                Debug.LogWarning($"{owner.dwellerName} HA MUERTO por {cause}! H:{(int)hunger} S:{(int)thirst} F:{(int)fatigue}");
                OnDeath?.Invoke(owner);
            }
        }
    }

    public void Eat(float deltaTime)
    {
        hunger = Mathf.Clamp(hunger - (eatingRecovery * deltaTime), 0, 100);
    }

    public void Drink(float deltaTime)
    {
        thirst = Mathf.Clamp(thirst - (drinkingRecovery * deltaTime), 0, 100);
    }

    public void Rest(float deltaTime)
    {
        fatigue = Mathf.Clamp(fatigue - (restingRecovery * deltaTime), 0, 100);
    }

    public bool IsFullyRested() => fatigue <= 5f;
    public bool IsFull() => hunger <= 10f;
    public bool IsHydrated() => thirst <= 10f;

    public string GetNeedsStatus()
    {
        string status = $"H:{(int)hunger} S:{(int)thirst} F:{(int)fatigue}";

        if (IsDead())
        {
            status += " [MUERTO]";
        }
        else if (IsCritical())
        {
            status += " [CRÍTICO]";
        }

        return status;
    }

    public void ResetNeeds()
    {
        hunger = 0f;
        thirst = 0f;
        fatigue = 0f;
    }

    public void KillInstantly()
    {
        hunger = 100f;
        thirst = 100f;
        fatigue = 100f;

        if (currentOwner != null)
        {
            Debug.LogWarning($"{currentOwner.dwellerName} MUERTO INSTANTÁNEAMENTE");
            OnDeath?.Invoke(currentOwner);
        }
    }

    public void AccelerateNeedsForTesting()
    {
        // Aumentar ratios
        hungerRate = 25f;
        thirstRate = 30f;
        fatigueRate = 20f;

        // FORZAR todas las necesidades al máximo inmediatamente
        hunger = 100f;
        thirst = 100f;
        fatigue = 100f;

        if (currentOwner != null)
        {
            Debug.Log($"{currentOwner.dwellerName} - NECESIDADES FORZADAS AL MÁXIMO: H:{hunger} S:{thirst} F:{fatigue}");

            // Disparar muerte inmediatamente
            OnDeath?.Invoke(currentOwner);
        }
    }

    // NUEVO: Método para acelerar sin matar inmediatamente
    public void AccelerateNeedsWithoutKilling()
    {
        hungerRate = 10f;
        thirstRate = 30f;
        fatigueRate = 20f;

        //hunger = Mathf.Min(hunger + 80f, 100f);
        //thirst = Mathf.Min(thirst + 90f, 100f);
        //fatigue = Mathf.Min(fatigue + 85f, 100f);

        if (currentOwner != null)
        {
            Debug.Log($"{currentOwner.dwellerName} - NECESIDADES ACELERADAS: H:{hunger} S:{thirst} F:{fatigue}");
        }
    }
}