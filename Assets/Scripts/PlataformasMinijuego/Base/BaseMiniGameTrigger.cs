using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class BaseMiniGameTrigger : MonoBehaviour
{
    [SerializeField] protected string targetScene = "NPCBunkerNavigation";
    [SerializeField] protected bool isWinCondition = false;
    [SerializeField] protected TriggerType triggerType = TriggerType.Collider;

    protected enum TriggerType { Collider, Collision }

    protected virtual void TriggerMiniGameEnd()
    {
        Debug.Log($"MiniGame triggered: {(isWinCondition ? "Win" : "Game Over")}");

        if (AdvancedSceneCameraManager.instance != null)
        {
            AdvancedSceneCameraManager.instance.ReturnToBaseScene(targetScene, isWinCondition);
        }
        else
        {
            Debug.LogError("AdvancedSceneCameraManager instance not found!");
            // Fallback: Load scene directly
            SceneManager.LoadScene(targetScene);
        }
    }
}