using UnityEngine;

[System.Serializable]
public class NPCNeeds
{
    [Header("Niveles Actuales")]
    [Range(0f, 100f)] public float hunger = 0f;
    [Range(0f, 100f)] public float thirst = 0f;
    [Range(0f, 100f)] public float fatigue = 0f;

    [Header("Umbrales Críticos")]
    public float criticalHunger = 70f;
    public float criticalThirst = 60f;
    public float criticalFatigue = 50f;

    [Header("Ratios de Degradación")]
    public float hungerRate = 2f;
    public float thirstRate = 2.5f;
    public float fatigueRate = 1.8f;

    [Header("Ratios de Recuperación")]
    public float eatingRecovery = 20f;
    public float drinkingRecovery = 25f;
    public float restingRecovery = 18f;

    public System.Action<DwellerNPC> OnDeath;

    private DwellerNPC _currentOwner;
    private bool _deathNotified;

    public void SetOwner(DwellerNPC _owner)
    {
        _currentOwner = _owner;
        _deathNotified = false;
    }

    public void UpdateNeeds(float _deltaTime, DwellerNPC _owner = null)
    {
        if (IsDead())
        {
            TryNotifyDeath(_owner != null ? _owner : _currentOwner);
            return;
        }

        float mult = 1f;
        if (ResourceManager.Instance != null)
        {
            // Dinámica Bé: si falta comida/agua/oxígeno, el deterioro se acelera y el rendimiento baja.
            mult = ResourceManager.Instance.GetNeedsDegradationMultiplier();
        }

        hunger = Mathf.Clamp(hunger + hungerRate * mult * _deltaTime, 0f, 100f);
        thirst = Mathf.Clamp(thirst + thirstRate * mult * _deltaTime, 0f, 100f);
        fatigue = Mathf.Clamp(fatigue + fatigueRate * mult * _deltaTime, 0f, 100f);

        if (IsDead())
        {
            TryNotifyDeath(_owner != null ? _owner : _currentOwner);
        }
    }

    public bool IsCritical()
    {
        return hunger >= criticalHunger || thirst >= criticalThirst || fatigue >= criticalFatigue;
    }

    public bool IsDead()
    {
        return hunger >= 100f || thirst >= 100f || fatigue >= 100f;
    }

    public ResourceType GetMostCriticalNeed()
    {
        float _hungerPriority = hunger - criticalHunger;
        float _thirstPriority = thirst - criticalThirst;
        float _fatiguePriority = fatigue - criticalFatigue;

        if (_thirstPriority >= _hungerPriority && _thirstPriority >= _fatiguePriority && thirst >= criticalThirst)
        {
            return ResourceType.Water;
        }

        if (_fatiguePriority >= _hungerPriority && _fatiguePriority >= _thirstPriority && fatigue >= criticalFatigue)
        {
            return ResourceType.Energy;
        }

        if (hunger >= criticalHunger)
        {
            return ResourceType.Food;
        }

        return ResourceType.Materials;
    }

    public void Eat(float _deltaTime)
    {
        hunger = Mathf.Clamp(hunger - eatingRecovery * _deltaTime, 0f, 100f);
    }

    public void Drink(float _deltaTime)
    {
        thirst = Mathf.Clamp(thirst - drinkingRecovery * _deltaTime, 0f, 100f);
    }

    public void Rest(float _deltaTime)
    {
        fatigue = Mathf.Clamp(fatigue - restingRecovery * _deltaTime, 0f, 100f);
    }

    public bool IsFullyRested()
    {
        return fatigue <= 5f;
    }

    public bool IsFull()
    {
        return hunger <= 10f;
    }

    public bool IsHydrated()
    {
        return thirst <= 10f;
    }

    public string GetNeedsStatus()
    {
        string _status = $"H:{(int)hunger} S:{(int)thirst} F:{(int)fatigue}";

        if (IsDead())
        {
            _status += " [MUERTO]";
        }
        else if (IsCritical())
        {
            _status += " [CRÍTICO]";
        }

        return _status;
    }

    public void ResetNeeds()
    {
        hunger = 0f;
        thirst = 0f;
        fatigue = 0f;
        _deathNotified = false;
    }

    public void KillInstantly()
    {
        hunger = 100f;
        thirst = 100f;
        fatigue = 100f;
        TryNotifyDeath(_currentOwner);
    }

    public void AccelerateNeedsForTesting()
    {
        hungerRate = 25f;
        thirstRate = 30f;
        fatigueRate = 20f;
        KillInstantly();
    }

    public void AccelerateNeedsWithoutKilling()
    {
        hungerRate = 10f;
        thirstRate = 12f;
        fatigueRate = 8f;

        hunger = Mathf.Clamp(hunger + 40f, 0f, 95f);
        thirst = Mathf.Clamp(thirst + 45f, 0f, 95f);
        fatigue = Mathf.Clamp(fatigue + 35f, 0f, 95f);
    }

    private void TryNotifyDeath(DwellerNPC _owner)
    {
        if (_deathNotified)
        {
            return;
        }

        _deathNotified = true;
        if (_owner != null)
        {
            OnDeath?.Invoke(_owner);
        }
    }
}
