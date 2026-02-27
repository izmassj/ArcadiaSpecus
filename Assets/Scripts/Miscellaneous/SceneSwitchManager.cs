using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton legacy mantenido por compatibilidad.
/// Ahora actúa como wrapper del SceneFlowManager.
/// </summary>
public class SceneSwitchManager : MonoBehaviour
{
    public static SceneSwitchManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneFlowManager.EnsureInstance();
    }

    public void LoadScene(string sceneName)
    {
        SceneFlowManager.EnsureInstance().LoadSceneByName(sceneName, LoadSceneMode.Single);
    }

    public void LoadSceneAdditive(string sceneName)
    {
        SceneFlowManager.EnsureInstance().LoadSceneByName(sceneName, LoadSceneMode.Additive);
    }

    public void LoadMainMenu()
    {
        SceneFlowManager.EnsureInstance().LoadMainMenu();
    }

    public void LoadBunker()
    {
        SceneFlowManager.EnsureInstance().LoadBunker();
    }

    public void RestartCurrentScene()
    {
        SceneFlowManager.EnsureInstance().RestartCurrentScene();
    }
}