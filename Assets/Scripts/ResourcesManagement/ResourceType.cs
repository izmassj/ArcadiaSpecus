// ResourceType.cs
using UnityEngine;

/// <summary>
/// Tipos de recursos disponibles en el juego
/// IMPORTANTE: Añadimos nuevos valores AL FINAL para no romper referencias serializadas.
/// </summary>
public enum ResourceType
{
    Food,
    Water,
    Energy,
    Materials,
    Oxygen,
    Medicine,


    Rations,
    Fuel,
    Metal,
    Electronics
}
