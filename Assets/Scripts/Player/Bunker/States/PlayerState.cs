public abstract class PlayerBunkerState
{
    protected PlayerBunkerManager playerManager;

    public PlayerBunkerState(PlayerBunkerManager manager)
    {
        playerManager = manager;
    }

    public virtual void Enter() { }

    public virtual void Exit() { }

    public virtual void Update() { }

    public virtual void HandleInput() { }
}