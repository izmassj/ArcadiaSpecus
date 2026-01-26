using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVisibilityManager : MonoBehaviour
{
    [System.Serializable]
    public class UIStateConfiguration
    {
        public PlayerManager.PlayerStates state;
        public GameObject[] activeElements;
        public GameObject[] inactiveElements;
    }

    [Header("UI References")]
    [SerializeField] private Button _roomBuildingEnterButton;
    [SerializeField] private Button _roomBuildingExitButton;

    [Header("UI Elements")]
    [SerializeField] private GameObject _roomBuildingButtons;
    [SerializeField] private GameObject _roomBuildingEnter;

    [Header("State Configurations")]
    [SerializeField] private UIStateConfiguration[] _stateConfigurations;

    private Dictionary<PlayerManager.PlayerStates, UIStateConfiguration> _stateConfigMap;
    private readonly GameObject[] _allUIElements = new GameObject[2];

    private void Awake()
    {
        InitializeAllUIElementsArray();
        InitializeStateConfiguration();
        SetupButtonListeners();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeAllUIElementsArray()
    {
        _allUIElements[0] = _roomBuildingButtons;
        _allUIElements[1] = _roomBuildingEnter;
    }

    private void InitializeStateConfiguration()
    {
        _stateConfigMap = new Dictionary<PlayerManager.PlayerStates, UIStateConfiguration>();

        foreach (var config in _stateConfigurations)
        {
            _stateConfigMap[config.state] = config;
        }

        EnsureDefaultConfigurations();
    }

    private void EnsureDefaultConfigurations()
    {
        AddDefaultConfigurationIfMissing(PlayerManager.PlayerStates.NONE,
            activeElements: new[] { _roomBuildingEnter },
            inactiveElements: new[] { _roomBuildingButtons });

        AddDefaultConfigurationIfMissing(PlayerManager.PlayerStates.ROOMBUILDING,
            activeElements: new[] { _roomBuildingButtons },
            inactiveElements: new[] { _roomBuildingEnter });
    }

    private void AddDefaultConfigurationIfMissing(PlayerManager.PlayerStates state,
        GameObject[] activeElements, GameObject[] inactiveElements)
    {
        if (_stateConfigMap.ContainsKey(state)) return;

        _stateConfigMap[state] = new UIStateConfiguration
        {
            state = state,
            activeElements = activeElements,
            inactiveElements = inactiveElements
        };
    }

    private void SetupButtonListeners()
    {
        _roomBuildingEnterButton.onClick.AddListener(OnRoomBuildingButtonClicked);
    }

    private void SubscribeToEvents()
    {
        PlayerManager.OnPlayerStateChanged += OnPlayerStateChanged;
    }

    private void UnsubscribeFromEvents()
    {
        PlayerManager.OnPlayerStateChanged -= OnPlayerStateChanged;
    }

    private void OnRoomBuildingButtonClicked()
    {
        var currentState = PlayerManager.Instance.GetCurrentPlayerState();
        var newState = currentState switch
        {
            PlayerManager.PlayerStates.NONE => PlayerManager.PlayerStates.ROOMBUILDING,
            PlayerManager.PlayerStates.ROOMBUILDING => PlayerManager.PlayerStates.NONE,
            _ => currentState
        };

        PlayerManager.Instance.SetCurrentPlayerState(newState);
    }

    private void OnPlayerStateChanged(PlayerManager.PlayerStates newState)
    {
        UpdateUIVisibility(newState);
    }

    private void UpdateUIVisibility(PlayerManager.PlayerStates state)
    {
        DeactivateAllUIElements();

        if (_stateConfigMap.TryGetValue(state, out var config))
        {
            SetElementsActive(config.activeElements, true);
            SetElementsActive(config.inactiveElements, false);
        }
    }

    private void DeactivateAllUIElements()
    {
        SetElementsActive(_allUIElements, false);
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

    public void SetupMainUI()
    {
        UpdateUIVisibility(PlayerManager.PlayerStates.NONE);
    }

    public void ChangeToState(PlayerManager.PlayerStates newState)
    {
        if (IsValidStateTransition(PlayerManager.Instance.GetCurrentPlayerState(), newState))
        {
            PlayerManager.Instance.SetCurrentPlayerState(newState);
        }
    }

    private bool IsValidStateTransition(PlayerManager.PlayerStates current,
        PlayerManager.PlayerStates next)
    {
        // Add transition validation logic here if needed
        return true;
    }
}