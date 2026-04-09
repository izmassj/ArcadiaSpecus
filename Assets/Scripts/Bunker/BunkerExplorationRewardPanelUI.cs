using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BunkerExplorationRewardPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Button _continueButton;

    [Header("Animation")]
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private Ease _fadeEase = Ease.OutQuad;
    [SerializeField] private float _autoCloseDelayWithoutButton = 2f;

    private Tween _fadeTween;
    private bool _continuePressed;

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_continueButton != null)
            _continueButton.onClick.AddListener(HandleContinuePressed);

        SetVisibleImmediate(false);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();

        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(HandleContinuePressed);
    }

    public void SetVisibleImmediate(bool visible)
    {
        gameObject.SetActive(true);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        if (_continueButton != null)
            _continueButton.interactable = visible;

        if (!visible)
            gameObject.SetActive(false);
    }

    public IEnumerator ShowRoutine(BunkerPendingRewardData rewardData)
    {
        if (!rewardData.HasAnyOutcome())
            yield break;

        ApplyRewardData(rewardData);
        _continuePressed = false;
        gameObject.SetActive(true);

        if (_canvasGroup == null)
            yield break;

        _fadeTween?.Kill();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        _fadeTween = _canvasGroup
            .DOFade(1f, _fadeDuration)
            .SetEase(_fadeEase)
            .SetUpdate(true);

        yield return _fadeTween.WaitForCompletion();

        _canvasGroup.interactable = true;
        if (_continueButton != null)
            _continueButton.interactable = true;

        if (_continueButton != null)
        {
            while (!_continuePressed)
                yield return null;
        }
        else
        {
            yield return new WaitForSecondsRealtime(_autoCloseDelayWithoutButton);
        }

        yield return HideRoutine();
    }

    public IEnumerator HideRoutine()
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        _fadeTween?.Kill();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _fadeTween = _canvasGroup
            .DOFade(0f, _fadeDuration)
            .SetEase(_fadeEase)
            .SetUpdate(true);

        yield return _fadeTween.WaitForCompletion();
        gameObject.SetActive(false);
    }

    private void ApplyRewardData(BunkerPendingRewardData rewardData)
    {
        if (_titleText != null)
            _titleText.text = "Recompensas de exploración";

        if (_bodyText != null)
            _bodyText.text = BuildBody(rewardData);
    }

    private string BuildBody(BunkerPendingRewardData rewardData)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (rewardData.scrap > 0)
            builder.AppendLine($"Chatarra: +{rewardData.scrap}");

        if (rewardData.electricity > 0)
            builder.AppendLine($"Electricidad: +{rewardData.electricity}");

        if (rewardData.water > 0)
            builder.AppendLine($"Agua: +{rewardData.water}");

        if (rewardData.food > 0)
            builder.AppendLine($"Comida: +{rewardData.food}");

        if (rewardData.joinedInhabitants > 0)
            builder.AppendLine($"Habitantes nuevos: +{rewardData.joinedInhabitants}");

        return builder.ToString().TrimEnd();
    }

    private void HandleContinuePressed()
    {
        _continuePressed = true;
    }
}
