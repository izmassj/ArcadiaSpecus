using System.Collections.Generic;
using Linework.Common;
using UnityEngine;

[DisallowMultipleComponent]
public class ChildRenderersOutlineOverride : MonoBehaviour
{
    [Header("Renderer Collection")]
    [SerializeField] private Transform root;
    [SerializeField] private bool includeInactiveRenderers = true;
    [SerializeField] private bool clearChildRenderersNotInList = true;
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

    [Header("Outline Overrides")]
    public List<ShaderPropertyOverride> overrides = new List<ShaderPropertyOverride>();

    [Header("Runtime")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool clearWhenDisabled = true;

    private MaterialPropertyBlock propertyBlock;

    public List<Renderer> TargetRenderers => targetRenderers;

    private void Reset()
    {
        root = transform;
        CollectChildRenderers();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            ApplyOverrides();
        }
    }

    private void OnDisable()
    {
        if (clearWhenDisabled)
        {
            ClearOverrides();
        }
    }

    private void OnValidate()
    {
        if (root == null)
        {
            root = transform;
        }

        RemoveNullRenderers();

        if (enabled && applyOnEnable)
        {
            ApplyOverrides();
        }
    }

    [ContextMenu("Collect Child Renderers")]
    public void CollectChildRenderers()
    {
        if (root == null)
        {
            root = transform;
        }

        targetRenderers.Clear();
        Renderer[] childRenderers = root.GetComponentsInChildren<Renderer>(includeInactiveRenderers);

        for (int i = 0; i < childRenderers.Length; i++)
        {
            Renderer childRenderer = childRenderers[i];

            if (childRenderer != null && !targetRenderers.Contains(childRenderer))
            {
                targetRenderers.Add(childRenderer);
            }
        }

        ApplyOverrides();
    }

    [ContextMenu("Apply Overrides")]
    public void ApplyOverrides()
    {
        RemoveNullRenderers();

        if (!enabled)
        {
            ClearOverrides();
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        propertyBlock.Clear();

        for (int i = 0; i < overrides.Count; i++)
        {
            ShaderPropertyOverride propertyOverride = overrides[i];

            if (propertyOverride == null || string.IsNullOrWhiteSpace(propertyOverride.propertyName))
            {
                continue;
            }

            propertyOverride.CachePropertyID();

            switch (propertyOverride.type)
            {
                case ShaderPropertyType.Float:
                    propertyBlock.SetFloat(propertyOverride.propertyId, propertyOverride.floatValue);
                    break;

                case ShaderPropertyType.Int:
                    propertyBlock.SetInt(propertyOverride.propertyId, propertyOverride.intValue);
                    break;

                case ShaderPropertyType.Vector:
                    propertyBlock.SetVector(propertyOverride.propertyId, propertyOverride.vectorValue);
                    break;

                case ShaderPropertyType.Color:
                    propertyBlock.SetColor(propertyOverride.propertyId, propertyOverride.colorValue);
                    break;

                default:
                    Debug.LogWarning("Unsupported shader property type: " + propertyOverride.type, this);
                    break;
            }
        }

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            Renderer targetRenderer = targetRenderers[i];

            if (targetRenderer != null)
            {
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        if (clearChildRenderersNotInList)
        {
            ClearChildRenderersNotInTargetList();
        }
    }

    [ContextMenu("Clear Overrides")]
    public void ClearOverrides()
    {
        RemoveNullRenderers();

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            Renderer targetRenderer = targetRenderers[i];

            if (targetRenderer != null)
            {
                targetRenderer.SetPropertyBlock(null);
            }
        }
    }

    public void AddFloatOverride(string propertyName, float value)
    {
        overrides.Add(new ShaderPropertyOverride
        {
            type = ShaderPropertyType.Float,
            propertyName = propertyName,
            floatValue = value
        });

        ApplyOverrides();
    }

    public void AddIntOverride(string propertyName, int value)
    {
        overrides.Add(new ShaderPropertyOverride
        {
            type = ShaderPropertyType.Int,
            propertyName = propertyName,
            intValue = value
        });

        ApplyOverrides();
    }

    public void AddColorOverride(string propertyName, Color color)
    {
        overrides.Add(new ShaderPropertyOverride
        {
            type = ShaderPropertyType.Color,
            propertyName = propertyName,
            colorValue = color
        });

        ApplyOverrides();
    }

    public void AddVectorOverride(string propertyName, Vector4 value)
    {
        overrides.Add(new ShaderPropertyOverride
        {
            type = ShaderPropertyType.Vector,
            propertyName = propertyName,
            vectorValue = value
        });

        ApplyOverrides();
    }

    private void ClearChildRenderersNotInTargetList()
    {
        if (root == null)
        {
            return;
        }

        Renderer[] childRenderers = root.GetComponentsInChildren<Renderer>(includeInactiveRenderers);

        for (int i = 0; i < childRenderers.Length; i++)
        {
            Renderer childRenderer = childRenderers[i];

            if (childRenderer != null && !targetRenderers.Contains(childRenderer))
            {
                childRenderer.SetPropertyBlock(null);
            }
        }
    }

    private void RemoveNullRenderers()
    {
        for (int i = targetRenderers.Count - 1; i >= 0; i--)
        {
            if (targetRenderers[i] == null)
            {
                targetRenderers.RemoveAt(i);
            }
        }
    }
}
