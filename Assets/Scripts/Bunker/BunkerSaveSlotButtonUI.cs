using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BunkerSaveSlotButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _detailsText;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _deleteButton;

    private Action _onLoad;
    private Action _onDelete;

    private void Awake()
    {
        if (_loadButton != null)
            _loadButton.onClick.AddListener(HandleLoadPressed);

        if (_deleteButton != null)
            _deleteButton.onClick.AddListener(HandleDeletePressed);
    }

    private void OnDestroy()
    {
        if (_loadButton != null)
            _loadButton.onClick.RemoveListener(HandleLoadPressed);

        if (_deleteButton != null)
            _deleteButton.onClick.RemoveListener(HandleDeletePressed);
    }

    public void Setup(BunkerSaveMetadata metadata, Action onLoad, Action onDelete)
    {
        _onLoad = onLoad;
        _onDelete = onDelete;

        if (_titleText != null)
            _titleText.text = string.IsNullOrWhiteSpace(metadata.displayName) ? "Partida" : metadata.displayName;

        if (_detailsText != null)
        {
            DateTime lastWrite = metadata.updatedUtcTicks > 0
                ? new DateTime(metadata.updatedUtcTicks, DateTimeKind.Utc).ToLocalTime()
                : DateTime.MinValue;

            string modeText = ((BunkerGameMode)metadata.gameMode).ToString();
            string dateText = lastWrite != DateTime.MinValue ? lastWrite.ToString("dd/MM/yyyy HH:mm") : "-";
            _detailsText.text = $"Modo: {modeText} | D\u00eda: {metadata.previewDay:00} | NPCs: {metadata.previewNpcCount} | Última: {dateText}";
        }
    }

    private void HandleLoadPressed()
    {
        _onLoad?.Invoke();
    }

    private void HandleDeletePressed()
    {
        _onDelete?.Invoke();
    }
}
