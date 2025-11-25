// NPCStateMachine.cs
using UnityEngine;
using System.Collections;

public class NPCStateMachine : MonoBehaviour
{
    [Header("Referencias")]
    public DwellerNPC dwellerNPC;

    [Header("Estaciones de Necesidades")]
    public RestStation[] restStations;
    public FoodStation[] foodStations;
    public WaterStation[] waterStations;

    [Header("Configuración")]
    public float checkNeedsInterval = 1.5f; // REDUCIDO para respuesta más rápida
    public float minWorkTime = 8f;          // AUMENTADO para trabajar más tiempo

    private NPCState currentState = NPCState.Idle;
    private float stateTimer = 0f;
    private float workTimer = 0f;
    private RestStation currentRestStation;
    private FoodStation currentFoodStation;
    private WaterStation currentWaterStation;

    /// <summary>
    /// Estados posibles de la máquina de estados del NPC
    /// </summary>
    public enum NPCState
    {
        Idle,
        MovingToWork,
        Working,
        MovingToEat,
        Eating,
        MovingToDrink,
        Drinking,
        MovingToRest,
        Resting
    }

    void Awake()
    {
        if (dwellerNPC == null)
            dwellerNPC = GetComponent<DwellerNPC>();

        FindAllNeedStations();
    }

    void Start()
    {
        if (dwellerNPC == null) return;

        StartCoroutine(NeedsCheckRoutine());
    }

    /// <summary>
    /// Encuentra todas las estaciones de necesidades en la escena
    /// </summary>
    void FindAllNeedStations()
    {
        restStations = FindObjectsOfType<RestStation>();
        foodStations = FindObjectsOfType<FoodStation>();
        waterStations = FindObjectsOfType<WaterStation>();

        Debug.Log($"{dwellerNPC.dwellerName} encontró: {restStations.Length} camas, {foodStations.Length} comedores, {waterStations.Length} bebederos");
    }

