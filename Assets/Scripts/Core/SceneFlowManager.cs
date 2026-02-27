using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor central del flujo de escenas.
/// Se usa desde menús, game over y wrappers antiguos para evitar hardcode repetido.
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Escenas por defecto")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private string _bunkerSceneName = "NPCBunkerNavigation";
    [SerializeField] private string _miniGame1SceneName = "MiniGameDesign1";
    [SerializeField] private string _miniGame2SceneName = "MiniGameDesign2";
    [SerializeField] private string _miniGame3SceneName = "MiniGameDesign3";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static SceneFlowManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        SceneFlowManager found = FindObjectOfType<SceneFlowManager>();
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        GameObject go = new GameObject("[SceneFlowManager]");
        return go.AddComponent<SceneFlowManager>();
    }

    public string GetMainMenuSceneName() => _mainMenuSceneName;
    public string GetBunkerSceneName() => _bunkerSceneName;
    public string GetMiniGame1SceneName() => _miniGame1SceneName;
    public string GetMiniGame2SceneName() => _miniGame2SceneName;
    public string GetMiniGame3SceneName() => _miniGame3SceneName;

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName, LoadSceneMode.Single);
    }

    public void LoadBunker()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_bunkerSceneName, LoadSceneMode.Single);
    }

    public void LoadSceneByName(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneFlowManager: nombre de escena vacío.");
            return;
        }

        if (loadMode == LoadSceneMode.Single)
            Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName, loadMode);
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
    }

    public void LoadMiniGameAdditive(string miniGameSceneName)
    {
        if (string.IsNullOrWhiteSpace(miniGameSceneName))
        {
            Debug.LogWarning("SceneFlowManager: escena de minijuego inválida.");
            return;
        }

        // Si existe el gestor avanzado, delegamos para mantener el sistema de cámaras/canvases.
        if (AdvancedSceneCameraManager.instance != null)
        {
            AdvancedSceneCameraManager.instance.LoadAdditiveScene(miniGameSceneName);
            return;
        }

        // Fallback simple si por algún motivo no está el manager.
        SceneManager.LoadScene(miniGameSceneName, LoadSceneMode.Additive);
    }

    public void ReturnFromMiniGame(string baseSceneName, bool win)
    {
        if (AdvancedSceneCameraManager.instance != null)
        {
            AdvancedSceneCameraManager.instance.ReturnToBaseScene(baseSceneName, win);
            return;
        }

        // Fallback simple: cargar la escena base en single.
        LoadSceneByName(baseSceneName, LoadSceneMode.Single);
    }
}