using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class Level2ElectricityAutoSetup : MonoBehaviour
{
    [Header("Scene Object Names")]
    [SerializeField] private string _smallBulbNameContains = "Bombilla_pequeña";
    [SerializeField] private string _centralBulbName = "Bombilla_grande";
    [SerializeField] private string[] _barrierNameContains = { "Barrera", "Barrier", "barrera", "barrier" };

    [Header("Connection")]
    [SerializeField] private float _smallBulbConnectionRadius = 1.35f;
    [SerializeField] private float _centralBulbConnectionRadius = 1.85f;
    [SerializeField] private bool _smallBulbsArePowerSources = true;

    [Header("Behaviour")]
    [SerializeField] private bool _configureOnAwake = true;
    [SerializeField] private bool _addElectricBarrierToNamedObject = true;
    [SerializeField] private bool _rebuildNetworkAfterSetup = true;

    private void Awake()
    {
        if (_configureOnAwake)
            ConfigureScene();
    }

    [ContextMenu("Configure LEVEL2 Electricity")]
    public void ConfigureScene()
    {
        List<GameObject> smallBulbs = FindObjectsByNameContains(_smallBulbNameContains);
        for (int i = 0; i < smallBulbs.Count; i++)
            ConfigureSmallBulb(smallBulbs[i]);

        GameObject centralObject = FindObjectByExactName(_centralBulbName);
        ElectricCentralBulb centralBulb = null;
        if (centralObject != null)
            centralBulb = ConfigureCentralBulb(centralObject);

        ElectricBarrier barrier = FindOrCreateBarrierFromNamedObject();
        if (centralBulb != null && barrier != null)
        {
            centralBulb.SetBarrier(barrier);
            barrier.SetBarrierActive(false);
        }

        if (_rebuildNetworkAfterSetup)
        {
#if UNITY_2023_1_OR_NEWER
            ElectricityNetworkManager[] managers = FindObjectsByType<ElectricityNetworkManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            ElectricityNetworkManager[] managers = FindObjectsOfType<ElectricityNetworkManager>();
#endif
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null)
                    managers[i].RebuildNetwork();
            }
        }
    }

    private void ConfigureSmallBulb(GameObject bulbObject)
    {
        if (bulbObject == null)
            return;

        ElectricNode node = GetOrAdd<ElectricNode>(bulbObject);
        ElectricSmallBulb bulb = GetOrAdd<ElectricSmallBulb>(bulbObject);

        node.SetStartsAsPowerSource(_smallBulbsArePowerSources);
        node.SetPowerSourceActive(_smallBulbsArePowerSources);
        node.SetCanReceivePower(true);
        node.SetCanOutputPower(true);
        node.SetDisabled(false);
        node.SetConnectionRadius(_smallBulbConnectionRadius);
        node.SetLocalConnectionPoints(new[] { Vector3.zero });
        node.RefreshAutoVisuals();
        bulb.RefreshAutoVisuals();
    }

    private ElectricCentralBulb ConfigureCentralBulb(GameObject centralObject)
    {
        ElectricNode node = GetOrAdd<ElectricNode>(centralObject);
        ElectricCentralBulb bulb = GetOrAdd<ElectricCentralBulb>(centralObject);

        node.SetStartsAsPowerSource(false);
        node.SetPowerSourceActive(false);
        node.SetCanReceivePower(true);
        node.SetCanOutputPower(false);
        node.SetDisabled(false);
        node.SetConnectionRadius(_centralBulbConnectionRadius);
        node.SetLocalConnectionPoints(new[] { Vector3.zero });
        node.RefreshAutoVisuals();
        bulb.RefreshAutoVisuals();

        return bulb;
    }

    private ElectricBarrier FindOrCreateBarrierFromNamedObject()
    {
#if UNITY_2023_1_OR_NEWER
        ElectricBarrier existingBarrier = FindFirstObjectByType<ElectricBarrier>(FindObjectsInactive.Include);
#else
        ElectricBarrier existingBarrier = FindObjectOfType<ElectricBarrier>(true);
#endif
        if (existingBarrier != null)
            return existingBarrier;

        GameObject barrierObject = FindObjectByNameKeywords(_barrierNameContains);
        if (barrierObject == null)
            return null;

        if (!_addElectricBarrierToNamedObject)
            return barrierObject.GetComponent<ElectricBarrier>();

        ElectricBarrier barrier = GetOrAdd<ElectricBarrier>(barrierObject);
        barrier.CacheTargetsFromChildren();
        return barrier;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = target.AddComponent<T>();
        return component;
    }

    private static GameObject FindObjectByExactName(string exactName)
    {
        if (string.IsNullOrWhiteSpace(exactName))
            return null;

        Transform[] transforms = FindAllTransforms();
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && string.Equals(transforms[i].name, exactName, StringComparison.Ordinal))
                return transforms[i].gameObject;
        }

        return null;
    }

    private static GameObject FindObjectByNameKeywords(string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
            return null;

        Transform[] transforms = FindAllTransforms();
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == null)
                continue;

            string objectName = transforms[i].name;
            for (int j = 0; j < keywords.Length; j++)
            {
                string keyword = keywords[j];
                if (!string.IsNullOrWhiteSpace(keyword) && objectName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return transforms[i].gameObject;
            }
        }

        return null;
    }

    private static List<GameObject> FindObjectsByNameContains(string namePart)
    {
        List<GameObject> results = new List<GameObject>();
        if (string.IsNullOrWhiteSpace(namePart))
            return results;

        Transform[] transforms = FindAllTransforms();
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                results.Add(transforms[i].gameObject);
        }

        return results;
    }

    private static Transform[] FindAllTransforms()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<Transform>(true);
#endif
    }
}
