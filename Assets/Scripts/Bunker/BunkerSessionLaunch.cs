public enum BunkerLaunchOperation
{
    None,
    Create,
    Load
}

public struct BunkerPendingRewardData
{
    public int scrap;
    public int electricity;
    public int water;
    public int food;
    public int joinedInhabitants;

    public bool HasAnyReward()
    {
        return scrap > 0 || electricity > 0 || water > 0 || food > 0;
    }

    public bool HasAnyOutcome()
    {
        return HasAnyReward() || joinedInhabitants > 0;
    }
}

public static class BunkerSessionLaunch
{
    public static string CurrentSlotId { get; private set; }
    public static BunkerLaunchOperation PendingOperation { get; private set; }
    public static string CurrentBunkerSceneName { get; private set; }

    private static bool _revealOnNextSceneLoad;
    private static BunkerPendingRewardData _pendingRewardData;

    public static void BeginCreate(string slotId, string bunkerSceneName = null)
    {
        CurrentSlotId = slotId;
        PendingOperation = BunkerLaunchOperation.Create;

        if (!string.IsNullOrWhiteSpace(bunkerSceneName))
            CurrentBunkerSceneName = bunkerSceneName;
    }

    public static void BeginLoad(string slotId, string bunkerSceneName = null)
    {
        CurrentSlotId = slotId;
        PendingOperation = BunkerLaunchOperation.Load;

        if (!string.IsNullOrWhiteSpace(bunkerSceneName))
            CurrentBunkerSceneName = bunkerSceneName;
    }

    public static BunkerLaunchOperation ConsumePendingOperation()
    {
        BunkerLaunchOperation operation = PendingOperation;
        PendingOperation = BunkerLaunchOperation.None;
        return operation;
    }

    public static void SetCurrentBunkerScene(string bunkerSceneName)
    {
        if (string.IsNullOrWhiteSpace(bunkerSceneName))
            return;

        CurrentBunkerSceneName = bunkerSceneName;
    }

    public static void RequestRevealOnNextSceneLoad()
    {
        _revealOnNextSceneLoad = true;
    }

    public static bool ConsumeRevealOnNextSceneLoad()
    {
        bool value = _revealOnNextSceneLoad;
        _revealOnNextSceneLoad = false;
        return value;
    }

    public static void AddPendingReward(BunkerResourceType resourceType, int amount)
    {
        if (amount <= 0)
            return;

        switch (resourceType)
        {
            case BunkerResourceType.Scrap:
                _pendingRewardData.scrap += amount;
                break;
            case BunkerResourceType.Electricity:
                _pendingRewardData.electricity += amount;
                break;
            case BunkerResourceType.Water:
                _pendingRewardData.water += amount;
                break;
            case BunkerResourceType.Food:
                _pendingRewardData.food += amount;
                break;
        }
    }

    public static void AddPendingJoinedInhabitants(int amount)
    {
        if (amount <= 0)
            return;

        _pendingRewardData.joinedInhabitants += amount;
    }

    public static BunkerPendingRewardData ConsumePendingRewards()
    {
        BunkerPendingRewardData rewards = _pendingRewardData;
        _pendingRewardData = default;
        return rewards;
    }

    public static void ClearSession()
    {
        CurrentSlotId = null;
        PendingOperation = BunkerLaunchOperation.None;
        CurrentBunkerSceneName = null;
        _revealOnNextSceneLoad = false;
        _pendingRewardData = default;
    }
}
