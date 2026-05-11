using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelDeathManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotController _player;
    [SerializeField] private CharacterController _playerCharacterController;
    [SerializeField] private RobotDeathExplosion _robotDeathExplosion;
    [SerializeField] private Transform _respawnPoint;
    [SerializeField] private RectTransform _respawnMaskRect;

    [Header("Mask")]
    [SerializeField] private Vector3 _shownMaskScale = new Vector3(24f, 24f, 24f);
    [SerializeField] private float _maskHideDuration = 0.25f;
    [SerializeField] private float _maskShowDuration = 0.25f;
    [SerializeField] private Ease _maskHideEase = Ease.InOutSine;
    [SerializeField] private Ease _maskShowEase = Ease.InOutSine;

    [Header("Death Camera")]
    [SerializeField] private float _deathCameraDuration = 2f;
    [SerializeField] private float _deathLookSharpness = 10f;
    [SerializeField] private Vector3 _lookOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Lives")]
    [SerializeField] private int _startingLives = 3;
    [SerializeField] private RectTransform[] _lifeIcons;

    [Header("Game Over")]
    [SerializeField] private string _fallbackBunkerSceneName = "REV-BUNKERSYS";

    [Header("Level Reset")]
    [SerializeField] private MonoBehaviour[] _resettableObjects;

    [Header("Debug")]
    [SerializeField] private int _currentLives;
    [SerializeField] private bool _isRespawning;

    private Transform _thirdPersonCameraTransform;
    private Transform _thirdPersonOriginalParent;
    private Vector3 _thirdPersonOriginalLocalPosition;
    private Quaternion _thirdPersonOriginalLocalRotation;

    private bool _wasFirstPersonBeforeDeath;
    private bool _detachedThirdPersonCameraThisDeath;

    private void Awake()
    {
        ResolveRuntimeReferences();

        _currentLives = _startingLives;
        RefreshLivesImmediate();
    }

    private void ResolveRuntimeReferences()
    {
        if (_player == null)
            _player = FindObjectOfType<RobotController>();

        if (_playerCharacterController == null && _player != null)
            _playerCharacterController = _player.GetComponent<CharacterController>();

        if (_robotDeathExplosion == null && _player != null)
        {
            _robotDeathExplosion = _player.GetComponent<RobotDeathExplosion>();

            if (_robotDeathExplosion == null)
                _robotDeathExplosion = _player.GetComponentInChildren<RobotDeathExplosion>(true);

            if (_robotDeathExplosion == null)
                _robotDeathExplosion = _player.gameObject.AddComponent<RobotDeathExplosion>();
        }

        if (_player != null && _player.ThirdPersonCamera != null && _thirdPersonCameraTransform == null)
        {
            _thirdPersonCameraTransform = _player.ThirdPersonCamera.transform;
            _thirdPersonOriginalParent = _thirdPersonCameraTransform.parent;
            _thirdPersonOriginalLocalPosition = _thirdPersonCameraTransform.localPosition;
            _thirdPersonOriginalLocalRotation = _thirdPersonCameraTransform.localRotation;
        }
    }

    public void KillPlayer()
    {
        if (_isRespawning)
            return;

        ResolveRuntimeReferences();

        if (_player == null)
        {
            Debug.LogWarning($"{nameof(LevelDeathManager)}: no se ha encontrado RobotController para matar al jugador.", this);
            return;
        }

        if (_respawnPoint == null)
        {
            Debug.LogWarning($"{nameof(LevelDeathManager)}: falta asignar Respawn Point.", this);
            return;
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        _isRespawning = true;
        _wasFirstPersonBeforeDeath = _player.IsFirstPerson;
        _detachedThirdPersonCameraThisDeath = false;

        _player.SetInputLocked(true, true);

        if (_robotDeathExplosion != null)
        {
            try
            {
                _robotDeathExplosion.PlayDeathExplosion();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, _robotDeathExplosion);
            }
        }

        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RegisterPlayerDeath(SceneManager.GetActiveScene().name);

        if (!_wasFirstPersonBeforeDeath)
        {
            DetachThirdPersonCamera();
            _detachedThirdPersonCameraThisDeath = true;

            float elapsed = 0f;

            while (elapsed < _deathCameraDuration)
            {
                elapsed += Time.deltaTime;
                UpdateDetachedCameraLook();
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(_deathCameraDuration);
        }

        if (_respawnMaskRect != null)
        {
            _respawnMaskRect.DOKill();
            yield return _respawnMaskRect
                .DOScale(Vector3.zero, _maskHideDuration)
                .SetEase(_maskHideEase)
                .WaitForCompletion();
        }

        bool gameOver = ConsumeLife();
        
        if (gameOver && GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RegisterGameOver(SceneManager.GetActiveScene().name);

        if (gameOver)
        {
            yield return LoadBunkerAfterGameOver();
            yield break;
        }

        _player.SetMovementLocked(true, true);
        ResetLevelState();

        if (_respawnMaskRect != null)
        {
            _respawnMaskRect.localScale = Vector3.zero;
            yield return _respawnMaskRect
                .DOScale(_shownMaskScale, _maskShowDuration)
                .SetEase(_maskShowEase)
                .WaitForCompletion();
        }

        _player.SetMovementLocked(false, true);
        _player.SetInputLocked(false, true);

        _isRespawning = false;
    }

    private IEnumerator LoadBunkerAfterGameOver()
    {
        Time.timeScale = 1f;

        string bunkerSceneName = !string.IsNullOrWhiteSpace(BunkerSessionLaunch.CurrentBunkerSceneName)
            ? BunkerSessionLaunch.CurrentBunkerSceneName
            : _fallbackBunkerSceneName;

        if (string.IsNullOrWhiteSpace(bunkerSceneName))
        {
            Debug.LogError("LevelDeathManager: no hay nombre de escena de bunker para volver.");
            _player.SetMovementLocked(false, true);
            _player.SetInputLocked(false, true);
            _isRespawning = false;
            yield break;
        }

        SceneManager.LoadScene(bunkerSceneName);
    }

    private void DetachThirdPersonCamera()
    {
        if (_thirdPersonCameraTransform == null)
            return;

        _thirdPersonCameraTransform.SetParent(null, true);
    }

    private void ReattachThirdPersonCamera()
    {
        if (_thirdPersonCameraTransform == null)
            return;

        _thirdPersonCameraTransform.SetParent(_thirdPersonOriginalParent, false);
        _thirdPersonCameraTransform.localPosition = _thirdPersonOriginalLocalPosition;
        _thirdPersonCameraTransform.localRotation = _thirdPersonOriginalLocalRotation;
    }

    private void UpdateDetachedCameraLook()
    {
        if (_thirdPersonCameraTransform == null || _player == null)
            return;

        Vector3 targetPosition = _player.transform.position + _lookOffset;
        Vector3 direction = targetPosition - _thirdPersonCameraTransform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        float t = 1f - Mathf.Exp(-_deathLookSharpness * Time.deltaTime);
        _thirdPersonCameraTransform.rotation = Quaternion.Slerp(_thirdPersonCameraTransform.rotation, targetRotation, t);
    }

    private void ResetLevelState()
    {
        for (int i = 0; i < _resettableObjects.Length; i++)
        {
            if (_resettableObjects[i] is ILevelResettable resettable)
                resettable.ResetLevelState();
        }

        if (_playerCharacterController != null)
            _playerCharacterController.enabled = false;

        _player.transform.SetPositionAndRotation(_respawnPoint.position, _respawnPoint.rotation);

        if (_robotDeathExplosion != null)
        {
            try
            {
                _robotDeathExplosion.RestoreAliveModel();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, _robotDeathExplosion);
            }
        }

        if (_playerCharacterController != null)
            _playerCharacterController.enabled = true;

        if (_detachedThirdPersonCameraThisDeath)
        {
            ReattachThirdPersonCamera();
            _detachedThirdPersonCameraThisDeath = false;
        }

        if (_player != null)
            _player.ForcePerspective(_wasFirstPersonBeforeDeath, true);
    }

    private bool ConsumeLife()
    {
        if (_currentLives <= 0)
            return true;

        _currentLives--;

        if (_lifeIcons != null && _currentLives >= 0 && _currentLives < _lifeIcons.Length && _lifeIcons[_currentLives] != null)
        {
            _lifeIcons[_currentLives].DOKill();
            _lifeIcons[_currentLives].DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        }

        return _currentLives <= 0;
    }

    private void RefreshLivesImmediate()
    {
        if (_lifeIcons == null)
            return;

        for (int i = 0; i < _lifeIcons.Length; i++)
        {
            if (_lifeIcons[i] == null)
                continue;

            _lifeIcons[i].localScale = i < _currentLives ? Vector3.one : Vector3.zero;
        }
    }
}