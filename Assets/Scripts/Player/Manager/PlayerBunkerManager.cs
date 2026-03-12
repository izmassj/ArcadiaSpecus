using System;
using UnityEngine;

public class PlayerBunkerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBunkerUIManager _playerUIManager;

    private PlayerBunkerState _currentState;

    public IdleState idleState;
    public BuildRoomState buildRoomState;
    public SelectRoomState selectRoomState;
    public NavigateState navigateState;

    // referencia para la UI para los estados
    public PlayerBunkerUIManager UI
    {
        get
        {
            return _playerUIManager;
        }
    }

    void Awake()
    {
        idleState = new IdleState(this);
        buildRoomState = new BuildRoomState(this);
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

    public void ChangeState(PlayerBunkerState newState)
    {
        if (_currentState != null)
            _currentState.Exit();

        _currentState = newState;

        _currentState.Enter();
    }

    public void EnterBuildRoomMode()
    {
        ChangeState(buildRoomState);
    }

    public void EnterPlaceMachineMode()
    {

    }
}
