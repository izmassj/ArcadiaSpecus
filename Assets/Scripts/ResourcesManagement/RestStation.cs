using System.Collections.Generic;
using UnityEngine;

public class RestStation : WorkStation
{
    [Header("Configuración Descanso")]
    public int maxRestingNPCs = 2;
    public float restEfficiency = 1.5f;
    public float fatigueRecoveryRate = 40f;

    [SerializeField] private float _slotSpacing = 1f;

    private readonly List<DwellerNPC> _restingNPCs = new List<DwellerNPC>();

    protected override void Awake()
    {
        base.Awake();
        stationName = string.IsNullOrWhiteSpace(stationName) ? "Cama Descanso" : stationName;
        isConsumptionStation = true;
    }

    private void Update()
    {
        for (int i = _restingNPCs.Count - 1; i >= 0; i--)
        {
            DwellerNPC _npc = _restingNPCs[i];
            if (_npc == null || _npc.IsDead)
            {
                _restingNPCs.RemoveAt(i);
                continue;
            }

            if (_npc.needs != null)
            {
                float _mult = Mathf.Max(0.1f, restEfficiency);
                _npc.needs.Rest(Time.deltaTime * _mult);
            }
        }
    }

    public bool CanAcceptNPC()
    {
        return _restingNPCs.Count < Mathf.Max(1, maxRestingNPCs);
    }

    public void AssignRestingNPC(DwellerNPC _npc)
    {
        if (_npc == null)
        {
            return;
        }

        if (_restingNPCs.Contains(_npc) || !CanAcceptNPC())
        {
            return;
        }

        _restingNPCs.Add(_npc);
        _npc.transform.position = GetRestingPosition(_restingNPCs.Count - 1);
    }

    public void RemoveRestingNPC(DwellerNPC _npc)
    {
        if (_npc == null)
        {
            return;
        }

        _restingNPCs.Remove(_npc);
    }

    private Vector3 GetRestingPosition(int _index)
    {
        float _offset = (_index - (Mathf.Max(1, maxRestingNPCs) - 1) * 0.5f) * _slotSpacing;
        return transform.position + transform.right * _offset;
    }

    public Vector3 GetRestPosition()
    {
        return GetRestingPosition(0);
    }

    public bool ShouldNPCLeave(DwellerNPC _npc)
    {
        return _npc == null || _npc.needs == null || _npc.needs.IsFullyRested();
    }

    public int GetRestingNPCCount()
    {
        return _restingNPCs.Count;
    }
}
