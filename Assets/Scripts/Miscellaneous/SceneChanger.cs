using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wrapper simple para botones/objetos legacy.
/// Mantiene el método ChangeScene(string).
/// </summary>
public class SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneChanger: nombre de escena vacío.");
            return;
        }

        SceneFlowManager.EnsureInstance().LoadSceneByName(sceneName, LoadSceneMode.Single);
    }

    public void ChangeToMainMenu()
    {
        SceneFlowManager.EnsureInstance().LoadMainMenu();
    }

    public void ChangeToBunker()
    {
        SceneFlowManager.EnsureInstance().LoadBunker();
    }

    public void RestartCurrentScene()
    {
        SceneFlowManager.EnsureInstance().RestartCurrentScene();
    }
}