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

        EnsureDefaultConfigurations();
    }

    private void EnsureDefaultConfigurations()
    {
        if (!_stateConfigMap.ContainsKey(PlayerManager.PlayerStates.NONE))
        {
            _stateConfigMap[PlayerManager.PlayerStates.NONE] = new UIStateConfiguration
            {
                state = PlayerManager.PlayerStates.NONE,
                activeElements = new[] { roomBuildingEnter },
                inactiveElements = new[] { roomBuildingButtons }
            };
        }

        // ✅ CAMBIO: usamos ROOMBUILDING (el estado real del enum)
        if (!_stateConfigMap.ContainsKey(PlayerManager.PlayerStates.ROOMBUILDING))
        {
            _stateConfigMap[PlayerManager.PlayerStates.ROOMBUILDING] = new UIStateConfiguration
            {
                state = PlayerManager.PlayerStates.ROOMBUILDING,
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
                PlayerManager.Instance.SetCurrentPlayerState(PlayerManager.PlayerStates.ROOMBUILDING);
                break;
            case PlayerManager.PlayerStates.ROOMBUILDING:
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
        DeactivateAllUIElements();

        if (_stateConfigMap.TryGetValue(state, out var config))
        {
            SetElementsActive(config.activeElements, true);
            SetElementsActive(config.inactiveElements, false);
        }
    }

    private void DeactivateAllUIElements()
    {
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

    public void SetUpMainUI()
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

    private bool IsValidStateTransition(PlayerManager.PlayerStates current, PlayerManager.PlayerStates next)
    {
        return true;
    }
}
