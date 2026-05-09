using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BunkerLevelLoadButton : MonoBehaviour
{
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private string _levelSceneName;
    [SerializeField] private string _bunkerSceneNameOverride;

    public void LoadLevelFromButton()
    {
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        BunkerSceneBootstrapper bootstrapper = FindFirstObjectByType<BunkerSceneBootstrapper>();
        if (bootstrapper != null)
            bootstrapper.SaveCurrentSession();

        string bunkerSceneName = !string.IsNullOrWhiteSpace(_bunkerSceneNameOverride)
            ? _bunkerSceneNameOverride
            : SceneManager.GetActiveScene().name;

        BunkerSessionLaunch.SetCurrentBunkerScene(bunkerSceneName);
        BunkerSessionLaunch.RequestRevealOnNextSceneLoad();

        if (_transition != null)
            yield return _transition.PlayCoverRoutine();

        // Seguridad: no dejar que el nivel herede un Time.timeScale pausado del bunker.
        Time.timeScale = 1f;

        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RegisterLevelStarted(_levelSceneName);    

        if (!string.IsNullOrWhiteSpace(_levelSceneName))
            SceneManager.LoadScene(_levelSceneName);
    }
}
