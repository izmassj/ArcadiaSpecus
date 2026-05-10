using UnityEngine;

public class BunkerAnalyticsHooks : MonoBehaviour
{
    [SerializeField] private BunkerResourceManager _resourceManager;

    private void Awake()
    {
        if (_resourceManager == null)
            _resourceManager = FindFirstObjectByType<BunkerResourceManager>();
    }

    private void OnEnable()
    {
        if (_resourceManager != null)
            _resourceManager.ResourcesAdded += HandleResourcesAdded;
    }

    private void OnDisable()
    {
        if (_resourceManager != null)
            _resourceManager.ResourcesAdded -= HandleResourcesAdded;
    }

    private void HandleResourcesAdded(BunkerResourceType resourceType, int amount)
    {
        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RegisterResourceCollected(resourceType.ToString(), amount);
    }
}