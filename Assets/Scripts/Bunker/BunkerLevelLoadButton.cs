using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BunkerLevelLoadButton : MonoBehaviour
{
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private string _levelSceneName;
    [SerializeField] private string[] _levelSceneNames = { "LEVEL1", "LEVEL2", "LEVEL3" };
    [SerializeField] private bool _avoidImmediateRepeat = true;
    [SerializeField] private string _bunkerSceneNameOverride;

    private static string _lastLoadedLevelSceneName;
    private bool _loading;

    public void LoadLevelFromButton()
    {
        if (_loading)
            return;

        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        _loading = true;

        string selectedLevelSceneName = GetRandomLevelSceneName();

        if (string.IsNullOrWhiteSpace(selectedLevelSceneName))
        {
            Debug.LogWarning($"{nameof(BunkerLevelLoadButton)}: no hay ningun nivel configurado para cargar.", this);
            _loading = false;
            yield break;
        }

        if (!IsSceneIncludedInBuildSettings(selectedLevelSceneName))
        {
            Debug.LogError($"{nameof(BunkerLevelLoadButton)}: la escena '{selectedLevelSceneName}' no esta en Build Settings. Añade LEVEL1, LEVEL2, LEVEL3 y REV-BUNKERSYS en File > Build Settings o File > Build Profiles.", this);
            _loading = false;
            yield break;
        }

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

        Time.timeScale = 1f;

        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RegisterLevelStarted(selectedLevelSceneName);

        _lastLoadedLevelSceneName = selectedLevelSceneName;
        SceneManager.LoadScene(selectedLevelSceneName);
    }

    private string GetRandomLevelSceneName()
    {
        List<string> validSceneNames = new List<string>();

        if (_levelSceneNames != null)
        {
            for (int i = 0; i < _levelSceneNames.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(_levelSceneNames[i]))
                    validSceneNames.Add(_levelSceneNames[i]);
            }
        }

        if (validSceneNames.Count == 0 && !string.IsNullOrWhiteSpace(_levelSceneName))
            validSceneNames.Add(_levelSceneName);

        if (validSceneNames.Count == 0)
            return string.Empty;

        if (_avoidImmediateRepeat && validSceneNames.Count > 1 && !string.IsNullOrWhiteSpace(_lastLoadedLevelSceneName))
            validSceneNames.Remove(_lastLoadedLevelSceneName);

        return validSceneNames[Random.Range(0, validSceneNames.Count)];
    }

    private bool IsSceneIncludedInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = Path.GetFileNameWithoutExtension(scenePath);

            if (sceneNameInBuild == sceneName)
                return true;
        }

        return false;
    }
}
