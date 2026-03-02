using System.Collections.Generic;
using UnityEngine;

public class FoodStation : WorkStation
{
    [Header("Configuración Dispensador Comida")]
    public int maxEatingNPCs = 3;
    public int foodConsumptionAmount = 1;
    public float eatingDuration = 5f;

    [SerializeField] private float _consumptionTickSeconds = 1f;
    [SerializeField] private float _slotSpacing = 1f;

    private readonly List<DwellerNPC> _eatingNPCs = new List<DwellerNPC>();
    private float _consumptionTimer;

    protected override void Awake()
    {
        base.Awake();
        stationName = string.IsNullOrWhiteSpace(stationName) ? "Dispensador de Comida" : stationName;
        isConsumptionStation = true;

        // Normalmente esto es un dispensador interno, así que requiere energía (apagón lo para).
        requiresPower = true;
    }

    private void Update()
    {
        if (_eatingNPCs.Count == 0)
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
        ProcessFoodConsumptionTick();
    }

    private void ProcessFoodConsumptionTick()
    {
        if (ResourceManager.Instance == null)
        {
            return;
        }

        for (int i = _eatingNPCs.Count - 1; i >= 0; i--)
        {
            DwellerNPC _npc = _eatingNPCs[i];
            if (_npc == null || _npc.IsDead)
            {
                _eatingNPCs.RemoveAt(i);
                continue;
            }

            int amt = Mathf.Max(1, foodConsumptionAmount);

            // Dinámica Bé: si no hay Food, usamos Rations (misma categoría).
            bool _consumed = ResourceManager.Instance.ConsumeFoodOrRations(amt);

            if (_consumed && _npc.needs != null)
            {
                _npc.needs.Eat(1f);
            }
        }
    }

    public bool CanAcceptNPC()
    {
        return _eatingNPCs.Count < Mathf.Max(1, maxEatingNPCs);
    }

    public void AssignEatingNPC(DwellerNPC _npc)
    {
        if (_npc == null)
        {
            return;
        }

        if (_eatingNPCs.Contains(_npc) || !CanAcceptNPC())
        {
            return;
        }

        _eatingNPCs.Add(_npc);
        _npc.transform.position = GetEatingPosition(_eatingNPCs.Count - 1);
    }

    public void RemoveEatingNPC(DwellerNPC _npc)
    {
        if (_npc == null)
        {
            return;
        }

        _eatingNPCs.Remove(_npc);
    }

    private Vector3 GetEatingPosition(int _index)
    {
        float _offset = (_index - (Mathf.Max(1, maxEatingNPCs) - 1) * 0.5f) * _slotSpacing;
        return transform.position + transform.right * _offset;
    }

    public Vector3 GetFoodPosition()
    {
        return GetEatingPosition(0);
    }

    public int GetEatingNPCCount()
    {
        return _eatingNPCs.Count;
    }
}
