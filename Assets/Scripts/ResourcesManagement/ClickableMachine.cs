// ClickableMachine.cs
using System;
using System.Reflection;
using UnityEngine;

public class ClickableMachine : MonoBehaviour
{
    [Header("Resource Settings")]
    public ResourceType resourceType;
    public int amountPerCycle = 5;

    [Header("Cooldown Settings")]
    public float cycleDuration = 3f;

    [Header("Power")]
    [Tooltip("Si es true, esta máquina no produce cuando no hay energía (rúbrica Bé).")]
    public bool requiresPower = true;

    private float cycleTimer = 0f;
    private int accumulatedAmount = 0;

    [Header("Visual Feedback")]
    public Renderer machineRenderer;
    private Color originalColor;

    void Start()
    {
        if (machineRenderer == null)
            machineRenderer = GetComponentInChildren<Renderer>();

        if (machineRenderer != null)
            originalColor = machineRenderer.material.color;

        cycleTimer = cycleDuration;
    }

    void Update()
    {
        // Apagón: no acumula (si requiere energía)
        if (requiresPower && ResourceManager.Instance != null &&
            ResourceManager.Instance.ShouldPowerOutageDisableStations() &&
            !ResourceManager.Instance.IsPowerOnline())
        {
            // Visualmente dejamos estado "normal"
            RestoreColor();
            return;
        }

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

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(resourceType, accumulatedAmount);
        }


        TryPlayVfx("ResourceCollect", transform.position);

        accumulatedAmount = 0;
        RestoreColor();
    }

    void DarkenColor()
    {
        if (machineRenderer == null) return;
        Color c = originalColor * 0.7f;
        machineRenderer.material.color = c;
    }

    void RestoreColor()
    {
        if (machineRenderer == null) return;
        machineRenderer.material.color = originalColor;
    }

    private void TryPlayVfx(string vfxKindName, Vector3 pos)
    {
        try
        {
            Type vfxManagerType = FindType("VFXManager");
            Type vfxKindType = FindType("VFXKind");
            if (vfxManagerType == null || vfxKindType == null) return;

            // EnsureInstance()
            MethodInfo ensure = vfxManagerType.GetMethod("EnsureInstance", BindingFlags.Public | BindingFlags.Static);
            ensure?.Invoke(null, null);

            // Instance property
            PropertyInfo instProp = vfxManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            object inst = instProp != null ? instProp.GetValue(null) : null;
            if (inst == null) return;

            object enumVal = Enum.Parse(vfxKindType, vfxKindName);
            MethodInfo play = vfxManagerType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance, null,
                new Type[] { vfxKindType, typeof(Vector3) }, null);

            play?.Invoke(inst, new object[] { enumVal, pos });
        }
        catch
        {
            // No spamear consola si no está disponible.
        }
    }

    private static Type FindType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = asm.GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }
}
