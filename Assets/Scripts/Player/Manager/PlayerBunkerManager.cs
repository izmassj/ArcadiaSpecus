using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBunkerManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] public Camera mainCamera;

    [Header("References")]
    [SerializeField] private PlayerBunkerUIManager _playerUIManager;

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerBunkerInputAction;

    [Header("Room Building")]
    [SerializeField] public float roomPlacementDistance;
    [SerializeField] public List<RoomPrefab> prefabsRoom;


    private InputActionMap _gameplayInputActionMap;
    [HideInInspector] public InputAction navigateInputAction;
    [HideInInspector] public InputAction confirmInputAction;

    private PlayerBunkerState _currentState;

    public IdleState idleState;
    public BuildRoomState buildRoomState;
    public SelectRoomState selectRoomState;
    public NavigateState navigateState;
    public PlaceMachineState placeMachineState;

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
        placeMachineState = new PlaceMachineState(this);
        navigateState = new NavigateState(this);
    }

    void Start()
    {
        StartInputActions();
        ChangeState(idleState);
    }

    void Update()
    {
        _currentState.HandleInput();
        _currentState.Update();
    }

    private void StartInputActions()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        _gameplayInputActionMap = _playerBunkerInputAction.FindActionMap("Gameplay", true);
        navigateInputAction = _gameplayInputActionMap.FindAction("Navigate", true);
        confirmInputAction = _gameplayInputActionMap.FindAction("Confirm", true);
    }

    public PlayerBunkerState GetCurrentState()
    {
        return _currentState;
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
        ChangeState(placeMachineState);
    }
}
