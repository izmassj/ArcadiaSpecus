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

    // NUEVO: Propiedad para controlar estado de muerte
    public bool IsDead { get; private set; } = false;

    // NUEVO: Evento para notificar muerte del NPC
    public System.Action<DwellerNPC> OnDeath;

    void Start()
    {
        if (stateMachine == null)
            stateMachine = GetComponent<NPCStateMachine>();

        // NUEVO: Configurar owner en las necesidades
        if (needs != null)
        {
            needs.SetOwner(this);
            needs.OnDeath += HandleDeath;
        }

        // Inicializar necesidades con valores aleatorios para variedad
        if (needs != null)
        {
            needs.hunger = Random.Range(0f, 30f);
            needs.thirst = Random.Range(0f, 30f);
            needs.fatigue = Random.Range(0f, 20f);
        }
    }

    void OnDestroy()
    {
        // NUEVO: Limpiar suscripción
        if (needs != null)
        {
            needs.OnDeath -= HandleDeath;
        }
    }

    void Update()
    {
        // Si está muerto, no hacer nada
        if (IsDead) return;

        // Debug visual de necesidades (opcional)
        if (Input.GetKeyDown(KeyCode.F1) && gameObject.name.Contains("Dweller"))
        {
            DebugNeeds();
        }
    }

    /// <summary>
    /// NUEVO: Maneja la muerte del NPC
    /// </summary>
    private void HandleDeath(DwellerNPC deadNPC)
    {
        if (IsDead) return; // Evitar múltiples llamadas

        IsDead = true;
        Debug.Log($"💀 {dwellerName} ha fallecido por necesidades extremas");

        // Cambiar estado a muerto
        currentState = DwellerState.Dead;

        // Desasignar de estación de trabajo
        if (assignedWorkStation != null)
        {
            assignedWorkStation.RemoveWorker(this);
            assignedWorkStation = null;
            isAssigned = false;
        }

        // NUEVO: Desactivar máquina de estados si existe
        if (stateMachine != null)
        {
            stateMachine.enabled = false;
        }

        // NUEVO: Desactivar colliders para evitar interacciones
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // NUEVO: Cambiar color a gris (visualmente muerto)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }

        // NUEVO: Notificar muerte a través del evento
        OnDeath?.Invoke(this);

        Debug.Log($"📢 {dwellerName} notificó su muerte al sistema");
    }

    /// <summary>
    /// Asigna este NPC a una estación de trabajo específica
    /// </summary>
    public void AssignToWorkStation(WorkStation station)
    {
        // NUEVO: No asignar si está muerto
        if (IsDead)
        {
            Debug.LogWarning($"No se puede asignar {dwellerName} - ESTÁ MUERTO");
            return;
        }

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
        // NUEVO: No cambiar estado si está muerto
        if (IsDead) return;

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
        if (needs.IsDead())
        {
            status = "MUERTO";
        }
        else if (needs.hunger >= needs.criticalHunger) status = "HAMBRE";
        else if (needs.thirst >= needs.criticalThirst) status = "SED";
        else if (needs.fatigue >= needs.criticalFatigue) status = "FATIGA";

        Debug.Log($"{dwellerName} - {status} | H:{(int)needs.hunger} S:{(int)needs.thirst} F:{(int)needs.fatigue} | Estado: {currentState} | Muerto: {IsDead}");
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
        // NUEVO: No puede trabajar si está muerto
        return !IsDead && !NeedsRecovery() && assignedWorkStation != null;
    }

    /// <summary>
    /// NUEVO: Reinicia el NPC (para reinicio de juego)
    /// </summary>
    public void Revive()
    {
        IsDead = false;
        needs.ResetNeeds();

        if (stateMachine != null)
        {
            stateMachine.enabled = true;
            // Llamar método de reinicio si existe en NPCStateMachine
            var restartMethod = stateMachine.GetType().GetMethod("RestartStateMachine");
            restartMethod?.Invoke(stateMachine, null);
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // Restaurar color original
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        currentState = DwellerState.Idle;
        Debug.Log($"{dwellerName} ha sido revivido");
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
        public bool isDead; // NUEVO: Guardar estado de muerte
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
            needs = this.needs,
            isDead = this.IsDead // NUEVO: Guardar estado de muerte
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

        // NUEVO: Cargar estado de muerte
        IsDead = data.isDead;

        if (data.needs != null && needs != null)
        {
            needs.hunger = data.needs.hunger;
            needs.thirst = data.needs.thirst;
            needs.fatigue = data.needs.fatigue;
        }

        // NUEVO: Si está muerto, aplicar estado de muerte
        if (IsDead)
        {
            HandleDeath(this);
        }
    }

    /// <summary>
    /// NUEVO: Mata instantáneamente a este NPC (para testing)
    /// </summary>
    [ContextMenu("💀 Matar NPC Instantáneamente")]
    public void KillInstantly()
    {
        if (IsDead)
        {
            Debug.LogWarning($"{dwellerName} ya está muerto");
            return;
        }

        Debug.LogWarning($"💀 MATANDO {dwellerName} INSTANTÁNEAMENTE...");

        if (needs != null)
        {
            needs.KillInstantly();
        }
        else
        {
            // Fallback si needs es null
            HandleDeath(this);
        }
    }

    /// <summary>
    /// NUEVO: Acelera necesidades para testing
    /// </summary>
    [ContextMenu("⚡ Acelerar Necesidades")]
    public void AccelerateNeeds()
    {
        if (needs != null)
        {
            needs.AccelerateNeedsForTesting();
        }
    }
}