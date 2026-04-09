using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

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

    [Header("Transition Range")]
    [SerializeField] private Vector2 _coverStartRange = new Vector2(0f, 0f);
    [SerializeField] private Vector2 _coverEndRange = new Vector2(0f, 1f);
    [SerializeField] private Vector2 _revealStartRange = new Vector2(0f, 1f);
    [SerializeField] private Vector2 _revealEndRange = new Vector2(1f, 1f);

    private bool _triggered;

    private void Update()
    {
        if (_currentCamera != CinemachineBrain.GetActiveBrain(0).ActiveVirtualCamera as CinemachineCamera)
        {
            _currentCamera = CinemachineBrain.GetActiveBrain(0).ActiveVirtualCamera as CinemachineCamera;
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

        _door.CloseDoor();

        yield return new WaitForSeconds(1f);

        if (_transition != null)
        {
            ResetTransitionValues();

            yield return _transition.PlayCoverRoutine();
        }
    }
}
