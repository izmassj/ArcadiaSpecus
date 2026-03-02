using System.Collections.Generic;
using UnityEngine;

public class WaterStation : WorkStation
{
    [Header("Configuración Dispensador Agua")]
    public int maxDrinkingNPCs = 3;
    public int waterConsumptionAmount = 1;
    public float drinkingDuration = 3f;

    [SerializeField] private float _consumptionTickSeconds = 1f;
    [SerializeField] private float _slotSpacing = 1f;

    private readonly List<DwellerNPC> _drinkingNPCs = new List<DwellerNPC>();
    private float _consumptionTimer;

    protected override void Awake()
    {
        base.Awake();
        stationName = string.IsNullOrWhiteSpace(stationName) ? "Dispensador de Agua" : stationName;
        isConsumptionStation = true;

        requiresPower = true;
    }

    private void Update()
    {
        if (_drinkingNPCs.Count == 0)
        {
            return;
        }

        // Apagón
        if (ResourceManager.Instance != null &&
            requiresPower &&
            ResourceManager.Instance.ShouldPowerOutageDisableStations() &&
            !ResourceManager.Instance.IsPowerOnline())
        {
            return;
        }

        _consumptionTimer += Time.deltaTime;
        if (_consumptionTimer < Mathf.Max(0.1f, _consumptionTickSeconds))
        {
            return;
        }

        _consumptionTimer = 0f;
        ProcessWaterConsumptionTick();
    }

    private void ProcessWaterConsumptionTick()
    {
        if (ResourceManager.Instance == null)
        {
            return;
        }

        for (int i = _drinkingNPCs.Count - 1; i >= 0; i--)
        {
            DwellerNPC _npc = _drinkingNPCs[i];
            if (_npc == null || _npc.IsDead)
            {
                _drinkingNPCs.RemoveAt(i);
                continue;
            }

            bool _consumed = ResourceManager.Instance.ConsumeResource(ResourceType.Water, Mathf.Max(1, waterConsumptionAmount));
            if (_consumed && _npc.needs != null)
            {
                _npc.needs.Drink(1f);
            }
        }
    }

    public bool CanAcceptNPC()
    {
        return _drinkingNPCs.Count < Mathf.Max(1, maxDrinkingNPCs);
    }

    public void AssignDrinkingNPC(DwellerNPC _npc)
    {
        if (_npc == null)
        {
            return;
        }

        if (_drinkingNPCs.Contains(_npc) || !CanAcceptNPC())
        {
            return;
        }

        _drinkingNPCs.Add(_npc);
        _npc.transform.position = GetDrinkingPosition(_drinkingNPCs.Count - 1);
    }

    public void RemoveDrinkingNPC(DwellerNPC _npc)
    {
        if (_npc == null)
        {
            return;
        }

        _drinkingNPCs.Remove(_npc);
    }

    private Vector3 GetDrinkingPosition(int _index)
    {
        float _offset = (_index - (Mathf.Max(1, maxDrinkingNPCs) - 1) * 0.5f) * _slotSpacing;
        return transform.position + transform.forward * _offset;
    }

    public Vector3 GetWaterPosition()
    {
        return GetDrinkingPosition(0);
    }

    public int GetDrinkingNPCCount()
    {
        return _drinkingNPCs.Count;
    }
}
