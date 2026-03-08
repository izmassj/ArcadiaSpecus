using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private PlayerState _currentState;

    public IdleState idleState;
    public BuildState buildState;
    public SelectRoomState selectRoomState;
    public NavigateState navigateState;

    void Awake()
    {
        idleState = new IdleState(this);
        buildState = new BuildState(this);
        selectRoomState = new SelectRoomState(this);
        navigateState = new NavigateState(this);
    }

    void Start()
    {
        ChangeState(idleState);
    }

    void Update()
    {
        _currentState.HandleInput();
        _currentState.Update();
    }

    public void ChangeState(PlayerState newState)
    {
        if (_currentState != null)
            _currentState.Exit();

        _currentState = newState;

        _currentState.Enter();
    }
}
