using UnityEngine;

public class WinMiniGame : BaseMiniGameTrigger
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerMiniGameEnd();
        }
    }

    protected override void TriggerMiniGameEnd()
    {
        Debug.Log("Win trigger activated!");
        base.TriggerMiniGameEnd();
    }
}