public abstract class PlayerState
{
    protected PlayerManager playerManager;

    public PlayerState(PlayerManager manager)
    {
        playerManager = manager;
    }

    public virtual void Enter() { }

    public virtual void Exit() { }

    public virtual void Update() { }

    public virtual void HandleInput() { }
}