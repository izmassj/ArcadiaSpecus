using UnityEngine;

public class GameOverMiniGame : BaseMiniGameTrigger
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerMiniGameEnd();
        }
    }
}