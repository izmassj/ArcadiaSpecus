using UnityEngine;

/// <summary>
/// Metadata muy simple para habitaciones colocadas.
/// Se añade automáticamente al colocar (sin afectar al sistema de colocación).
/// </summary>
public class PlacedRoom : MonoBehaviour
{
    [Header("Tipo de habitación")]
    public RoomKind roomKind = RoomKind.MIDDLE;

    [Header("Combo (Bé)")]
    public int comboId = 0;

    public void ClearCombo()
    {
        comboId = 0;
    }

    public void SetCombo(int id)
    {
        comboId = id;
    }
}
