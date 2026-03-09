public abstract class PlayerState
{
    protected PlayerBunkerManager playerManager;

    public PlayerState(PlayerBunkerManager manager)
    {
        playerManager = manager;
    }

    public virtual void Enter() { }

    public virtual void Exit() { }

    public virtual void Update() { }

    public virtual void HandleInput() { }
}