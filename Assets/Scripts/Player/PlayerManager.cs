using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestor de estado de interacción del jugador (dragging/building/etc).
/// Mantiene la API usada por otros scripts (UIVisibilityManager, ClickableObjectManager...).
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string _gameplayActionMapName = "Gameplay";
    [SerializeField] private string _dragActionName = "Drag";

    private InputActionMap _gameplayMap;
    private InputAction _dragAction;

    public static PlayerManager Instance { get; private set; }
    public static event Action<PlayerStates> OnPlayerStateChanged;

    public enum PlayerStates
    {
        NONE,
        DRAGGING,
        ROOMBUILDING,
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
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerInput>();

        CacheInputActions();

        // Base de remapping: cargamos overrides guardados si existen.
        InputBindingSaveManager.LoadBindingOverrides(playerInput);

        _currentPlayerState = PlayerStates.NONE;
    }

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void CacheInputActions()
    {
        _gameplayMap = null;
        _dragAction = null;

        if (playerInput == null || playerInput.actions == null)
            return;

        _gameplayMap = playerInput.actions.FindActionMap(_gameplayActionMapName, throwIfNotFound: false);

        if (_gameplayMap != null)
            _dragAction = _gameplayMap.FindAction(_dragActionName, throwIfNotFound: false);
        else
            _dragAction = playerInput.actions.FindAction(_dragActionName, throwIfNotFound: false);
    }

    private void SubscribeInput()
    {
        if (_dragAction == null)
        {
            CacheInputActions();
        }

        if (_dragAction != null)
        {
            _dragAction.performed -= ToDragging;
            _dragAction.canceled -= ToDraggingRelease;

            _dragAction.performed += ToDragging;
            _dragAction.canceled += ToDraggingRelease;
        }
    }

    private void UnsubscribeInput()
    {
        if (_dragAction != null)
        {
            _dragAction.performed -= ToDragging;
            _dragAction.canceled -= ToDraggingRelease;
        }
    }

    private void ToDragging(InputAction.CallbackContext ctx)
    {
        SetCurrentPlayerState(PlayerStates.DRAGGING);
    }

    private void ToDraggingRelease(InputAction.CallbackContext ctx)
    {
        // Para botones típicos del Input System esto vuelve a 0 en cancel.
        if (_currentPlayerState == PlayerStates.DRAGGING)
            SetCurrentPlayerState(PlayerStates.NONE);
    }

    public PlayerStates GetCurrentPlayerState()
    {
        return _currentPlayerState;
    }

    public void SetCurrentPlayerState(PlayerStates state)
    {
        if (_currentPlayerState == state)
            return;

        _currentPlayerState = state;
        OnPlayerStateChanged?.Invoke(state);
    }

    public PlayerInput GetPlayerInput()
    {
        return playerInput;
    }

    public void SaveBindingOverrides()
    {
        InputBindingSaveManager.SaveBindingOverrides(playerInput);
    }

    public void ResetBindingOverrides()
    {
        InputBindingSaveManager.ResetBindingOverrides(playerInput);
        CacheInputActions();
    }
}