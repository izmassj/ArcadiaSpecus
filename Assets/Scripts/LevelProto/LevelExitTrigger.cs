using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExitTrigger : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private LockedDoor _door;
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private RobotController _player;
    [SerializeField] private CinemachineCamera _currentCamera;
    [SerializeField] private CinemachineCamera _exitCamera;
    [SerializeField] private int _activePriority = 20;
    [SerializeField] private int _inactivePriority = 10;
    [SerializeField] private string _bunkerSceneNameOverride;

    [Header("Reward Range")]
    [SerializeField] private bool _allowJoinedInhabitantsReward = true;
    [SerializeField, Range(0f, 1f)] private float _joinedInhabitantsChance = 0.2f;
    [SerializeField] private Vector2Int _joinedInhabitantsRange = new Vector2Int(1, 1);
    [SerializeField] private Vector2Int _scrapRewardRange = new Vector2Int(5, 15);
    [SerializeField] private Vector2Int _electricityRewardRange = new Vector2Int(2, 8);
    [SerializeField] private Vector2Int _waterRewardRange = new Vector2Int(2, 8);
    [SerializeField] private Vector2Int _foodRewardRange = new Vector2Int(2, 8);

    [Header("Transition Range")]
    [SerializeField] private Vector2 _coverStartRange = new Vector2(0f, 0f);
    [SerializeField] private Vector2 _coverEndRange = new Vector2(0f, 1f);
    [SerializeField] private Vector2 _revealStartRange = new Vector2(0f, 1f);
    [SerializeField] private Vector2 _revealEndRange = new Vector2(1f, 1f);

    private bool _triggered;

    private void Update()
    {
        CinemachineBrain brain = CinemachineBrain.GetActiveBrain(0);
        if (brain != null)
        {
            CinemachineCamera activeCamera = brain.ActiveVirtualCamera as CinemachineCamera;
            if (activeCamera != null && _currentCamera != activeCamera)
                _currentCamera = activeCamera;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag(_playerTag))
            return;

        _triggered = true;
        ResetTransitionValues();
        StartCoroutine(ExitRoutine());
    }

    private void ResetTransitionValues()
    {
        if (_transition == null)
            return;

        _transition.coverStartRange = _coverStartRange;
        _transition.coverEndRange = _coverEndRange;
        _transition.revealStartRange = _revealStartRange;
        _transition.revealEndRange = _revealEndRange;
    }

    private IEnumerator ExitRoutine()
    {
        if (_player != null)
            _player.SetMovementLocked(true, true);

        if (_exitCamera != null)
            _exitCamera.Priority = _activePriority;

        if (_currentCamera != null)
            _currentCamera.Priority = _inactivePriority;

        yield return new WaitForSeconds(1f);

        if (_door != null)
            _door.CloseDoor();

        yield return new WaitForSeconds(1f);

        if (_transition != null)
        {
            ResetTransitionValues();
            yield return _transition.PlayCoverRoutine();
        }

        QueueRandomRewards();

        string bunkerSceneName = !string.IsNullOrWhiteSpace(_bunkerSceneNameOverride)
            ? _bunkerSceneNameOverride
            : BunkerSessionLaunch.CurrentBunkerSceneName;

        if (!string.IsNullOrWhiteSpace(bunkerSceneName))
            SceneManager.LoadScene(bunkerSceneName);
    }

    private void QueueRandomRewards()
    {
        int scrap = GetRandomAmount(_scrapRewardRange);
        int electricity = GetRandomAmount(_electricityRewardRange);
        int water = GetRandomAmount(_waterRewardRange);
        int food = GetRandomAmount(_foodRewardRange);
        int joinedInhabitants = 0;

        BunkerSessionLaunch.AddPendingReward(BunkerResourceType.Scrap, scrap);
        BunkerSessionLaunch.AddPendingReward(BunkerResourceType.Electricity, electricity);
        BunkerSessionLaunch.AddPendingReward(BunkerResourceType.Water, water);
        BunkerSessionLaunch.AddPendingReward(BunkerResourceType.Food, food);

        if (_allowJoinedInhabitantsReward && Random.value <= _joinedInhabitantsChance)
        {
            joinedInhabitants = GetRandomAmount(_joinedInhabitantsRange);
            BunkerSessionLaunch.AddPendingJoinedInhabitants(joinedInhabitants);
        }

        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RegisterLevelCompleted(SceneManager.GetActiveScene().name, scrap, electricity, water, food, joinedInhabitants);
    }

    private int GetRandomAmount(Vector2Int range)
    {
        int min = Mathf.Min(range.x, range.y);
        int max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max + 1);
    }
}
