using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private PlayerInput playerInput;

    private InputActionMap _gameplayMap;
    private InputAction _clickAction;

    // Singleton
    public static PlayerManager Instance { get; private set; }

    // Events for state changes
    public static event Action<PlayerStates> OnPlayerStateChanged;

    public enum PlayerStates
    {
        NONE,
        UI, UI_MENU, UI_SETTINGS, UI_ROBOT, UI_ROOMBUILDING,
        BUNKER, BUNKER_DRAGGING, BUNKER_ROOMBUILDING,
        ROBOT
    }

    private PlayerStates _currentPlayerState = PlayerStates.NONE;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _currentPlayerState = PlayerStates.NONE;

        _gameplayMap = playerInput.actions.FindActionMap("Gameplay");
        _clickAction = _gameplayMap.FindAction("Drag");
    }

    private void OnEnable()
    {
        _clickAction.performed += ToDragging;
        _clickAction.canceled += ToDraggingRelease;
    }

    private void OnDisable()
    {
        _clickAction.performed -= ToDragging;
        _clickAction.canceled -= ToDraggingRelease;
    }

    void Update()
    {
        switch (_currentPlayerState)
        {
            case PlayerStates.NONE:
                break;
            case PlayerStates.BUNKER_DRAGGING:
                break;
        }
    }

    // Transition functions
    private void ToDragging(InputAction.CallbackContext ctx)
    {
        SetCurrentPlayerState(PlayerStates.BUNKER_DRAGGING);
    }

    private void ToDraggingRelease(InputAction.CallbackContext ctx)
    {
        if (ctx.ReadValue<float>() == 0 && _currentPlayerState == PlayerStates.BUNKER_DRAGGING)
        {
            SetCurrentPlayerState(PlayerStates.NONE);
        }
    }

    // Getters
    public PlayerStates GetCurrentPlayerState() => _currentPlayerState;

    // Setter with event notification
    public void SetCurrentPlayerState(PlayerStates state)
    {
        if (_currentPlayerState == state) return;

        var previousState = _currentPlayerState;
        _currentPlayerState = state;
        OnPlayerStateChanged?.Invoke(state);
    }
}