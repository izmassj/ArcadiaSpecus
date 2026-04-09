using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuBunkerSavePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_InputField _saveNameInput;
    [SerializeField] private TMP_Text _statusText;

    [Header("Create Buttons")]
    [SerializeField] private Button _multitudButton;
    [SerializeField] private Button _prosperoButton;
    [SerializeField] private Button _createButton;
    [SerializeField] private Button _closeButton;

    [Header("Save List")]
    [SerializeField] private Transform _saveListRoot;
    [SerializeField] private BunkerSaveSlotButtonUI _saveListItemPrefab;

    [Header("Scene Load")]
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private string _bunkerSceneName;

    [Header("Text")]
    [SerializeField] private string _populusText;
    [SerializeField] private string _abundantText;


    private readonly List<BunkerSaveSlotButtonUI> _spawnedItems = new List<BunkerSaveSlotButtonUI>();
    private BunkerGameMode _selectedMode = BunkerGameMode.None;
    private bool _isLoading;

    private void Awake()
    {
        if (_multitudButton != null)
            _multitudButton.onClick.AddListener(() => SelectMode(BunkerGameMode.Multitud));

        if (_prosperoButton != null)
            _prosperoButton.onClick.AddListener(() => SelectMode(BunkerGameMode.Prospero));

        if (_createButton != null)
            _createButton.onClick.AddListener(HandleCreatePressed);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(ClosePanel);

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_multitudButton != null)
            _multitudButton.onClick.RemoveAllListeners();

        if (_prosperoButton != null)
            _prosperoButton.onClick.RemoveAllListeners();

        if (_createButton != null)
            _createButton.onClick.RemoveAllListeners();

        if (_closeButton != null)
            _closeButton.onClick.RemoveAllListeners();
    }

    public void OpenPanel()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        RefreshSaveList();
        SetStatus("Selecciona un modo o carga una partida existente.");
    }

    public void ClosePanel()
    {
        if (_isLoading)
            return;

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    public void RefreshSaveList()
    {
        ClearSaveList();
        List<BunkerSaveMetadata> metadataList = BunkerSaveSystem.LoadAllMetadata();

        if (_saveListRoot == null || _saveListItemPrefab == null)
            return;

        for (int i = 0; i < metadataList.Count; i++)
        {
            BunkerSaveMetadata metadata = metadataList[i];
            BunkerSaveSlotButtonUI item = Instantiate(_saveListItemPrefab, _saveListRoot);
            item.Setup(
                metadata,
                () => HandleLoadPressed(metadata.slotId),
                () => HandleDeletePressed(metadata.slotId));
            _spawnedItems.Add(item);
        }
    }

    private void SelectMode(BunkerGameMode mode)
    {
        _selectedMode = mode;

        switch (mode)
        {
            case BunkerGameMode.Multitud:
                SetStatus(_populusText);
                break;
            case BunkerGameMode.Prospero:
                SetStatus(_abundantText);
                break;
        }
        
    }

    private void HandleCreatePressed()
    {
        if (_isLoading)
            return;

        if (_selectedMode == BunkerGameMode.None)
        {
            SetStatus("Antes de crear la partida, selecciona Multitud o Prospero.");
            return;
        }

        string saveName = _saveNameInput != null ? _saveNameInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(saveName))
        {
            SetStatus("Escribe un nombre para la partida.");
            return;
        }

        BunkerSaveFileData newSlot = BunkerSaveSystem.CreateNewSlot(saveName, _selectedMode);
        BunkerSessionLaunch.BeginCreate(newSlot.metadata.slotId);
        StartCoroutine(LoadBunkerSceneRoutine());
    }

    private void HandleLoadPressed(string slotId)
    {
        if (_isLoading)
            return;

        BunkerSessionLaunch.BeginLoad(slotId);
        StartCoroutine(LoadBunkerSceneRoutine());
    }

    private void HandleDeletePressed(string slotId)
    {
        if (_isLoading)
            return;

        BunkerSaveSystem.Delete(slotId);
        RefreshSaveList();
        SetStatus("Partida eliminada.");
    }

    private IEnumerator LoadBunkerSceneRoutine()
    {
        _isLoading = true;

        if (_transition != null)
            yield return _transition.PlayCoverRoutine();

        SceneManager.LoadScene(_bunkerSceneName);
    }

    private void ClearSaveList()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
    }
}
