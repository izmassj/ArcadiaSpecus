using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkStation : MonoBehaviour
{
    [Header("Configuración Estación")]
    public string stationId;
    public string stationName = "Estación";
    public ResourceType producedResource = ResourceType.Materials;
    public bool isConsumptionStation = false;
    public int baseProduction = 1;
    public float productionInterval = 3f;

    [Header("Power")]
    [Tooltip("Si es true, esta estación se para completamente cuando no hay energía (rúbrica Bé).")]
    public bool requiresPower = true;

    [Header("Puntos opcionales")]
    [SerializeField] private Transform _workerPoint;

    [Header("Estado")]
    [SerializeField] protected List<DwellerNPC> assignedWorkers = new List<DwellerNPC>();
    [SerializeField] protected bool isProducing = false;

    private Coroutine _productionCoroutine;

    public Action<ResourceType, int> OnProduction;

    [Serializable]
    public class WorkStationSaveData
    {
        public string stationId;
        public string stationName;
        public List<string> assignedWorkerNames;
        public Vector3 position;
        public Quaternion rotation;
        public bool isConsumptionStation;
        public ResourceType producedResource;
        public bool requiresPower;
    }

    protected virtual void Awake()
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            stationId = Guid.NewGuid().ToString();
        }
    }

    protected virtual void OnDisable()
    {
        StopProduction();
    }

    public virtual void AssignWorker(DwellerNPC _worker)
    {
        if (_worker == null)
        {
            return;
        }

        if (assignedWorkers.Contains(_worker))
        {
            return;
        }

        assignedWorkers.Add(_worker);

        if (!isConsumptionStation && assignedWorkers.Count > 0)
        {
            StartProduction();
        }
    }

    public virtual void RemoveWorker(DwellerNPC _worker)
    {
        if (_worker == null)
        {
            return;
        }

        assignedWorkers.Remove(_worker);

        if (!isConsumptionStation && assignedWorkers.Count == 0)
        {
            StopProduction();
        }
    }

    private void StartProduction()
    {
        if (isConsumptionStation)
        {
            return;
        }

        if (_productionCoroutine != null)
        {
            StopCoroutine(_productionCoroutine);
        }

        isProducing = true;
        _productionCoroutine = StartCoroutine(ProductionLoop());
    }

    private void StopProduction()
    {
        if (_productionCoroutine != null)
        {
            StopCoroutine(_productionCoroutine);
            _productionCoroutine = null;
        }

        isProducing = false;
    }

    private IEnumerator ProductionLoop()
    {
        while (isProducing && enabled && gameObject.activeInHierarchy)
        {
            float _wait = Mathf.Max(0.1f, productionInterval);
            yield return new WaitForSeconds(_wait);

            if (ResourceManager.Instance == null)
            {
                continue;
            }

            // Apagón: si requiere power, se para totalmente.
            if (requiresPower && ResourceManager.Instance.ShouldPowerOutageDisableStations() && !ResourceManager.Instance.IsPowerOnline())
            {
                continue;
            }

            int _effectiveWorkers = 0;
            for (int i = assignedWorkers.Count - 1; i >= 0; i--)
            {
                DwellerNPC _worker = assignedWorkers[i];
                if (_worker == null)
                {
                    assignedWorkers.RemoveAt(i);
                    continue;
                }

                if (_worker.CanWorkEffectively())
                {
                    _effectiveWorkers++;
                }
            }

            if (_effectiveWorkers <= 0)
            {
                continue;
            }

            float _mult = ResourceManager.Instance.GetGlobalProductionMultiplier();
            int _totalProduction = Mathf.RoundToInt(Mathf.Max(1, baseProduction) * _effectiveWorkers * _mult);
            _totalProduction = Mathf.Max(1, _totalProduction);

            ResourceManager.Instance.AddResource(producedResource, _totalProduction);
            OnProduction?.Invoke(producedResource, _totalProduction);

            // Aquí iría feedback visual/sonoro de producción (si queréis).
        }
    }

    public Vector3 GetWorkerPosition()
    {
        if (_workerPoint != null)
        {
            return _workerPoint.position;
        }

        return transform.position + (transform.forward.sqrMagnitude > 0.01f ? transform.forward : Vector3.forward) * 1.5f;
    }

    public Vector3 GetExactWorkerPosition()
    {
        return GetWorkerPosition();
    }

    public List<DwellerNPC> GetAssignedWorkers()
    {
        return new List<DwellerNPC>(assignedWorkers);
    }

    public virtual WorkStationSaveData GetSaveData()
    {
        List<string> _workerNames = new List<string>();

        for (int i = 0; i < assignedWorkers.Count; i++)
        {
            if (assignedWorkers[i] != null)
            {
                _workerNames.Add(assignedWorkers[i].dwellerName);
            }
        }

        return new WorkStationSaveData
        {
            stationId = stationId,
            stationName = stationName,
            assignedWorkerNames = _workerNames,
            position = transform.position,
            rotation = transform.rotation,
            isConsumptionStation = isConsumptionStation,
            producedResource = producedResource,
            requiresPower = requiresPower
        };
    }

    public virtual void LoadData(WorkStationSaveData _data)
    {
        if (_data == null)
        {
            return;
        }

        stationId = string.IsNullOrWhiteSpace(_data.stationId) ? stationId : _data.stationId;
        stationName = string.IsNullOrWhiteSpace(_data.stationName) ? stationName : _data.stationName;
        transform.position = _data.position;
        transform.rotation = _data.rotation;
        isConsumptionStation = _data.isConsumptionStation;
        producedResource = _data.producedResource;

        // Compatibilidad: si el campo no existía en saves antiguos, queda en default true.
        requiresPower = _data.requiresPower;
    }
}
