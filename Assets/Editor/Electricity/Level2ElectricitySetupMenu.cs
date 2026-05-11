using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Level2ElectricitySetupMenu
{
    private const string PlayerRobotInputActionsPath = "Assets/InputActions/PlayerRobot.inputactions";

    [MenuItem("Tools/Arcadia/Electricity/Repair LEVEL2 Electricity And Shock")]
    public static void RepairLevel2ElectricityAndShock()
    {
        ConfigureLevel2BulbChannelizer();
    }

    [MenuItem("Tools/Arcadia/Electricity/Configure LEVEL2 Bulb Channelizer")]
    public static void ConfigureLevel2BulbChannelizer()
    {
        ElectricityNetworkManager manager = EnsureNetworkManager();
        ConfigureNetworkManager(manager);
        ConfigureRoutes();
        ElectricSmallBulb[] smallBulbs = ConfigureSmallBulbs();
        ElectricBarrier barrier = ConfigureBarrier();
        ElectricCentralBulb centralBulb = ConfigureCentralBulb(smallBulbs, barrier);
        ConfigureRobotShockController();

        Level2ElectricityAutoSetup autoSetup = manager.GetComponent<Level2ElectricityAutoSetup>();
        if (autoSetup == null)
            autoSetup = Undo.AddComponent<Level2ElectricityAutoSetup>(manager.gameObject);

        Undo.RecordObject(autoSetup, "Configure LEVEL2 Electricity Auto Setup");
        SerializedObject autoSetupSerialized = new SerializedObject(autoSetup);
        SetBool(autoSetupSerialized, "_configureOnAwake", true);
        SetBool(autoSetupSerialized, "_configureRobotShockOnAwake", false);
        autoSetupSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(autoSetup);

        if (manager != null)
        {
            manager.RebuildNetwork();
            EditorUtility.SetDirty(manager);
        }

        if (centralBulb != null)
            EditorUtility.SetDirty(centralBulb);

        MarkSceneDirty();
        Debug.Log("LEVEL2 configurado: BigLightbulb ya no necesita conectarse a la ruta. La barrera se mantiene activa mientras al menos una SmallLightbulb siga encendida; cuando las 3 se apagan por Shock, la barrera baja.");
    }

    private static ElectricityNetworkManager EnsureNetworkManager()
    {
#if UNITY_2023_1_OR_NEWER
        ElectricityNetworkManager manager = Object.FindFirstObjectByType<ElectricityNetworkManager>(FindObjectsInactive.Include);
#else
        ElectricityNetworkManager manager = Object.FindObjectOfType<ElectricityNetworkManager>(true);
#endif
        if (manager != null)
            return manager;

        GameObject managerObject = new GameObject("ElectricityNetworkManager");
        Undo.RegisterCreatedObjectUndo(managerObject, "Create Electricity Network Manager");
        return Undo.AddComponent<ElectricityNetworkManager>(managerObject);
    }

    private static void ConfigureNetworkManager(ElectricityNetworkManager manager)
    {
        if (manager == null)
            return;

        Undo.RecordObject(manager, "Configure Electricity Network Manager");
        SerializedObject serializedObject = new SerializedObject(manager);
        SetBool(serializedObject, "_autoFindNodesOnStart", true);
        SetBool(serializedObject, "_autoRebuildWhenRequested", true);
        SetBool(serializedObject, "_recalculateEveryFrame", true);
        SetBool(serializedObject, "_connectPaintedRouteTiles", true);
        SetBool(serializedObject, "_connectExternalNodesToPaintedRoutes", true);
        SetBool(serializedObject, "_fallbackConnectRoutesByDistance", true);
        SetFloat(serializedObject, "_externalRouteConnectDistance", 4f);
        SetFloat(serializedObject, "_routeRouteFallbackConnectDistance", 1.35f);
        SetFloat(serializedObject, "_extraConnectionTolerance", 0.08f);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
    }

    private static void ConfigureRoutes()
    {
        Transform routeParent = FindFirstTransformByNameContains(new[] { "ElectricityParentRoute", "ElectricRoutes", "ElectricityRoute", "Electric Route", "Ruta" });
        if (routeParent == null)
            return;

        for (int i = 0; i < routeParent.childCount; i++)
        {
            Transform routeTransform = routeParent.GetChild(i);
            if (routeTransform == null || routeTransform.GetComponentInChildren<Renderer>(true) == null)
                continue;

            ElectricRouteNode routeNode = routeTransform.GetComponent<ElectricRouteNode>();
            if (routeNode == null)
                routeNode = Undo.AddComponent<ElectricRouteNode>(routeTransform.gameObject);

            Undo.RecordObject(routeNode, "Configure Electric Route Node");
            routeNode.SetStartsAsPowerSource(false);
            routeNode.SetPowerSourceActive(false);
            routeNode.SetCanReceivePower(true);
            routeNode.SetCanOutputPower(true);
            routeNode.SetDisabled(false);
            routeNode.SetConnectionRadius(0.8f);
            routeNode.SetApplyVisuals(true);

            ElectricRoutePaintedTile paintedTile = routeTransform.GetComponent<ElectricRoutePaintedTile>();
            if (paintedTile == null)
            {
                paintedTile = Undo.AddComponent<ElectricRoutePaintedTile>(routeTransform.gameObject);
                paintedTile.InferGridPositionFromWorld(1f);
                paintedTile.SetConnections(true, true, true, true);
            }

            EditorUtility.SetDirty(routeNode);
            EditorUtility.SetDirty(paintedTile);
        }
    }

    private static ElectricSmallBulb[] ConfigureSmallBulbs()
    {
        GameObject[] bulbObjects = FindSceneObjectsByNameContains(new[] { "Bombilla_peque", "Bombilla pequeña", "SmallLightbulb" });
        List<ElectricSmallBulb> bulbs = new List<ElectricSmallBulb>();

        for (int i = 0; i < bulbObjects.Length; i++)
        {
            GameObject bulbObject = bulbObjects[i];
            if (bulbObject == null)
                continue;

            ElectricNode node = bulbObject.GetComponent<ElectricNode>();
            if (node == null)
                node = Undo.AddComponent<ElectricNode>(bulbObject);

            ElectricSmallBulb bulb = bulbObject.GetComponent<ElectricSmallBulb>();
            if (bulb == null)
                bulb = Undo.AddComponent<ElectricSmallBulb>(bulbObject);

            Undo.RecordObject(node, "Configure Small Electric Bulb Node");
            node.SetStartsAsPowerSource(true);
            node.SetPowerSourceActive(true);
            node.SetCanReceivePower(true);
            node.SetCanOutputPower(true);
            node.SetDisabled(bulb.IsDisabledByShock);
            node.SetConnectionRadius(2.5f);
            node.SetLocalConnectionPoints(new[] { Vector3.zero });
            node.SetApplyVisuals(false);

            Undo.RecordObject(bulb, "Configure Small Electric Bulb");
            SerializedObject bulbSerialized = new SerializedObject(bulb);
            SetBool(bulbSerialized, "_shockDisablesBulb", true);
            SetBool(bulbSerialized, "_onlyShockOnce", true);
            SetBool(bulbSerialized, "_stopParticlesWhenDisabled", true);
            SetBool(bulbSerialized, "_clearParticlesWhenDisabled", true);
            SetBool(bulbSerialized, "_deactivateParticleObjectsWhenDisabled", true);
            SetBool(bulbSerialized, "_playParticlesWhenReset", true);
            bulbSerialized.ApplyModifiedProperties();
            bulb.RefreshAutoVisuals();

            EnsureTriggerCollider(bulbObject, 1.5f);
            bulbs.Add(bulb);

            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(bulb);
        }

        return bulbs.ToArray();
    }

    private static ElectricCentralBulb ConfigureCentralBulb(ElectricSmallBulb[] smallBulbs, ElectricBarrier barrier)
    {
        GameObject centralObject = FindFirstSceneObjectByNameContains(new[] { "Bombilla_grande", "Bombilla grande", "BigLightbulb" });
        if (centralObject == null)
        {
            Debug.LogWarning("No se ha encontrado BigLightbulb/Bombilla_grande en la escena abierta.");
            return null;
        }

        ElectricNode node = centralObject.GetComponent<ElectricNode>();
        if (node == null)
            node = Undo.AddComponent<ElectricNode>(centralObject);

        ElectricCentralBulb centralBulb = centralObject.GetComponent<ElectricCentralBulb>();
        if (centralBulb == null)
            centralBulb = Undo.AddComponent<ElectricCentralBulb>(centralObject);

        Undo.RecordObject(node, "Configure Big Lightbulb Node");
        node.SetStartsAsPowerSource(false);
        node.SetPowerSourceActive(false);
        node.SetCanReceivePower(false);
        node.SetCanOutputPower(false);
        node.SetDisabled(false);
        node.SetLocalConnectionPoints(new[] { Vector3.zero });
        node.SetApplyVisuals(false);

        Undo.RecordObject(centralBulb, "Configure Big Lightbulb Channelizer");
        centralBulb.SetBarrierControlMode(ElectricCentralBulb.BarrierControlMode.AnySmallBulbSendingPower);
        centralBulb.SetLinkedSmallBulbs(smallBulbs);
        if (barrier != null)
            centralBulb.SetBarrier(barrier);

        SerializedObject centralSerialized = new SerializedObject(centralBulb);
        SetEnum(centralSerialized, "_barrierControlMode", (int)ElectricCentralBulb.BarrierControlMode.AnySmallBulbSendingPower);
        SetBool(centralSerialized, "_autoFindBarrier", true);
        SetBool(centralSerialized, "_autoFindSmallBulbs", true);
        SetBool(centralSerialized, "_activateBarrierWhenPowered", true);
        SetBool(centralSerialized, "_doNotForceBarrierOffBeforeFirstNetworkUpdate", true);
        SetFloat(centralSerialized, "_firstUpdateDelay", 0.05f);
        SetFloat(centralSerialized, "_startupDelayBeforeFirstApply", 0.05f);
        centralSerialized.ApplyModifiedProperties();
        centralBulb.RefreshAutoVisuals();

        EditorUtility.SetDirty(node);
        EditorUtility.SetDirty(centralBulb);
        return centralBulb;
    }

    private static ElectricBarrier ConfigureBarrier()
    {
        GameObject barrierObject = FindFirstSceneObjectByNameContains(new[] { "Barrier", "Barrera", "barrier", "barrera" });
        if (barrierObject == null)
        {
            Debug.LogWarning("No se ha encontrado ningún objeto llamado Barrier/Barrera en la escena abierta.");
            return null;
        }

        ElectricBarrier barrier = barrierObject.GetComponent<ElectricBarrier>();
        if (barrier == null)
            barrier = Undo.AddComponent<ElectricBarrier>(barrierObject);

        Undo.RecordObject(barrier, "Configure Electric Barrier");
        SerializedObject barrierSerialized = new SerializedObject(barrier);
        SetBool(barrierSerialized, "_applyInitialStateOnAwake", false);
        SetBool(barrierSerialized, "_applyStateOnAwake", false);
        SetBool(barrierSerialized, "_activeOnStart", true);
        SetBool(barrierSerialized, "_autoCollectTargets", true);
        SetBool(barrierSerialized, "_autoFindTargetsWhenEmpty", true);
        SetBool(barrierSerialized, "_controlBarrierRootActive", false);
        SetBool(barrierSerialized, "_controlBarrierRoot", false);
        SetBool(barrierSerialized, "_controlBehaviours", false);
        SetObject(barrierSerialized, "_barrierRoot", barrierObject);
        barrierSerialized.ApplyModifiedProperties();
        barrier.CacheTargetsFromChildren();
        barrier.SetBarrierActive(true);

        EditorUtility.SetDirty(barrier);
        return barrier;
    }

    private static void ConfigureRobotShockController()
    {
#if UNITY_2023_1_OR_NEWER
        RobotController robotController = Object.FindFirstObjectByType<RobotController>(FindObjectsInactive.Include);
#else
        RobotController robotController = Object.FindObjectOfType<RobotController>(true);
#endif
        if (robotController == null)
            return;

        RobotShockController shockController = robotController.GetComponent<RobotShockController>();
        if (shockController == null)
            shockController = Undo.AddComponent<RobotShockController>(robotController.gameObject);

        Undo.RecordObject(shockController, "Configure Robot Shock Controller");
        SerializedObject shockSerialized = new SerializedObject(shockController);
        SetObject(shockSerialized, "_robotController", robotController);
        SetObject(shockSerialized, "_shockCenter", robotController.transform);
        SetObject(shockSerialized, "_shockEffectObject", FindChildGameObjectByName(robotController.transform, "CFXR Electrified 3"));
        SetObject(shockSerialized, "_inputActions", AssetDatabase.LoadAssetAtPath<Object>(PlayerRobotInputActionsPath));
        SetString(shockSerialized, "_gameplayMapName", "Gameplay");
        SetString(shockSerialized, "_shockActionName", "Shock");
        SetBool(shockSerialized, "_useKeyboardFallback", true);
        SetBool(shockSerialized, "_useGamepadFallback", true);
        SetFloat(shockSerialized, "_shockRadius", 5.5f);
        SetFloat(shockSerialized, "_cooldownSeconds", 2f);
        SetBool(shockSerialized, "_alsoFindShockablesWithoutCollider", true);
        SetBool(shockSerialized, "_requireLineOfSight", false);
        shockSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(shockController);
    }

    private static void EnsureTriggerCollider(GameObject target, float radius)
    {
        if (target == null)
            return;

        Collider existingCollider = target.GetComponentInChildren<Collider>(true);
        if (existingCollider != null)
            return;

        SphereCollider trigger = Undo.AddComponent<SphereCollider>(target);
        trigger.isTrigger = true;
        trigger.radius = Mathf.Max(0.1f, radius);
        EditorUtility.SetDirty(trigger);
    }

    private static GameObject[] FindSceneObjectsByNameContains(string[] nameParts)
    {
        List<GameObject> results = new List<GameObject>();
#if UNITY_2023_1_OR_NEWER
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
#endif
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transformItem = transforms[i];
            if (transformItem == null)
                continue;

            if (NameContainsAny(transformItem.name, nameParts))
                results.Add(transformItem.gameObject);
        }

        return results.ToArray();
    }

    private static GameObject FindFirstSceneObjectByNameContains(string[] nameParts)
    {
        GameObject[] objects = FindSceneObjectsByNameContains(nameParts);
        return objects.Length > 0 ? objects[0] : null;
    }

    private static Transform FindFirstTransformByNameContains(string[] nameParts)
    {
        GameObject obj = FindFirstSceneObjectByNameContains(nameParts);
        return obj != null ? obj.transform : null;
    }

    private static bool NameContainsAny(string name, string[] nameParts)
    {
        if (string.IsNullOrEmpty(name) || nameParts == null)
            return false;

        for (int i = 0; i < nameParts.Length; i++)
        {
            string part = nameParts[i];
            if (!string.IsNullOrEmpty(part) && name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static GameObject FindChildGameObjectByName(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
            return null;

        if (root.name == objectName)
            return root.gameObject;

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject found = FindChildGameObjectByName(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.stringValue = value;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetEnum(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.enumValueIndex = value;
    }

    private static void MarkSceneDirty()
    {
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
