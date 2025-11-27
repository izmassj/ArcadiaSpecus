// DwellerNPC.cs
using UnityEngine;
using System.Collections;

public class DwellerNPC : MonoBehaviour
{
    [Header("Configuración NPC")]
    public string dwellerName = "Habitante";
    public DwellerState currentState = DwellerState.Idle;

    [Header("Asignación")]
    public WorkStation assignedWorkStation;
    public bool isAssigned = false;

    [Header("Estadísticas")]
    public float workEfficiency = 1.0f;
    public float moveSpeed = 2.0f;

    [Header("Sistema de Necesidades")]
    public NPCNeeds needs = new NPCNeeds();
    [SerializeField] private NPCStateMachine stateMachine;

    void Start()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<NPCStateMachine>();

        // Inicializar necesidades con valores aleatorios para variedad
        if (needs != null)
        {
            needs.hunger = Random.Range(0f, 30f);
            needs.thirst = Random.Range(0f, 30f);
            needs.fatigue = Random.Range(0f, 20f);
        }
    }

    void Update()
    {
        // Debug visual de necesidades (opcional)
        if (Input.GetKeyDown(KeyCode.F1) && gameObject.name.Contains("Dweller"))
        {
            DebugNeeds();
        }
    }

    /// <summary>
    /// Asigna este NPC a una estación de trabajo específica
    /// </summary>
    public void AssignToWorkStation(WorkStation station)
    {
        if (assignedWorkStation != null)
            assignedWorkStation.RemoveWorker(this);

        assignedWorkStation = station;
        isAssigned = (station != null);

        if (isAssigned)
        {
            if (stateMachine != null)
                stateMachine.AssignToWork(station);
            else
            {
                ChangeState(DwellerState.MovingToWork);
                StartCoroutine(MoveToWorkStation());
            }
        }
        else
        {
            ChangeState(DwellerState.Idle);
        }
    }

    /// <summary>
    /// Corrutina para mover el NPC a su estación de trabajo
    /// </summary>
    private IEnumerator MoveToWorkStation()
    {
        if (assignedWorkStation == null) yield break;

        Vector3 targetPosition = assignedWorkStation.GetWorkerPosition();

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        ChangeState(DwellerState.Working);
        Debug.Log($"{dwellerName} llegó a {assignedWorkStation.stationName} y empezó a trabajar");
    }

    /// <summary>
    /// Cambia el estado del NPC
    /// </summary>
    private void ChangeState(DwellerState newState)
    {
        currentState = newState;
    }

    /// <summary>
    /// Muestra información de debug sobre las necesidades del NPC
    /// </summary>
    [ContextMenu("Debug Necesidades")]
    public void DebugNeeds()
    {
        if (needs == null)
        {
            Debug.Log($"{dwellerName} - NECESIDADES NULL");
            return;
        }

        string status = "OK";
        if (needs.hunger >= needs.criticalHunger) status = "HAMBRE";
        else if (needs.thirst >= needs.criticalThirst) status = "SED";
        else if (needs.fatigue >= needs.criticalFatigue) status = "FATIGA";

        Debug.Log($"{dwellerName} - {status} | H:{(int)needs.hunger} S:{(int)needs.thirst} F:{(int)needs.fatigue} | Estado: {currentState}");
    }

    /// <summary>
    /// Verifica si el NPC está en estado de recuperación (comiendo, bebiendo o descansando)
    /// </summary>
    public bool IsRecovering()
    {
        return currentState == DwellerState.Eating ||
               currentState == DwellerState.Resting ||
               currentState == DwellerState.Drinking;
    }

    /// <summary>
    /// Verifica si el NPC necesita recuperación (alguna necesidad crítica)
    /// </summary>
    public bool NeedsRecovery()
    {
        return needs.IsCritical();
    }

    /// <summary>
    /// Obtiene la necesidad más crítica del NPC
    /// </summary>
    public ResourceType GetMostCriticalNeed()
    {
        return needs.GetMostCriticalNeed();
    }

    /// <summary>
    /// Verifica si el NPC puede trabajar efectivamente
    /// </summary>
    public bool CanWorkEffectively()
    {
        return !NeedsRecovery() && assignedWorkStation != null;
    }

    /// <summary>
    /// Datos para guardar el estado del NPC
    /// </summary>
    [System.Serializable]
    public class DwellerSaveData
    {
        public string dwellerName;
        public Vector3 position;
        public string assignedStationId;
        public float workEfficiency;
        public NPCNeeds needs;
    }

    /// <summary>
    /// Obtiene los datos para guardar el estado del NPC
    /// </summary>
    public DwellerSaveData GetSaveData()
    {
        return new DwellerSaveData
        {
            dwellerName = this.dwellerName,
            position = transform.position,
            assignedStationId = assignedWorkStation != null ? assignedWorkStation.stationId : "",
            workEfficiency = this.workEfficiency,
            needs = this.needs
        };
    }

    /// <summary>
    /// Carga los datos guardados del NPC
    /// </summary>
    public void LoadData(DwellerSaveData data)
    {
        dwellerName = data.dwellerName;
        transform.position = data.position;
        workEfficiency = data.workEfficiency;

        if (data.needs != null && needs != null)
        {
            needs.hunger = data.needs.hunger;
            needs.thirst = data.needs.thirst;
            needs.fatigue = data.needs.fatigue;
        }
    }
}