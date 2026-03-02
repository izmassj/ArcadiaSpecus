using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RoomComboManager
///
/// Detecta combos "LEFT + MIDDLE(s) + RIGHT" en una misma fila (por proximidad de bounds)
/// y aplica un multiplicador global de producción a ResourceManager.
///
/// Importante:
/// - NO modifica la colocación de habitaciones.
/// - Solo analiza lo que ya está colocado.
/// - Si no encuentra nada, deja el multiplicador en 1.0.
/// </summary>
public class RoomComboManager : MonoBehaviour
{
    public static RoomComboManager Instance { get; private set; }

    [Header("Detección de adyacencia")]
    [Tooltip("Tolerancia para considerar que dos habitaciones están en la misma fila (en unidades mundo).")]
    [SerializeField] private float _rowTolerance = 1.5f;

    [Tooltip("Tolerancia para considerar que dos habitaciones están tocándose por el lado (en unidades mundo).")]
    [SerializeField] private float _edgeGapTolerance = 1.0f;

    [Header("Bonus (Bé)")]
    [SerializeField] private float _bonusPerCombo = 0.05f; // +5%
    [SerializeField] private float _bonusMax = 0.50f;      // +50%

    [Header("Room Mask (para bootstrap)")]
    [SerializeField] private LayerMask _roomLayerMask;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static RoomComboManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        RoomComboManager found = FindObjectOfType<RoomComboManager>();
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        GameObject go = new GameObject("[RoomComboManager]");
        return go.AddComponent<RoomComboManager>();
    }

    public void SetRoomLayerMask(LayerMask mask)
    {
        _roomLayerMask = mask;
    }

    /// <summary>
    /// Añade PlacedRoom a colliders del bunker que estén en el roomLayerMask, si no lo tienen.
    /// No mueve nada; solo añade metadata.
    /// </summary>
    public void BootstrapExistingRooms()
    {
        if (_roomLayerMask.value == 0)
            return;

        BoxCollider[] all = FindObjectsOfType<BoxCollider>(true);

        for (int i = 0; i < all.Length; i++)
        {
            BoxCollider col = all[i];
            if (col == null)
                continue;

            if (!IsInLayerMask(col.gameObject.layer, _roomLayerMask))
                continue;

            PlacedRoom pr = col.GetComponent<PlacedRoom>();
            if (pr == null)
                pr = col.gameObject.AddComponent<PlacedRoom>();

            // Si el tipo está por defecto, intentamos inferirlo por nombre (no es perfecto, pero suficiente).
            if (pr.roomKind == RoomKind.MIDDLE)
            {
                RoomKind inferred = InferKindFromHierarchy(col.transform);
                pr.roomKind = inferred;
            }
        }
    }

    public void RecalculateCombos()
    {
        PlacedRoom[] rooms = FindObjectsOfType<PlacedRoom>(true);
        if (rooms == null || rooms.Length == 0)
        {
            ApplyMultiplier(1f);
            return;
        }

        // Limpiar combos
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null)
                rooms[i].ClearCombo();
        }

        // Preparar datos
        List<RoomNode> nodes = new List<RoomNode>();
        for (int i = 0; i < rooms.Length; i++)
        {
            PlacedRoom pr = rooms[i];
            if (pr == null)
                continue;

            BoxCollider col = pr.GetComponent<BoxCollider>();
            if (col == null)
                continue;

            nodes.Add(new RoomNode
            {
                placed = pr,
                col = col,
                bounds = col.bounds
            });
        }

        if (nodes.Count == 0)
        {
            ApplyMultiplier(1f);
            return;
        }

        // Construir "right neighbor" por proximidad (misma fila)
        Dictionary<PlacedRoom, PlacedRoom> rightOf = new Dictionary<PlacedRoom, PlacedRoom>();

        for (int i = 0; i < nodes.Count; i++)
        {
            RoomNode a = nodes[i];

            PlacedRoom best = null;
            float bestGap = float.PositiveInfinity;

            for (int j = 0; j < nodes.Count; j++)
            {
                if (i == j) continue;

                RoomNode b = nodes[j];

                // misma fila
                if (Mathf.Abs(a.bounds.center.y - b.bounds.center.y) > _rowTolerance)
                    continue;

                // b a la derecha
                if (b.bounds.center.x <= a.bounds.center.x)
                    continue;

                // gap entre bordes
                float gap = b.bounds.min.x - a.bounds.max.x;

                // si b se solapa en x, gap negativo; lo tratamos como 0 (ya "toca")
                float absGap = Mathf.Abs(gap);

                if (absGap > _edgeGapTolerance)
                    continue;

                // asegurar que hay overlap razonable en Y (evita diagonales raras)
                float yOverlap = Mathf.Min(a.bounds.max.y, b.bounds.max.y) - Mathf.Max(a.bounds.min.y, b.bounds.min.y);
                if (yOverlap <= 0f)
                    continue;

                if (absGap < bestGap)
                {
                    bestGap = absGap;
                    best = b.placed;
                }
            }

            if (best != null)
                rightOf[a.placed] = best;
        }

        // Detectar combos: LEFT -> MIDDLE(s) -> RIGHT
        int comboId = 0;
        int comboCount = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            PlacedRoom start = nodes[i].placed;
            if (start == null || start.roomKind != RoomKind.LEFT)
                continue;

            List<PlacedRoom> chain = new List<PlacedRoom>();
            chain.Add(start);

            PlacedRoom cur = start;
            int safety = 0;

            while (safety < 64 && rightOf.TryGetValue(cur, out PlacedRoom next) && next != null)
            {
                chain.Add(next);

                if (next.roomKind == RoomKind.MIDDLE)
                {
                    cur = next;
                    safety++;
                    continue;
                }

                if (next.roomKind == RoomKind.RIGHT)
                {
                    // mínimo LEFT + MIDDLE + RIGHT => 3
                    if (chain.Count >= 3)
                    {
                        comboId++;
                        comboCount++;

                        for (int k = 0; k < chain.Count; k++)
                            chain[k].SetCombo(comboId);
                    }
                    break;
                }

                // otro tipo rompe la cadena
                break;
            }
        }

        float bonus = Mathf.Clamp(comboCount * _bonusPerCombo, 0f, _bonusMax);
        ApplyMultiplier(1f + bonus);

        // Comentario UI: aquí podríais mostrar un aviso tipo "+X% producción por combo".
    }

    private void ApplyMultiplier(float multiplier)
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.SetGlobalProductionMultiplier(multiplier);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private RoomKind InferKindFromHierarchy(Transform t)
    {
        if (t == null)
            return RoomKind.MIDDLE;

        string n = "";
        Transform cur = t;
        int safety = 0;

        while (cur != null && safety < 4)
        {
            n += " " + cur.name;
            cur = cur.parent;
            safety++;
        }

        n = n.ToUpperInvariant();

        if (n.Contains("INTER")) return RoomKind.INTERSECTION;
        if (n.Contains("LEFT")) return RoomKind.LEFT;
        if (n.Contains("RIGHT")) return RoomKind.RIGHT;
        if (n.Contains("DOOR")) return RoomKind.DOORWALL;

        return RoomKind.MIDDLE;
    }

    private struct RoomNode
    {
        public PlacedRoom placed;
        public BoxCollider col;
        public Bounds bounds;
    }
}
