using UnityEngine;

[DisallowMultipleComponent]
public class ElectricPowerProvider : MonoBehaviour
{
    [SerializeField] private bool _canPowerSockets = true;

    public bool CanPowerSockets => _canPowerSockets && isActiveAndEnabled;
}
