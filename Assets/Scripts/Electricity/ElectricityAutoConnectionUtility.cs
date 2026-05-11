using System;
using UnityEngine;

public static class ElectricityAutoConnectionUtility
{
    public static bool SnapNodeToNearestRoute(ElectricNode node, Transform sourceTransform, float maxDistance, float connectionRadius)
    {
        if (node == null || sourceTransform == null)
            return false;

        ElectricRouteNode nearestRoute;
        Vector3 nearestPoint;
        if (!TryGetNearestRouteConnectionPoint(sourceTransform.position, maxDistance, out nearestRoute, out nearestPoint))
            return false;

        node.SetConnectionRadius(connectionRadius);
        node.SetLocalConnectionPoints(new[] { node.transform.InverseTransformPoint(nearestPoint) });
        return true;
    }

    public static bool TryGetNearestRouteConnectionPoint(Vector3 fromPosition, float maxDistance, out ElectricRouteNode nearestRoute, out Vector3 nearestPoint)
    {
        nearestRoute = null;
        nearestPoint = fromPosition;

        ElectricRouteNode[] routes = FindRouteNodes();
        if (routes == null || routes.Length == 0)
            return false;

        float maxSqrDistance = maxDistance > 0f ? maxDistance * maxDistance : float.PositiveInfinity;
        float bestSqrDistance = float.PositiveInfinity;

        for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
        {
            ElectricRouteNode route = routes[routeIndex];
            if (route == null || !route.isActiveAndEnabled)
                continue;

            int pointCount = route.ConnectionPointCount;
            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                Vector3 point = route.GetConnectionPointWorldPosition(pointIndex);
                float sqrDistance = (point - fromPosition).sqrMagnitude;
                if (sqrDistance > maxSqrDistance || sqrDistance >= bestSqrDistance)
                    continue;

                bestSqrDistance = sqrDistance;
                nearestRoute = route;
                nearestPoint = point;
            }
        }

        return nearestRoute != null;
    }

    public static ElectricBarrier FindOrCreateBarrier(string[] nameKeywords, bool addComponentIfMissing)
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        ElectricBarrier existingBarrier = UnityEngine.Object.FindFirstObjectByType<ElectricBarrier>(FindObjectsInactive.Include);
#else
        ElectricBarrier existingBarrier = UnityEngine.Object.FindObjectOfType<ElectricBarrier>(true);
#endif
        if (existingBarrier != null)
            return existingBarrier;

        GameObject barrierObject = FindObjectByNameKeywords(nameKeywords);
        if (barrierObject == null)
            return null;

        ElectricBarrier barrier = barrierObject.GetComponent<ElectricBarrier>();
        if (barrier == null && addComponentIfMissing)
            barrier = barrierObject.AddComponent<ElectricBarrier>();

        if (barrier != null)
            barrier.CacheTargetsFromChildren();

        return barrier;
    }

    public static GameObject FindObjectByExactName(params string[] exactNames)
    {
        if (exactNames == null || exactNames.Length == 0)
            return null;

        Transform[] transforms = FindAllTransforms();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null)
                continue;

            for (int nameIndex = 0; nameIndex < exactNames.Length; nameIndex++)
            {
                string exactName = exactNames[nameIndex];
                if (!string.IsNullOrWhiteSpace(exactName) && string.Equals(target.name, exactName, StringComparison.Ordinal))
                    return target.gameObject;
            }
        }

        return null;
    }

    public static GameObject FindObjectByNameKeywords(string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
            return null;

        Transform[] transforms = FindAllTransforms();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null)
                continue;

            string objectName = target.name;
            for (int keywordIndex = 0; keywordIndex < keywords.Length; keywordIndex++)
            {
                string keyword = keywords[keywordIndex];
                if (!string.IsNullOrWhiteSpace(keyword) && objectName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return target.gameObject;
            }
        }

        return null;
    }

    public static Transform[] FindAllTransforms()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<Transform>(true);
#endif
    }

    public static ElectricRouteNode[] FindRouteNodes()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<ElectricRouteNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<ElectricRouteNode>();
#endif
    }
}
