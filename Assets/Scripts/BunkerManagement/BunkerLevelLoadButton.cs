using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BunkerLevelLoadButton : MonoBehaviour
{
    [SerializeField] private ScreenPatternTransition _transition;
    [SerializeField] private string _levelSceneName;

    public void LoadLevelFromButton()
    {
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        if (_transition != null)
            yield return _transition.PlayCoverRoutine();

        if (!string.IsNullOrWhiteSpace(_levelSceneName))
            SceneManager.LoadScene(_levelSceneName);
    }
}
