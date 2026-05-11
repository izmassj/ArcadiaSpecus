using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Level2ElectricityAutoSetup : MonoBehaviour
{
    [Header("Runtime Auto Setup")]
    [SerializeField] private bool _configureOnAwake = true;
    [SerializeField] private bool _configureRobotShockOnAwake;

    [Header("Names")]
    [SerializeField] private string[] _smallBulbNameParts = { "Bombilla_peque", "Bombilla pequeña", "SmallLightbulb" };
    [SerializeField] private string[] _centralBulbNameParts = { "Bombilla_grande", "Bombilla grande", "BigLightbulb" };
    [SerializeField] private string[] _barrierNameParts = { "Barrier", "Barrera", "barrier", "barrera" };
    [SerializeField] private string[] _routeParentNameParts = { "ElectricityParentRoute", "ElectricRoutes", "ElectricityRoute", "Electric Route", "Ruta" };

    [Header("Default Values")]
    [SerializeField] private float _smallBulbConnectionRadius = 2.5f;
    [SerializeField] private float _routeConnectionRadius = 0.8f;
    [SerializeField] private float _smallBulbTriggerRadius = 1.5f;

    private void Awake()
    {
        if (_configureOnAwake)
            ConfigureScene();

        if (_configureRobotShockOnAwake)
            ConfigureRobotShock();
    }

    [ContextMenu("Configure Scene")]
    public void ConfigureScene()
    {
        ConfigureRoutes();
        ElectricSmallBulb[] smallBulbs = ConfigureSmallBulbs();
        ElectricCentralBulb central = ConfigureCentralBulb(smallBulbs);
        ElectricBarrier barrier = ConfigureBarrier();

        if (central != null)
        {
            central.SetBarrierControlMode(ElectricCentralBulb.BarrierControlMode.AnySmallBulbSendingPower);
            central.SetLinkedSmallBulbs(smallBulbs);

            if (barrier != null)
                central.SetBarrier(barrier);
        }

        ElectricityNetworkManager manager = GetComponent<ElectricityNetworkManager>();
        if (manager != null)
            manager.RebuildNetwork();
    }

    public void ConfigureRobotShock()
    {
        RobotController robotController = FindObjectOfType<RobotController>();
        if (robotController == null)
            return;

        RobotShockController shockController = robotController.GetComponent<RobotShockController>();
        if (shockController == null)
            robotController.gameObject.AddComponent<RobotShockController>();
    }

    private void ConfigureRoutes()
    {
        Transform routeParent = FindFirstTransformByNameContains(_routeParentNameParts);
        if (routeParent == null)
            return;

        for (int i = 0; i < routeParent.childCount; i++)
        {
            Transform route = routeParent.GetChild(i);
            if (route == null || route.GetComponentInChildren<Renderer>(true) == null)
                continue;

            ElectricRouteNode routeNode = route.GetComponent<ElectricRouteNode>();
            if (routeNode == null)
                routeNode = route.gameObject.AddComponent<ElectricRouteNode>();

            routeNode.SetStartsAsPowerSource(false);
            routeNode.SetPowerSourceActive(false);
            routeNode.SetCanReceivePower(true);
            routeNode.SetCanOutputPower(true);
            routeNode.SetDisabled(false);
            routeNode.SetConnectionRadius(_routeConnectionRadius);
            routeNode.SetApplyVisuals(true);

            ElectricRoutePaintedTile paintedTile = route.GetComponent<ElectricRoutePaintedTile>();
            if (paintedTile == null)
            {
                paintedTile = route.gameObject.AddComponent<ElectricRoutePaintedTile>();
                paintedTile.InferGridPositionFromWorld(1f);
                paintedTile.SetConnections(true, true, true, true);
            }
        }
    }

    private ElectricSmallBulb[] ConfigureSmallBulbs()
    {
        GameObject[] bulbObjects = FindSceneObjectsByNameContains(_smallBulbNameParts);
        List<ElectricSmallBulb> bulbs = new List<ElectricSmallBulb>();

        for (int i = 0; i < bulbObjects.Length; i++)
        {
            GameObject bulbObject = bulbObjects[i];
            if (bulbObject == null)
                continue;

            ElectricNode node = bulbObject.GetComponent<ElectricNode>();
            if (node == null)
                node = bulbObject.AddComponent<ElectricNode>();

            ElectricSmallBulb bulb = bulbObject.GetComponent<ElectricSmallBulb>();
            if (bulb == null)
                bulb = bulbObject.AddComponent<ElectricSmallBulb>();

            node.SetStartsAsPowerSource(true);
            node.SetPowerSourceActive(true);
            node.SetCanReceivePower(true);
            node.SetCanOutputPower(true);
            node.SetDisabled(bulb.IsDisabledByShock);
            node.SetConnectionRadius(_smallBulbConnectionRadius);
            node.SetLocalConnectionPoints(new[] { Vector3.zero });
            node.SetApplyVisuals(false);

            bulb.RefreshAutoVisuals();
            EnsureTriggerCollider(bulbObject, _smallBulbTriggerRadius);
            bulbs.Add(bulb);
        }

        return bulbs.ToArray();
    }

    private ElectricCentralBulb ConfigureCentralBulb(ElectricSmallBulb[] smallBulbs)
    {
        GameObject centralObject = FindFirstSceneObjectByNameContains(_centralBulbNameParts);
        if (centralObject == null)
            return null;

        ElectricNode node = centralObject.GetComponent<ElectricNode>();
        if (node == null)
            node = centralObject.AddComponent<ElectricNode>();

        ElectricCentralBulb central = centralObject.GetComponent<ElectricCentralBulb>();
        if (central == null)
            central = centralObject.AddComponent<ElectricCentralBulb>();

        node.SetStartsAsPowerSource(false);
        node.SetPowerSourceActive(false);
        node.SetCanReceivePower(false);
        node.SetCanOutputPower(false);
        node.SetDisabled(false);
        node.SetLocalConnectionPoints(new[] { Vector3.zero });
        node.SetApplyVisuals(false);

        central.SetBarrierControlMode(ElectricCentralBulb.BarrierControlMode.AnySmallBulbSendingPower);
        central.SetLinkedSmallBulbs(smallBulbs);
        central.RefreshAutoVisuals();
        return central;
    }

    private ElectricBarrier ConfigureBarrier()
    {
        GameObject barrierObject = FindFirstSceneObjectByNameContains(_barrierNameParts);
        if (barrierObject == null)
            return null;

        ElectricBarrier barrier = barrierObject.GetComponent<ElectricBarrier>();
        if (barrier == null)
            barrier = barrierObject.AddComponent<ElectricBarrier>();

        barrier.CacheTargetsFromChildren();
        return barrier;
    }

    private void EnsureTriggerCollider(GameObject target, float radius)
    {
        if (target == null)
            return;

        Collider existingCollider = target.GetComponentInChildren<Collider>(true);
        if (existingCollider != null)
            return;

        SphereCollider trigger = target.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = Mathf.Max(0.1f, radius);
    }

    private GameObject FindFirstSceneObjectByNameContains(string[] nameParts)
    {
        GameObject[] objects = FindSceneObjectsByNameContains(nameParts);
        return objects.Length > 0 ? objects[0] : null;
    }

    private Transform FindFirstTransformByNameContains(string[] nameParts)
    {
#if UNITY_2023_1_OR_NEWER
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        Transform[] transforms = FindObjectsOfType<Transform>();
#endif
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transformItem = transforms[i];
            if (transformItem != null && NameContainsAny(transformItem.name, nameParts))
                return transformItem;
        }

        return null;
    }

    private GameObject[] FindSceneObjectsByNameContains(string[] nameParts)
    {
        List<GameObject> results = new List<GameObject>();
#if UNITY_2023_1_OR_NEWER
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        Transform[] transforms = FindObjectsOfType<Transform>();
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
}
