using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RobotLevelHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelDeathManager _levelDeathManager;
    [SerializeField] private RobotHookController _hookController;
    [SerializeField] private RobotShockController _shockController;

    [Header("Objective")]
    [SerializeField] private CanvasGroup _objectivePanelCanvasGroup;
    [SerializeField] private RectTransform _objectivePanelRoot;
    [SerializeField] private TextMeshProUGUI _objectiveText;
    [SerializeField][TextArea] private string _levelObjective = "Encuentra la salida.";

    [Header("Lives")]
    [SerializeField] private RectTransform[] _heartIcons;
    [SerializeField] private float _heartAnimDuration = 0.18f;
    [SerializeField] private Ease _heartLostEase = Ease.InBack;
    [SerializeField] private Ease _heartGainEase = Ease.OutBack;

    [Header("Hook Cooldown")]
    [SerializeField] private Image _hookCooldownRadial;
    [SerializeField] private CanvasGroup _hookAbilityCanvasGroup;
    [SerializeField] private RectTransform _hookAbilityRoot;

    [Header("Shock Cooldown")]
    [SerializeField] private Image _shockCooldownRadial;
    [SerializeField] private CanvasGroup _shockAbilityCanvasGroup;
    [SerializeField] private RectTransform _shockAbilityRoot;

    [Header("Cooldown Settings")]
    [SerializeField] private bool _radialShowsRemainingCooldown = true;
    [SerializeField] private bool _hideCooldownTextWhenReady = true;
    [SerializeField] private float _readyAlpha = 1f;
    [SerializeField] private float _cooldownAlpha = 0.55f;
    [SerializeField] private float _readyPunchScale = 0.18f;

    [Header("Intro Animation")]
    [SerializeField] private bool _animateObjectiveOnStart = true;
    [SerializeField] private float _objectiveFadeDuration = 0.25f;
    [SerializeField] private float _objectiveScaleDuration = 0.25f;
    [SerializeField] private Ease _objectiveEase = Ease.OutBack;

    private bool _wasHookOnCooldown;
    private bool _wasShockOnCooldown;

    private void Awake()
    {
        ResolveReferences();
        RefreshObjectiveInstant();
        RefreshLivesImmediate();
        RefreshCooldowns();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (_levelDeathManager != null)
            _levelDeathManager.LivesChanged += OnLivesChanged;

        RefreshLivesImmediate();

        if (_animateObjectiveOnStart)
            AnimateObjectivePanel();
    }

    private void OnDisable()
    {
        if (_levelDeathManager != null)
            _levelDeathManager.LivesChanged -= OnLivesChanged;
    }

    private void Update()
    {
        RefreshCooldowns();
    }

    private void ResolveReferences()
    {
        if (_levelDeathManager == null)
            _levelDeathManager = FindObjectOfType<LevelDeathManager>();

        if (_hookController == null)
            _hookController = FindObjectOfType<RobotHookController>();

        if (_shockController == null)
            _shockController = FindObjectOfType<RobotShockController>();
    }

    public void SetObjective(string objective)
    {
        _levelObjective = objective;
        RefreshObjectiveInstant();
        AnimateObjectivePanel();
    }

    private void RefreshObjectiveInstant()
    {
        if (_objectiveText != null)
            _objectiveText.text = _levelObjective;
    }

    private void AnimateObjectivePanel()
    {
        if (_objectivePanelCanvasGroup != null)
        {
            _objectivePanelCanvasGroup.DOKill();
            _objectivePanelCanvasGroup.alpha = 0f;
            _objectivePanelCanvasGroup
                .DOFade(1f, _objectiveFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        if (_objectivePanelRoot != null)
        {
            _objectivePanelRoot.DOKill();
            _objectivePanelRoot.localScale = Vector3.one * 0.85f;
            _objectivePanelRoot
                .DOScale(Vector3.one, _objectiveScaleDuration)
                .SetEase(_objectiveEase)
                .SetUpdate(true);
        }
    }

    private void OnLivesChanged(int currentLives, int maxLives)
    {
        RefreshLives(currentLives, true);
    }

    private void RefreshLivesImmediate()
    {
        if (_levelDeathManager == null)
            return;

        RefreshLives(_levelDeathManager.CurrentLives, false);
    }

    private void RefreshLives(int currentLives, bool animate)
    {
        if (_heartIcons == null)
            return;

        for (int i = 0; i < _heartIcons.Length; i++)
        {
            RectTransform heart = _heartIcons[i];

            if (heart == null)
                continue;

            bool visible = i < currentLives;

            heart.DOKill();

            if (!animate)
            {
                heart.localScale = visible ? Vector3.one : Vector3.zero;
                continue;
            }

            Ease ease = visible ? _heartGainEase : _heartLostEase;
            Vector3 targetScale = visible ? Vector3.one : Vector3.zero;

            heart
                .DOScale(targetScale, _heartAnimDuration)
                .SetEase(ease)
                .SetUpdate(true);
        }
    }

    private void RefreshCooldowns()
    {
        RefreshHookCooldown();
        RefreshShockCooldown();
    }

    private void RefreshHookCooldown()
    {
        if (_hookController == null)
        {
            SetCooldownVisuals(
                _hookCooldownRadial,
                _hookAbilityCanvasGroup,
                0f,
                0f,
                false
            );

            return;
        }

        bool isOnCooldown = _hookController.IsHookOnCooldown;
        float remaining01 = _hookController.HookCooldown01;
        float remainingSeconds = _hookController.HookCooldownSecondsRemaining;

        SetCooldownVisuals(
            _hookCooldownRadial,
            _hookAbilityCanvasGroup,
            remaining01,
            remainingSeconds,
            isOnCooldown
        );

        if (_wasHookOnCooldown && !isOnCooldown)
            PunchAbility(_hookAbilityRoot);

        _wasHookOnCooldown = isOnCooldown;
    }

    private void RefreshShockCooldown()
    {
        if (_shockController == null)
        {
            SetCooldownVisuals(
                _shockCooldownRadial,
                _shockAbilityCanvasGroup,
                0f,
                0f,
                false
            );

            return;
        }

        bool isOnCooldown = _shockController.IsOnCooldown;
        float remaining01 = _shockController.Cooldown01;
        float remainingSeconds = _shockController.CooldownSecondsRemaining;

        SetCooldownVisuals(
            _shockCooldownRadial,
            _shockAbilityCanvasGroup,
            remaining01,
            remainingSeconds,
            isOnCooldown
        );

        if (_wasShockOnCooldown && !isOnCooldown)
            PunchAbility(_shockAbilityRoot);

        _wasShockOnCooldown = isOnCooldown;
    }

    private void SetCooldownVisuals(Image radial, CanvasGroup group, float remaining01, float remainingSeconds, bool isOnCooldown)
    {
        if (radial != null)
        {
            radial.fillAmount = _radialShowsRemainingCooldown ? remaining01 : 1f - remaining01;
            radial.enabled = isOnCooldown || !_hideCooldownTextWhenReady;
        }

        if (group != null)
            group.alpha = isOnCooldown ? _cooldownAlpha : _readyAlpha;
    }

    private void PunchAbility(RectTransform abilityRoot)
    {
        if (abilityRoot == null)
            return;

        abilityRoot.DOKill();
        abilityRoot.localScale = Vector3.one;
        abilityRoot
            .DOPunchScale(Vector3.one * _readyPunchScale, 0.22f, 8, 0.75f)
            .SetUpdate(true);
    }
}