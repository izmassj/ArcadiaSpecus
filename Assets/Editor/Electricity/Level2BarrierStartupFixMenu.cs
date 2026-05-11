#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Level2BarrierStartupFixMenu
{
    [MenuItem("Tools/Arcadia/Electricity/Fix LEVEL2 Barrier Startup")]
    public static void FixBarrierStartup()
    {
        GameObject barrierObject = FindByNames("Barrier", "Barrera", "ElectricBarrier", "Electric Barrier");
        if (barrierObject == null)
        {
            Debug.LogWarning("No he encontrado ningún GameObject llamado Barrier/Barrera en la escena abierta.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(barrierObject, "Fix LEVEL2 Barrier Startup");
        barrierObject.SetActive(true);

        ElectricBarrier barrier = barrierObject.GetComponent<ElectricBarrier>();
        if (barrier == null)
            barrier = Undo.AddComponent<ElectricBarrier>(barrierObject);

        SerializedObject barrierSerialized = new SerializedObject(barrier);
        SetBool(barrierSerialized, "_applyInitialStateOnAwake", false);
        SetBool(barrierSerialized, "_activeOnStart", true);
        SetBool(barrierSerialized, "_autoCollectTargets", true);
        SetBool(barrierSerialized, "_controlBarrierRootActive", false);
        barrierSerialized.ApplyModifiedPropertiesWithoutUndo();

        Collider[] colliders = barrierObject.GetComponentsInChildren<Collider>(true);
        Renderer[] renderers = barrierObject.GetComponentsInChildren<Renderer>(true);
        ParticleSystem[] particles = barrierObject.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = true;

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;

        for (int i = 0; i < particles.Length; i++)
            particles[i].Play(true);

        GameObject bigLightbulb = FindByNames("BigLightbulb", "Bombilla_grande", "Bombilla grande");
        if (bigLightbulb != null)
        {
            ElectricCentralBulb central = bigLightbulb.GetComponent<ElectricCentralBulb>();
            if (central == null)
                central = Undo.AddComponent<ElectricCentralBulb>(bigLightbulb);

            SerializedObject centralSerialized = new SerializedObject(central);
            SerializedProperty barrierProperty = centralSerialized.FindProperty("_barrier");
            if (barrierProperty != null)
                barrierProperty.objectReferenceValue = barrier;

            SetBool(centralSerialized, "_autoFindBarrier", true);
            SetBool(centralSerialized, "_activateBarrierWhenPowered", true);
            SerializedProperty delayProperty = centralSerialized.FindProperty("_startupDelayBeforeFirstApply");
            if (delayProperty != null)
                delayProperty.floatValue = 0.05f;
            centralSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(central);
        }

        EditorUtility.SetDirty(barrier);
        EditorUtility.SetDirty(barrierObject);
        Debug.Log("LEVEL2 Barrier Startup corregido: la barrera ya no se apaga en Awake/OnEnable. Guarda la escena.");
    }

    private static GameObject FindByNames(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            GameObject found = GameObject.Find(names[i]);
            if (found != null)
                return found;
        }

        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null)
                continue;

            for (int j = 0; j < names.Length; j++)
            {
                if (t.name == names[j])
                    return t.gameObject;
            }
        }

        return null;
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }
}
#endif
