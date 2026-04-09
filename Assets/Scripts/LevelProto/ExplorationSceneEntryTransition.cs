using System.Collections;
using UnityEngine;

public class ExplorationSceneEntryTransition : MonoBehaviour
{
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private bool _requireSessionRequest = true;

    private IEnumerator Start()
    {
        // Seguridad extra: si el bunker dejó el juego pausado al cambiar de escena,
        // el nivel no debe heredar ese estado.
        Time.timeScale = 1f;

        bool shouldReveal = !_requireSessionRequest || BunkerSessionLaunch.ConsumeRevealOnNextSceneLoad();
        if (!shouldReveal)
        {
            if (_transition != null)
                _transition.SetRevealedInstant();
            yield break;
        }

        // LEVEL1 ya tiene LevelIntroTransition, que bloquea/desbloquea al robot y
        // reproduce la transición. Si los dos scripts intentan hacer reveal a la vez,
        // uno de los coroutines se queda esperando y el robot puede quedarse bloqueado.
        if (FindFirstObjectByType<LevelIntroTransition>() != null)
            yield break;

        if (_transition == null)
            yield break;

        _transition.SetCoveredInstant();
        yield return null;
        yield return _transition.PlayRevealRoutine();
    }
}
