using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVisibilityBunkerManager : MonoBehaviour
{
    [System.Serializable]
    public class UIStateConfiguration
    {
        public PlayerManager.PlayerStates state;
        public GameObject[] activeElements;
        public GameObject[] inactiveElements;
    }

    [Header("UI References")]
    [SerializeField] private Button roomBuildingEnterButton;
    [SerializeField] private Button roomBuildingExitButton;

    [Header("UI Elements")]
    [SerializeField] private GameObject roomBuildingButtons;
    [SerializeField] private GameObject roomBuildingEnter;

    [Header("State Configurations")]
    [SerializeField] private UIStateConfiguration[] stateConfigurations;

    private Dictionary<PlayerManager.PlayerStates, UIStateConfiguration> _stateConfigMap;

    private void Awake()
    {
        InitializeStateConfiguration();
        roomBuildingEnterButton.onClick.AddListener(OnRoomBuildingButtonClicked);

        // Subscribe to state changes
        PlayerManager.OnPlayerStateChanged += OnPlayerStateChanged;
    }

    private void OnDestroy()
    {
        PlayerManager.OnPlayerStateChanged -= OnPlayerStateChanged;
    }

    private void InitializeStateConfiguration()
    {
        _stateConfigMap = new Dictionary<PlayerManager.PlayerStates, UIStateConfiguration>();

        foreach (var config in stateConfigurations)
        {
            _stateConfigMap[config.state] = config;
        }

        // Set up default configurations if not in inspector
        EnsureDefaultConfigurations();
    }

    private void EnsureDefaultConfigurations()
    {
        // NONE state - only show basic UI
        if (!_stateConfigMap.ContainsKey(PlayerManager.PlayerStates.NONE))
        {
            _stateConfigMap[PlayerManager.PlayerStates.NONE] = new UIStateConfiguration
            {
                state = PlayerManager.PlayerStates.NONE,
                activeElements = new[] { roomBuildingEnter },
                inactiveElements = new[] { roomBuildingButtons }
            };
        }

        // UI_ROOMBUILDING state - show building interface
        if (!_stateConfigMap.ContainsKey(PlayerManager.PlayerStates.UI_ROOMBUILDING))
        {
            _stateConfigMap[PlayerManager.PlayerStates.UI_ROOMBUILDING] = new UIStateConfiguration
            {
                state = PlayerManager.PlayerStates.UI_ROOMBUILDING,
                activeElements = new[] { roomBuildingButtons },
                inactiveElements = new[] { roomBuildingEnter }
            };
        }
    }

    private void OnRoomBuildingButtonClicked()
    {
        var currentState = PlayerManager.Instance.GetCurrentPlayerState();

        switch (currentState)
        {
            case PlayerManager.PlayerStates.NONE:
                PlayerManager.Instance.SetCurrentPlayerState(PlayerManager.PlayerStates.UI_ROOMBUILDING);
                break;
            case PlayerManager.PlayerStates.UI_ROOMBUILDING:
                PlayerManager.Instance.SetCurrentPlayerState(PlayerManager.PlayerStates.NONE);
                break;
        }
    }

    private void OnPlayerStateChanged(PlayerManager.PlayerStates newState)
    {
        UpdateUIVisibility(newState);
    }

    private void UpdateUIVisibility(PlayerManager.PlayerStates state)
    {
        // Deactivate all UI elements first
        DeactivateAllUIElements();

        // Activate elements for current state
        if (_stateConfigMap.TryGetValue(state, out var config))
        {
            SetElementsActive(config.activeElements, true);
            SetElementsActive(config.inactiveElements, false);
        }
    }

    private void DeactivateAllUIElements()
    {
        // You can expand this list with all your UI elements
        GameObject[] allUIElements = { roomBuildingButtons, roomBuildingEnter };
        SetElementsActive(allUIElements, false);
    }

    private void SetElementsActive(GameObject[] elements, bool active)
    {
        if (elements == null) return;

        foreach (var element in elements)
        {
            if (element != null)
                element.SetActive(active);
        }
    }

    // Public method to setup initial UI state
    public void SetUpMainUI()
    {
        UpdateUIVisibility(PlayerManager.PlayerStates.NONE);
    }

    // Optional: Manual state transition with validation
    public void ChangeToState(PlayerManager.PlayerStates newState)
    {
        if (IsValidStateTransition(PlayerManager.Instance.GetCurrentPlayerState(), newState))
        {
            PlayerManager.Instance.SetCurrentPlayerState(newState);
        }
    }

    private bool IsValidStateTransition(PlayerManager.PlayerStates current, PlayerManager.PlayerStates next)
    {
        // Define your state transition rules here
        // Example: Can't go from DRAGGING directly to UI_MENU
        return true; // Implement your logic
    }
}