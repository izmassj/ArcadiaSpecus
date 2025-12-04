using UnityEngine;

public class SceneChangeTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            GetComponent<SceneSwitchManager>().points = 40;
            // Call the scene change from the CollectibleManager
            CollectibleManager.Instance.ChangeScene();
        }
    }
}