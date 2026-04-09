using System.Collections;
using UnityEngine;

public class LevelIntroTransition : MonoBehaviour
{
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private RobotController _player;

    private IEnumerator Start()
    {
        if (_player != null)
            _player.SetMovementLocked(true, true);

        if (_transition != null)
        {
            _transition.SetCoveredInstant();
            yield return null;
            yield return _transition.PlayRevealRoutine();
        }

        if (_player != null)
            _player.SetMovementLocked(false, false);
    }
}
