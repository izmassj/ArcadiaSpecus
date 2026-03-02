using System;
using UnityEngine;

/// <summary>
/// Reglas deterministas para decidir qué recursos aparecen en cada zona exterior.
///
/// Está pensado para funcionar sin editar escenas: se determina el tipo de zona por nombre
/// de escena (MiniGameDesignX) y la recompensa por nodo usando un hash estable del nodeId.
/// Así un mismo nodo siempre representa el mismo tipo de recurso.
/// </summary>
public static class ExteriorZoneRules
{
    public static bool IsExteriorScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        // En este proyecto, los "exteriores" son los minijuegos de exploración.
        return sceneName.StartsWith("MiniGame", StringComparison.OrdinalIgnoreCase);
    }

    public static ExteriorZoneKind GetZoneKind(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return ExteriorZoneKind.Unknown;

        // Mapping simple y estable.
        if (sceneName.IndexOf("Design1", StringComparison.OrdinalIgnoreCase) >= 0)
            return ExteriorZoneKind.AbandonedStores;
        if (sceneName.IndexOf("Design2", StringComparison.OrdinalIgnoreCase) >= 0)
            return ExteriorZoneKind.IndustrialRuins;
        if (sceneName.IndexOf("Design3", StringComparison.OrdinalIgnoreCase) >= 0)
            return ExteriorZoneKind.MedicalWing;

        return ExteriorZoneKind.Unknown;
    }

    public static string GetZoneDisplayName(ExteriorZoneKind kind)
    {
        switch (kind)
        {
            case ExteriorZoneKind.AbandonedStores: return "Tiendas abandonadas";
            case ExteriorZoneKind.IndustrialRuins: return "Ruinas industriales";
            case ExteriorZoneKind.MedicalWing: return "Ala médica";
            default: return "Exterior";
        }
    }

    /// <summary>
    /// Decide el recurso base de un nodo en función de la zona y un hash estable.
    /// Mantiene consistencia: dentro de una misma zona prioriza ciertos recursos.
    /// </summary>
    public static ResourceType PickResourceType(ExteriorZoneKind kind, int stableHash)
    {
        int h = Mathf.Abs(stableHash);
        int roll = h % 100;

        switch (kind)
        {
            case ExteriorZoneKind.AbandonedStores:
                // comida/agua/materiales
                if (roll < 45) return ResourceType.Food;
                if (roll < 85) return ResourceType.Water;
                return ResourceType.Materials;

            case ExteriorZoneKind.IndustrialRuins:
                // materiales/energía (baterías/cables)
                if (roll < 65) return ResourceType.Materials;
                return ResourceType.Energy;

            case ExteriorZoneKind.MedicalWing:
                // medicina/agua (botiquines/sueros)
                if (roll < 60) return ResourceType.Medicine;
                return ResourceType.Water;

            default:
                // fallback: mezcla simple
                if (roll < 25) return ResourceType.Food;
                if (roll < 50) return ResourceType.Water;
                if (roll < 75) return ResourceType.Energy;
                return ResourceType.Materials;
        }
    }

    public static int PickBaseAmount(ExteriorZoneKind kind, ResourceType type, int stableHash)
    {
        int h = Mathf.Abs(stableHash);
        int roll = (h / 101) % 100;

        // Rangos pequeños para no romper el balance.
        switch (type)
        {
            case ResourceType.Materials: return 2 + (roll % 3); // 2..4
            case ResourceType.Energy: return 1 + (roll % 2); // 1..2
            case ResourceType.Food: return 1 + (roll % 3); // 1..3
            case ResourceType.Water: return 1 + (roll % 3); // 1..3
            case ResourceType.Medicine: return 1; // escaso
            default: return 1;
        }
    }

    public static int PickMaxHarvests(ExteriorZoneKind kind, int stableHash)
    {
        int h = Mathf.Abs(stableHash);
        int roll = (h / 313) % 100;

        // Cuántas veces puede dar loot antes de quedarse seco.
        switch (kind)
        {
            case ExteriorZoneKind.MedicalWing: return 1 + (roll % 2); // 1..2
            case ExteriorZoneKind.IndustrialRuins: return 2 + (roll % 2); // 2..3
            case ExteriorZoneKind.AbandonedStores: return 2 + (roll % 3); // 2..4
            default: return 2;
        }
    }

    public static int GetRespawnMinutes(ExteriorZoneKind kind)
    {
        // Respawn pensado para re-entradas al exterior (no para "reaparecer" instantáneo).
        switch (kind)
        {
            case ExteriorZoneKind.MedicalWing: return 30;
            case ExteriorZoneKind.IndustrialRuins: return 20;
            case ExteriorZoneKind.AbandonedStores: return 15;
            default: return 20;
        }
    }

    public static int GetRaidEveryHours(ExteriorZoneKind kind)
    {
        // Cada X horas ausente, se puede aplicar un "saqueo".
        switch (kind)
        {
            case ExteriorZoneKind.MedicalWing: return 2;
            case ExteriorZoneKind.IndustrialRuins: return 3;
            case ExteriorZoneKind.AbandonedStores: return 4;
            default: return 4;
        }
    }
}
