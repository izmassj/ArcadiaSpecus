using LineworkLite.FreeOutline;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBunkerManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] public Camera mainCamera;
    [SerializeField] public Camera syncedCamera;
    [SerializeField] public CinemachineCamera navigationVirtualCamera;
    [SerializeField] public CinemachineConfiner2D navigationConfiner2D;

    [Header("Navigate State - Mouse Drag")]
    [SerializeField] public float cameraMouseSensitivity = 1f;
    [SerializeField] public bool cameraInvert = false;

    [Header("Navigate State - Stick")]
    [SerializeField] public float cameraStickSpeed = 12f;
    [SerializeField] public float cameraStickDeadzone = 0.15f;

    [Header("Navigate State - Smooth")]
    [SerializeField] public float cameraSharpness = 12f;

    [Header("Navigate State - Inertia")]
    [SerializeField] public bool cameraEnableInertia = true;
    [SerializeField] public float cameraInertiaDecay = 8f;
    [SerializeField] public float cameraInertiaMaxSpeed = 35f;
    [SerializeField] public float cameraInertiaStopSpeed = 0.05f;

    [Header("Layers")]
    [SerializeField] public LayerMask interactaingRoomsLayer;
    [SerializeField] public LayerMask defaultLayer;
    [SerializeField] public LayerMask outlineLayer;

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
    [HideInInspector] public InputAction unconfirmInputAction;
    [HideInInspector] public InputAction dragInputAction;

    private PlayerBunkerState _currentState;

    public IdleState idleState;
    public BuildRoomState buildRoomState;
    public SelectRoomState selectRoomState;
    public NavigateState navigateState;
    public PlaceMachineState placeMachineState;

    public PlayerBunkerUIManager UI => _playerUIManager;

    private void Awake()
    {
        idleState = new IdleState(this);
        buildRoomState = new BuildRoomState(this);
        selectRoomState = new SelectRoomState(this);
        placeMachineState = new PlaceMachineState(this);
        navigateState = new NavigateState(this);
    }

    private void Start()
    {
        StartInputActions();
        ChangeState(navigateState);
    }

    private void Update()
    {
        _currentState?.HandleInput();
        _currentState?.Update();
    }

    private void StartInputActions()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        _gameplayInputActionMap = _playerBunkerInputAction.FindActionMap("Gameplay", true);

        navigateInputAction = _gameplayInputActionMap.FindAction("Navigate", true);
        confirmInputAction = _gameplayInputActionMap.FindAction("Confirm", true);
        unconfirmInputAction = _gameplayInputActionMap.FindAction("Unconfirm", true);
        dragInputAction = _gameplayInputActionMap.FindAction("Drag", true);

        if (!_gameplayInputActionMap.enabled)
            _gameplayInputActionMap.Enable();
    }

    public PlayerBunkerState GetCurrentState()
    {
        return _currentState;
    }

    public void ChangeState(PlayerBunkerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState?.Enter();
    }

    public void EnterNavigateMode()
    {
        ChangeState(navigateState);
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