using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    // input

    [SerializeField] private PlayerInput playerInput;

    private InputActionMap _gameplayMap;
    private InputAction _clickAction;


    // Singleton

    public static PlayerManager Instance;

    // enums

    public enum PlayerStates
    {
        NONE, UI_MENU, BUNKER_DRAGGING, BUNKER_INTERACT,
        ROBOT
    }

    private PlayerStates currentPlayerState;


    private void Awake()
    {
        currentPlayerState = PlayerStates.NONE;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

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

    // States


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentPlayerState)
        {
            case PlayerStates.NONE:
                break;
            case PlayerStates.BUNKER_DRAGGING:
                break;
        }
    }

    // transition functions

    private void ToDragging(InputAction.CallbackContext ctx)
    {
        currentPlayerState = PlayerStates.BUNKER_DRAGGING;
    }

    private void ToDraggingRelease(InputAction.CallbackContext ctx)
    {
        if (ctx.ReadValue<float>() == 0 && currentPlayerState == PlayerStates.BUNKER_DRAGGING)
        {
            currentPlayerState = PlayerStates.NONE;
        }
    }

    // Getters

    public PlayerStates GetCurrentPlayerState()
    {
        return currentPlayerState;
    }

}
