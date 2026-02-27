using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla visibilidad de bloques UI según el estado del PlayerManager.
/// Refactorizado para ser tolerante a nulls y a escenas donde no existe PlayerManager.
/// </summary>
public class UIVisibilityManager : MonoBehaviour
{
    [System.Serializable]
    public class UIStateConfiguration
    {
        public PlayerManager.PlayerStates state;
        public GameObject[] activeElements;
        public GameObject[] inactiveElements;
    }

    [Header("Botones (opcionales)")]
    [SerializeField] private Button _roomBuildingEnterButton;
    [SerializeField] private Button _roomBuildingExitButton;

    [Header("Bloques UI (opcionales)")]
    [SerializeField] private GameObject _roomBuildingButtons;
    [SerializeField] private GameObject _roomBuildingEnter;

    [Header("Configuración por estado")]
    [SerializeField] private UIStateConfiguration[] _stateConfigurations;
    [SerializeField] private bool _applyDefaultStateOnStart = true;

    private readonly Dictionary<PlayerManager.PlayerStates, UIStateConfiguration> _stateConfigMap = new Dictionary<PlayerManager.PlayerStates, UIStateConfiguration>();
    private readonly List<GameObject> _allKnownElements = new List<GameObject>(8);
    private bool _isSubscribed;

    private void Awake()
    {
        RebuildConfigurationCache();
        SetupButtonListeners();
    }

    private void OnEnable()
    {
        SubscribeToEvents();

        if (_applyDefaultStateOnStart)
        {
            RefreshFromCurrentState();
        }
    }

    private void Start()
    {
        // Segunda pasada por si PlayerManager se inicializa un poco más tarde.
        RefreshFromCurrentState();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
        UnsubscribeFromEvents();
    }

    private void RebuildConfigurationCache()
    {
        _stateConfigMap.Clear();
        _allKnownElements.Clear();

        if (_stateConfigurations != null)
        {
            for (int i = 0; i < _stateConfigurations.Length; i++)
            {
                UIStateConfiguration _config = _stateConfigurations[i];
                if (_config == null)
                {
                    continue;
                }

                _stateConfigMap[_config.state] = _config;
                RegisterElements(_config.activeElements);
                RegisterElements(_config.inactiveElements);
            }
        }

        EnsureDefaultConfigurations();
    }

    private void EnsureDefaultConfigurations()
    {
        AddDefaultConfigurationIfMissing(
            PlayerManager.PlayerStates.NONE,
            new[] { _roomBuildingEnter },
            new[] { _roomBuildingButtons });

        AddDefaultConfigurationIfMissing(
            PlayerManager.PlayerStates.ROOMBUILDING,
            new[] { _roomBuildingButtons },
            new[] { _roomBuildingEnter });
    }

    private void AddDefaultConfigurationIfMissing(
        PlayerManager.PlayerStates _state,
        GameObject[] _activeElements,
        GameObject[] _inactiveElements)
    {
        if (_stateConfigMap.ContainsKey(_state))
        {
            return;
        }

        UIStateConfiguration _config = new UIStateConfiguration
        {
            state = _state,
            activeElements = _activeElements,
            inactiveElements = _inactiveElements
        };

        _stateConfigMap[_state] = _config;
        RegisterElements(_activeElements);
        RegisterElements(_inactiveElements);
    }

    private void RegisterElements(GameObject[] _elements)
    {
        if (_elements == null)
        {
            return;
        }

        for (int i = 0; i < _elements.Length; i++)
        {
            GameObject _go = _elements[i];
            if (_go == null)
            {
                continue;
            }

            if (!_allKnownElements.Contains(_go))
            {
                _allKnownElements.Add(_go);
            }
        }
    }

    private void SetupButtonListeners()
    {
        if (_roomBuildingEnterButton != null)
        {
            _roomBuildingEnterButton.onClick.RemoveListener(OnRoomBuildingEnterClicked);
            _roomBuildingEnterButton.onClick.AddListener(OnRoomBuildingEnterClicked);
        }

        if (_roomBuildingExitButton != null)
        {
            _roomBuildingExitButton.onClick.RemoveListener(OnRoomBuildingExitClicked);
            _roomBuildingExitButton.onClick.AddListener(OnRoomBuildingExitClicked);
        }
    }

    private void RemoveButtonListeners()
    {
        if (_roomBuildingEnterButton != null)
        {
            _roomBuildingEnterButton.onClick.RemoveListener(OnRoomBuildingEnterClicked);
        }

        if (_roomBuildingExitButton != null)
        {
            _roomBuildingExitButton.onClick.RemoveListener(OnRoomBuildingExitClicked);
        }
    }

    private void SubscribeToEvents()
    {
        if (_isSubscribed)
        {
            return;
        }

        PlayerManager.OnPlayerStateChanged += OnPlayerStateChanged;
        _isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        PlayerManager.OnPlayerStateChanged -= OnPlayerStateChanged;
        _isSubscribed = false;
    }

    private void OnRoomBuildingEnterClicked()
    {
        ChangeToState(PlayerManager.PlayerStates.ROOMBUILDING);
    }

    private void OnRoomBuildingExitClicked()
    {
        ChangeToState(PlayerManager.PlayerStates.NONE);
    }

    private void OnPlayerStateChanged(PlayerManager.PlayerStates _newState)
    {
        UpdateUIVisibility(_newState);
    }

    public void RefreshFromCurrentState()
    {
        RebuildConfigurationCache();

        if (PlayerManager.Instance == null)
        {
            // Si no hay PlayerManager en esta escena, dejamos la UI en estado base.
            UpdateUIVisibility(PlayerManager.PlayerStates.NONE);
            return;
        }

        UpdateUIVisibility(PlayerManager.Instance.GetCurrentPlayerState());
    }

    private void UpdateUIVisibility(PlayerManager.PlayerStates _state)
    {
        DeactivateAllKnownElements();

        if (_stateConfigMap.TryGetValue(_state, out UIStateConfiguration _config))
        {
            SetElementsActive(_config.activeElements, true);
            SetElementsActive(_config.inactiveElements, false);
        }
    }

    private void DeactivateAllKnownElements()
    {
        for (int i = 0; i < _allKnownElements.Count; i++)
        {
            if (_allKnownElements[i] != null)
            {
                _allKnownElements[i].SetActive(false);
            }
        }
    }

    private void SetElementsActive(GameObject[] _elements, bool _active)
    {
        if (_elements == null)
        {
            return;
        }

        for (int i = 0; i < _elements.Length; i++)
        {
            if (_elements[i] != null)
            {
                _elements[i].SetActive(_active);
            }
        }
    }

    public void SetupMainUI()
    {
        RefreshFromCurrentState();
    }

    public void ChangeToState(PlayerManager.PlayerStates _newState)
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogWarning("[UIVisibilityManager] No existe PlayerManager en la escena para cambiar estado.");
            return;
        }

        if (!IsValidStateTransition(PlayerManager.Instance.GetCurrentPlayerState(), _newState))
        {
            return;
        }

        PlayerManager.Instance.SetCurrentPlayerState(_newState);
    }

    private bool IsValidStateTransition(PlayerManager.PlayerStates _current, PlayerManager.PlayerStates _next)
    {
        // De momento se permite todo para no romper vuestra lógica existente.
        return true;
    }
}