    void Update()
    {
        if (dwellerNPC == null) return;

        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case NPCState.Working:
                workTimer += Time.deltaTime;
                UpdateNeeds(Time.deltaTime);

                // Verificar si debe tomar un descanso programado
                if (workTimer > minWorkTime && dwellerNPC.needs.IsCritical())
                {
                    CheckNeedsImmediate();
                }
                break;

            case NPCState.Eating:
                UpdateNeeds(Time.deltaTime);
                if (currentFoodStation != null)
                {
                    dwellerNPC.needs.Eat(Time.deltaTime);

                    if (dwellerNPC.needs.IsFull() || stateTimer > 5f) // Tiempo máximo de comida
                    {
                        Debug.Log($"{dwellerNPC.dwellerName} terminó de comer. Hambre: {(int)dwellerNPC.needs.hunger}");
                        ReturnToWork();
                    }
                }
                break;

            case NPCState.Drinking:
                UpdateNeeds(Time.deltaTime);
                if (currentWaterStation != null)
                {
                    dwellerNPC.needs.Drink(Time.deltaTime);

                    if (dwellerNPC.needs.IsHydrated() || stateTimer > 3f) // Tiempo máximo de bebida
                    {
                        Debug.Log($"{dwellerNPC.dwellerName} terminó de beber. Sed: {(int)dwellerNPC.needs.thirst}");
                        ReturnToWork();
                    }
                }
                break;

            case NPCState.Resting:
                UpdateNeeds(Time.deltaTime);
                if (currentRestStation != null)
                {
                    float restEfficiency = currentRestStation.restEfficiency;
                    dwellerNPC.needs.Rest(Time.deltaTime * restEfficiency);

                    if (dwellerNPC.needs.IsFullyRested() || stateTimer > 15f) // Tiempo máximo de descanso
                    {
                        Debug.Log($"{dwellerNPC.dwellerName} descansó suficiente (F:{(int)dwellerNPC.needs.fatigue})");
                        ReturnToWork();
                    }
                }
                break;

            case NPCState.Idle:
                if (dwellerNPC.assignedWorkStation != null)
                {
                    ChangeState(NPCState.MovingToWork);
                    StartCoroutine(MoveToWorkStation());
                }
                break;
        }
    }

    /// <summary>
    /// Actualiza las necesidades del NPC
    /// </summary>
    void UpdateNeeds(float deltaTime)
    {
        if (dwellerNPC.needs != null)
        {
            dwellerNPC.needs.UpdateNeeds(deltaTime);
        }
    }

    /// <summary>
    /// Corrutina para verificar necesidades periódicamente
    /// </summary>
    IEnumerator NeedsCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkNeedsInterval);

            if (dwellerNPC.needs != null && currentState == NPCState.Working)
            {
                CheckNeeds();
            }
        }
    }

    /// <summary>
    /// Verificación inmediata de necesidades (para interrupciones)
    /// </summary>
    void CheckNeedsImmediate()
    {
        if (dwellerNPC.needs == null) return;

        // PRIORIDAD MEJORADA: Sed > Fatiga > Hambre
        if (dwellerNPC.needs.thirst >= dwellerNPC.needs.criticalThirst)
        {
            FindDrinkStation();
        }
        else if (dwellerNPC.needs.fatigue >= dwellerNPC.needs.criticalFatigue)
        {
            FindRestStation();
        }
        else if (dwellerNPC.needs.hunger >= dwellerNPC.needs.criticalHunger)
        {
            FindFoodStation();
        }
    }

    /// <summary>
    /// Verificación regular de necesidades
    /// </summary>
    void CheckNeeds()
    {
        if (dwellerNPC.needs == null) return;

        // Solo verificar si ha trabajado el tiempo mínimo
        if (workTimer < minWorkTime) return;

        CheckNeedsImmediate();
    }

    /// <summary>
    /// Busca una estación de descanso disponible
    /// </summary>
    void FindRestStation()
    {
        if (restStations == null || restStations.Length == 0)
        {
            Debug.Log($"{dwellerNPC.dwellerName} no encontró camas disponibles");
            return;
        }

        foreach (var bed in restStations)
        {
            if (bed != null && bed.CanAcceptNPC())
            {
                currentRestStation = bed;
                ChangeState(NPCState.MovingToRest);
                StartCoroutine(MoveToRestStation());
                return;
            }
        }

        Debug.Log($"{dwellerNPC.dwellerName} no encontró camas disponibles (todas ocupadas)");
    }

    /// <summary>
    /// Busca una estación de comida disponible
    /// </summary>
    void FindFoodStation()
    {
        if (foodStations == null || foodStations.Length == 0)
        {
            Debug.Log($"{dwellerNPC.dwellerName} no encontró comedores disponibles");
            return;
        }

        foreach (var foodStation in foodStations)
        {
            if (foodStation != null && foodStation.CanAcceptNPC())
            {
                currentFoodStation = foodStation;
                ChangeState(NPCState.MovingToEat);
                StartCoroutine(MoveToFoodStation());
                return;
            }
        }

        Debug.Log($"{dwellerNPC.dwellerName} no encontró comedores disponibles (todos ocupados)");
    }

    /// <summary>
    /// Busca una estación de agua disponible
    /// </summary>
    void FindDrinkStation()
    {
        if (waterStations == null || waterStations.Length == 0)
        {
            Debug.Log($"{dwellerNPC.dwellerName} no encontró bebederos disponibles");
            return;
        }

        foreach (var waterStation in waterStations)
        {
            if (waterStation != null && waterStation.CanAcceptNPC())
            {
                currentWaterStation = waterStation;
                ChangeState(NPCState.MovingToDrink);
                StartCoroutine(MoveToWaterStation());
                return;
            }
        }

        Debug.Log($"{dwellerNPC.dwellerName} no encontró bebederos disponibles (todos ocupados)");
    }

    /// <summary>
    /// Corrutina para mover el NPC a una estación de descanso
    /// </summary>
    IEnumerator MoveToRestStation()
    {
        if (currentRestStation == null) yield break;

        Vector3 targetPosition = currentRestStation.GetRestPosition();

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, dwellerNPC.moveSpeed * Time.deltaTime);
            yield return null;
        }

        currentRestStation.AssignRestingNPC(dwellerNPC);
        ChangeState(NPCState.Resting);
        Debug.Log($"{dwellerNPC.dwellerName} se acostó (F:{(int)dwellerNPC.needs.fatigue})");
    }

    /// <summary>
    /// Corrutina para mover el NPC a una estación de comida
    /// </summary>
    IEnumerator MoveToFoodStation()
    {
        if (currentFoodStation == null) yield break;

        Vector3 targetPosition = currentFoodStation.GetFoodPosition();

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, dwellerNPC.moveSpeed * Time.deltaTime);
            yield return null;
        }

        currentFoodStation.AssignEatingNPC(dwellerNPC);
        ChangeState(NPCState.Eating);
        Debug.Log($"{dwellerNPC.dwellerName} empezó a comer (H:{(int)dwellerNPC.needs.hunger})");
    }

    /// <summary>
    /// Corrutina para mover el NPC a una estación de agua
    /// </summary>
    IEnumerator MoveToWaterStation()
    {
        if (currentWaterStation == null) yield break;

        Vector3 targetPosition = currentWaterStation.GetWaterPosition();

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, dwellerNPC.moveSpeed * Time.deltaTime);
            yield return null;
        }

        currentWaterStation.AssignDrinkingNPC(dwellerNPC);
        ChangeState(NPCState.Drinking);
        Debug.Log($"{dwellerNPC.dwellerName} empezó a beber (S:{(int)dwellerNPC.needs.thirst})");
    }

    /// <summary>
    /// Corrutina para mover el NPC a su estación de trabajo
    /// </summary>
    IEnumerator MoveToWorkStation()
    {
        if (dwellerNPC.assignedWorkStation == null) yield break;

        Vector3 targetPosition = dwellerNPC.assignedWorkStation.GetWorkerPosition();

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, dwellerNPC.moveSpeed * Time.deltaTime);
            yield return null;
        }

        ChangeState(NPCState.Working);
        workTimer = 0f;
        OnArrivedAtWorkStation();
    }

    /// <summary>
    /// Hace que el NPC regrese a trabajar después de satisfacer sus necesidades
    /// </summary>
    void ReturnToWork()
    {
        // Limpiar todas las estaciones de necesidades
        if (currentRestStation != null)
        {
            currentRestStation.RemoveRestingNPC(dwellerNPC);
            currentRestStation = null;
        }
        if (currentFoodStation != null)
        {
            currentFoodStation.RemoveEatingNPC(dwellerNPC);
            currentFoodStation = null;
        }
        if (currentWaterStation != null)
        {
            currentWaterStation.RemoveDrinkingNPC(dwellerNPC);
            currentWaterStation = null;
        }

        if (dwellerNPC.assignedWorkStation != null)
        {
            Debug.Log($"{dwellerNPC.dwellerName} vuelve al trabajo");
            ChangeState(NPCState.MovingToWork);
            StartCoroutine(MoveToWorkStation());
        }
        else
        {
            ChangeState(NPCState.Idle);
        }
    }

    /// <summary>
    /// Asigna el NPC a una estación de trabajo específica
    /// </summary>
    public void AssignToWork(WorkStation station)
    {
        // Limpiar estaciones de necesidades si está en una
        if (currentState == NPCState.Resting && currentRestStation != null)
        {
            currentRestStation.RemoveRestingNPC(dwellerNPC);
            currentRestStation = null;
        }
        if (currentState == NPCState.Eating && currentFoodStation != null)
        {
            currentFoodStation.RemoveEatingNPC(dwellerNPC);
            currentFoodStation = null;
        }
        if (currentState == NPCState.Drinking && currentWaterStation != null)
        {
            currentWaterStation.RemoveDrinkingNPC(dwellerNPC);
            currentWaterStation = null;
        }

        ChangeState(NPCState.MovingToWork);
        StartCoroutine(MoveToWorkStation());
    }

    /// <summary>
    /// Llamado cuando el NPC llega a su estación de trabajo
    /// </summary>
    public void OnArrivedAtWorkStation()
    {
        Debug.Log($"{dwellerNPC.dwellerName} llegó a la estación de trabajo");
    }

    /// <summary>
    /// Cambia el estado actual del NPC
    /// </summary>
    void ChangeState(NPCState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"{dwellerNPC.dwellerName}: {currentState} -> {newState} | {dwellerNPC.needs.GetNeedsStatus()}");
        currentState = newState;
        stateTimer = 0f;

        if (dwellerNPC != null)
        {
            switch (newState)
            {
                case NPCState.Working:
                    dwellerNPC.currentState = DwellerState.Working;
                    workTimer = 0f;
                    break;
                case NPCState.MovingToWork: dwellerNPC.currentState = DwellerState.MovingToWork; break;
                case NPCState.Eating: dwellerNPC.currentState = DwellerState.Eating; break;
                case NPCState.Drinking: dwellerNPC.currentState = DwellerState.Drinking; break;
                case NPCState.Resting: dwellerNPC.currentState = DwellerState.Resting; break;
                case NPCState.Idle: dwellerNPC.currentState = DwellerState.Idle; break;
                default: dwellerNPC.currentState = DwellerState.Idle; break;
            }
        }
    }

    // Métodos de verificación de estado
    public bool IsWorking() => currentState == NPCState.Working;
    public bool IsResting() => currentState == NPCState.Resting;
    public bool IsEating() => currentState == NPCState.Eating;
    public bool IsDrinking() => currentState == NPCState.Drinking;
}