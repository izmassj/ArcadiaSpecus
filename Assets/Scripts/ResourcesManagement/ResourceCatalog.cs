// ResourceCatalog.cs
using System.Collections.Generic;

/// <summary>
/// Catálogo estático para agrupar ResourceType por categorías.
/// Se usa para lógica, debug y UI opcional.
/// </summary>
public static class ResourceCatalog
{
    public static readonly IReadOnlyDictionary<ResourceType, ResourceCategory> CategoryByType =
        new Dictionary<ResourceType, ResourceCategory>
        {
            { ResourceType.Food, ResourceCategory.Sustenance },
            { ResourceType.Water, ResourceCategory.Sustenance },
            { ResourceType.Rations, ResourceCategory.Sustenance },

            { ResourceType.Energy, ResourceCategory.Power },
            { ResourceType.Oxygen, ResourceCategory.Power },
            { ResourceType.Fuel, ResourceCategory.Power },

            { ResourceType.Materials, ResourceCategory.Construction },
            { ResourceType.Metal, ResourceCategory.Construction },
            { ResourceType.Electronics, ResourceCategory.Construction },

            // Medicine lo dejamos fuera de categorías de rúbrica (extra).
            { ResourceType.Medicine, ResourceCategory.Sustenance },
        };

    public static readonly ResourceType[] SustenanceTypes = { ResourceType.Food, ResourceType.Water, ResourceType.Rations };
    public static readonly ResourceType[] PowerTypes = { ResourceType.Energy, ResourceType.Oxygen, ResourceType.Fuel };
    public static readonly ResourceType[] ConstructionTypes = { ResourceType.Materials, ResourceType.Metal, ResourceType.Electronics };

    public static ResourceType[] GetTypes(ResourceCategory category)
    {
        switch (category)
        {
            case ResourceCategory.Sustenance: return SustenanceTypes;
            case ResourceCategory.Power: return PowerTypes;
            case ResourceCategory.Construction: return ConstructionTypes;
            default: return SustenanceTypes;
        }
    }
}
