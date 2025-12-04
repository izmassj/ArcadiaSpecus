// WorkStation.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorkStation : MonoBehaviour
{
    [Header("Configuración Estación")]
    public string stationId;
    public string stationName;
    public ResourceType producedResource;
    public bool isConsumptionStation = false; // Distingue entre producción y consumo
    public int baseProduction = 1;
    public float productionInterval = 3f;

    [Header("Estado")]
    [SerializeField] protected List<DwellerNPC> assignedWorkers = new List<DwellerNPC>();
    [SerializeField] protected bool isProducing = false;

    private Coroutine productionCoroutine;

    public System.Action<ResourceType, int> OnProduction;

    void Start()
    {
        if (string.IsNullOrEmpty(stationId))
            stationId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Asigna un trabajador a esta estación
    /// </summary>
    public virtual void AssignWorker(DwellerNPC worker)
    {
        if (!assignedWorkers.Contains(worker))
        {
            assignedWorkers.Add(worker);

            // Solo iniciar producción si NO es estación de consumo
            if (!isConsumptionStation && assignedWorkers.Count == 1 && !isProducing)
            {
                StartProduction();
            }
        }
    }

    /// <summary>
    /// Remueve un trabajador de esta estación
    /// </summary>
    public virtual void RemoveWorker(DwellerNPC worker)
    {
        if (assignedWorkers.Contains(worker))
        {
            assignedWorkers.Remove(worker);

            // Solo detener producción si NO es estación de consumo
            if (!isConsumptionStation && assignedWorkers.Count == 0 && isProducing)
            {
                StopProduction();
            }
        }
    }

    /// <summary>
    /// Inicia la producción de recursos (solo para estaciones de producción)
    /// </summary>
    private void StartProduction()
    {
        if (isConsumptionStation)
        {
            Debug.Log($"{stationName} es estación de consumo - No inicia producción");
            return;
        }

        if (productionCoroutine != null)
            StopCoroutine(productionCoroutine);

        productionCoroutine = StartCoroutine(ProductionLoop());
        isProducing = true;
        Debug.Log($"{stationName} inició producción de {producedResource}");
    }

    /// <summary>
    /// Detiene la producción de recursos (solo para estaciones de producción)
    /// </summary>
    private void StopProduction()
    {
        if (isConsumptionStation) return;

        if (productionCoroutine != null)
            StopCoroutine(productionCoroutine);

        isProducing = false;
        Debug.Log($"{stationName} detuvo producción");
    }

    /// <summary>
    /// Bucle de producción que genera recursos periódicamente
    /// </summary>
    private IEnumerator ProductionLoop()
    {
        while (isProducing && !isConsumptionStation)
        {
            yield return new WaitForSeconds(productionInterval);

            if (assignedWorkers.Count > 0)
            {
                int effectiveWorkers = 0;
                foreach (var worker in assignedWorkers)
                {
                    if (worker != null && worker.CanWorkEffectively())
                    {
                        effectiveWorkers++;
                    }
                }

                if (effectiveWorkers > 0)
                {
                    int totalProduction = baseProduction * effectiveWorkers;
                    ResourceManager.Instance.AddResource(producedResource, totalProduction);
                    OnProduction?.Invoke(producedResource, totalProduction);

                    Debug.Log($"{stationName} produjo {totalProduction} {producedResource} " +
                             $"(Trabajadores: {effectiveWorkers}/{assignedWorkers.Count})");
                }
                else
                {
                    Debug.Log($"{stationName} sin producción - trabajadores necesitan descanso");
                }
            }
        }
    }

    /// <summary>
    /// Obtiene una posición aleatoria para el trabajador cerca de la estación
    /// </summary>
    public Vector3 GetWorkerPosition()
    {
        return transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
    }

    /// <summary>
    /// Obtiene una posición exacta para el trabajador frente a la estación
    /// </summary>
    public Vector3 GetExactWorkerPosition()
    {
        return transform.position + transform.forward * 2f;
    }

    /// <summary>
    /// Obtiene la lista de trabajadores asignados a esta estación
    /// </summary>
    public List<DwellerNPC> GetAssignedWorkers()
    {
        return new List<DwellerNPC>(assignedWorkers);
    }

    /// <summary>
    /// Datos para guardar el estado de la estación de trabajo
    /// </summary>
    [System.Serializable]
    public class WorkStationSaveData
    {
        public string stationId;
        public string stationName;
        public List<string> assignedWorkerNames;
        public Vector3 position;
        public Quaternion rotation;
        public bool isConsumptionStation;
        public ResourceType producedResource;
    }

    /// <summary>
    /// Obtiene los datos para guardar el estado de la estación
    /// </summary>
    public virtual WorkStationSaveData GetSaveData()
    {
        List<string> workerNames = new List<string>();
        foreach (var worker in assignedWorkers)
        {
            if (worker != null)
                workerNames.Add(worker.dwellerName);
        }

        return new WorkStationSaveData
        {
            stationId = this.stationId,
            stationName = this.stationName,
            assignedWorkerNames = workerNames,
            position = transform.position,
            rotation = transform.rotation,
            isConsumptionStation = this.isConsumptionStation,
            producedResource = this.producedResource
        };
    }

    /// <summary>
    /// Carga los datos guardados de la estación
    /// </summary>
    public virtual void LoadData(WorkStationSaveData data)
    {
        stationName = data.stationName;
        transform.position = data.position;
        transform.rotation = data.rotation;
        isConsumptionStation = data.isConsumptionStation;
        producedResource = data.producedResource;
    }
}