using UnityEngine;

public class KillFloorTrigger : MonoBehaviour
{
    [SerializeField] private LevelDeathManager _deathManager;

    private void OnTriggerEnter(Collider other)
    {
        RobotController player = other.GetComponentInParent<RobotController>();

        if (player == null)
            return;

        if (_deathManager != null)
            _deathManager.KillPlayer();
    }
}