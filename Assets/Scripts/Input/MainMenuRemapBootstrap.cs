using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Instala el menú de remapping en el Main Menu sin tocar escenas/prefabs.
/// Crea automáticamente el botón "Controls" y el panel de remap al cargar la escena del menú.
/// </summary>
public static class MainMenuRemapBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Por si ya está cargada una escena cuando entra el dominio (Play Mode con domain reload off).
        TryInstallForActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstallForScene(scene);
    }

    private static void TryInstallForActiveScene()
    {
        Scene s = SceneManager.GetActiveScene();
        TryInstallForScene(s);
    }

    private static void TryInstallForScene(Scene scene)
    {
        // Solo en escenas que parezcan menú, y/o si existe MainMenuManager.
        MainMenuManager menu = Object.FindObjectOfType<MainMenuManager>(true);
        if (menu == null) return;

        if (Object.FindObjectOfType<MainMenuRemapMenu>(true) != null)
            return;

        GameObject go = new GameObject("MainMenuRemapMenu");
        go.AddComponent<MainMenuRemapMenu>();
        Object.DontDestroyOnLoad(go);
    }
}
