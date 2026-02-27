using System.Collections;
using UnityEngine;

public class NPCStateMachine : MonoBehaviour
{
    [Header("Referencias")]
    public DwellerNPC dwellerNPC;

    [Header("Estaciones de Necesidades")]
    public RestStation[] restStations;
    public FoodStation[] foodStations;
    public WaterStation[] waterStations;

    [Header("Configuración")]
    public float checkNeedsInterval = 1.5f;
    public float minWorkTime = 8f;
    [SerializeField] private float _moveStopDistance = 0.1f;
    [SerializeField] private bool _autoFindStationsOnStart = true;

    private NPCState _currentState = NPCState.Idle;
    private float _stateTimer;
    private float _workTimer;

    private RestStation _currentRestStation;
    private FoodStation _currentFoodStation;
    private WaterStation _currentWaterStation;
    private Coroutine _moveCoroutine;
    private Coroutine _needsRoutine;

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

    private void Awake()
    {
        if (dwellerNPC == null)
        {
            dwellerNPC = GetComponent<DwellerNPC>();
        }

        if (_autoFindStationsOnStart)
        {
            FindAllNeedStations();
        }
    }

    private void OnEnable()
    {
        if (_needsRoutine == null)
        {
            _needsRoutine = StartCoroutine(NeedsCheckRoutine());
        }
    }

    private void Start()
    {
        if (_autoFindStationsOnStart)
        {
            FindAllNeedStations();
        }
    }

    private void OnDisable()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        if (_needsRoutine != null)
        {
            StopCoroutine(_needsRoutine);
            _needsRoutine = null;
        }

        ReleaseCurrentNeedStations();
    }

    private void Update()
    {
        if (!IsValidAndAlive())
        {
            return;
        }

        _stateTimer += Time.deltaTime;

        // Siempre degradan necesidades mientras el NPC está vivo.
        if (dwellerNPC.needs != null)
        {
            dwellerNPC.needs.UpdateNeeds(Time.deltaTime, dwellerNPC);
        }

        switch (_currentState)
        {
            case NPCState.Working:
                _workTimer += Time.deltaTime;
                // Cuando pasa el tiempo mínimo de trabajo, permitimos cortar para necesidades críticas.
                if (_workTimer >= minWorkTime && dwellerNPC.needs != null && dwellerNPC.needs.IsCritical())
                {
                    CheckNeedsImmediate();
                }
                break;

            case NPCState.Eating:
                UpdateEating();
                break;

            case NPCState.Drinking:
                UpdateDrinking();
                break;

            case NPCState.Resting:
                UpdateResting();
                break;
        }
    }

    private IEnumerator NeedsCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, checkNeedsInterval));
            CheckNeedsImmediate();
        }
    }

    public void FindAllNeedStations()
    {
        restStations = FindObjectsOfType<RestStation>(true);
        foodStations = FindObjectsOfType<FoodStation>(true);
        waterStations = FindObjectsOfType<WaterStation>(true);
    }

    public void AssignToWork(WorkStation _station)
    {
        if (!IsValidAndAlive())
        {
            return;
        }

        ReleaseCurrentNeedStations();

        if (_station == null)
        {
            ChangeState(NPCState.Idle);
            return;
        }

        StartMoveCoroutine(MoveToTarget(_station.GetWorkerPosition(), () =>
        {
            ChangeState(NPCState.Working);
            _workTimer = 0f;
            OnArrivedAtWorkStation();
        }, NPCState.MovingToWork));
    }

    public void OnArrivedAtWorkStation()
    {
        // Comentario audio: aquí iría sonido de inicio de tarea o animación de máquina.
    }

    private void CheckNeedsImmediate()
    {
        if (!IsValidAndAlive())
        {
            return;
        }

        if (dwellerNPC.needs == null)
        {
            return;
        }

        // Si ya está recuperándose, no redecidir.
        if (_currentState == NPCState.Eating || _currentState == NPCState.Drinking || _currentState == NPCState.Resting ||
            _currentState == NPCState.MovingToEat || _currentState == NPCState.MovingToDrink || _currentState == NPCState.MovingToRest)
        {
            return;
        }

        if (!dwellerNPC.needs.IsCritical())
        {
            return;
        }

        ResourceType _need = dwellerNPC.needs.GetMostCriticalNeed();

        if (_need == ResourceType.Water)
        {
            TryGoDrink();
            return;
        }

        if (_need == ResourceType.Energy)
        {
            TryGoRest();
            return;
        }

        if (_need == ResourceType.Food)
        {
            TryGoEat();
            return;
        }
    }

    private void TryGoEat()
    {
        if (foodStations == null || foodStations.Length == 0)
        {
            FindAllNeedStations();
        }

        for (int i = 0; i < foodStations.Length; i++)
        {
            FoodStation _station = foodStations[i];
            if (_station == null || !_station.CanAcceptNPC())
            {
                continue;
            }

            _currentFoodStation = _station;
            StartMoveCoroutine(MoveToTarget(_station.GetFoodPosition(), () =>
            {
                if (!IsValidAndAlive() || _currentFoodStation == null)
                {
                    return;
                }

                _currentFoodStation.AssignEatingNPC(dwellerNPC);
                ChangeState(NPCState.Eating);
            }, NPCState.MovingToEat));
            return;
        }
    }

    private void TryGoDrink()
    {
        if (waterStations == null || waterStations.Length == 0)
        {
            FindAllNeedStations();
        }

        for (int i = 0; i < waterStations.Length; i++)
        {
            WaterStation _station = waterStations[i];
            if (_station == null || !_station.CanAcceptNPC())
            {
                continue;
            }

            _currentWaterStation = _station;
            StartMoveCoroutine(MoveToTarget(_station.GetWaterPosition(), () =>
            {
                if (!IsValidAndAlive() || _currentWaterStation == null)
                {
                    return;
                }

                _currentWaterStation.AssignDrinkingNPC(dwellerNPC);
                ChangeState(NPCState.Drinking);
            }, NPCState.MovingToDrink));
            return;
        }
    }

    private void TryGoRest()
    {
        if (restStations == null || restStations.Length == 0)
        {
            FindAllNeedStations();
        }

        for (int i = 0; i < restStations.Length; i++)
        {
            RestStation _station = restStations[i];
            if (_station == null || !_station.CanAcceptNPC())
            {
                continue;
            }

            _currentRestStation = _station;
            StartMoveCoroutine(MoveToTarget(_station.GetRestPosition(), () =>
            {
                if (!IsValidAndAlive() || _currentRestStation == null)
                {
                    return;
                }

                _currentRestStation.AssignRestingNPC(dwellerNPC);
                ChangeState(NPCState.Resting);
            }, NPCState.MovingToRest));
            return;
        }
    }

    private IEnumerator MoveToTarget(Vector3 _target, System.Action _onArrive, NPCState _movingState)
    {
        ChangeState(_movingState);

        while (IsValidAndAlive())
        {
            float _distance = Vector3.Distance(transform.position, _target);
            if (_distance <= _moveStopDistance)
            {
                break;
            }

            float _speed = Mathf.Max(0.01f, dwellerNPC.moveSpeed);
            transform.position = Vector3.MoveTowards(transform.position, _target, _speed * Time.deltaTime);
            yield return null;
        }

        if (IsValidAndAlive())
        {
            _onArrive?.Invoke();
        }
    }

    private void UpdateEating()
    {
        if (_currentFoodStation == null || dwellerNPC.needs == null)
        {
            ReturnToWorkOrIdle();
            return;
        }

        // La estación consume recurso; aquí aplicamos recuperación continua del estado.
        dwellerNPC.needs.Eat(Time.deltaTime);

        if (dwellerNPC.needs.IsFull() || _stateTimer >= Mathf.Max(1f, _currentFoodStation.eatingDuration))
        {
            _currentFoodStation.RemoveEatingNPC(dwellerNPC);
            _currentFoodStation = null;
            ReturnToWorkOrIdle();
        }
    }

    private void UpdateDrinking()
    {
        if (_currentWaterStation == null || dwellerNPC.needs == null)
        {
            ReturnToWorkOrIdle();
            return;
        }

        dwellerNPC.needs.Drink(Time.deltaTime);

        if (dwellerNPC.needs.IsHydrated() || _stateTimer >= Mathf.Max(1f, _currentWaterStation.drinkingDuration))
        {
            _currentWaterStation.RemoveDrinkingNPC(dwellerNPC);
            _currentWaterStation = null;
            ReturnToWorkOrIdle();
        }
    }

    private void UpdateResting()
    {
        if (_currentRestStation == null || dwellerNPC.needs == null)
        {
            ReturnToWorkOrIdle();
            return;
        }

        // La estación también puede recuperar fatiga; este refuerzo hace la sensación más clara.
        dwellerNPC.needs.Rest(Time.deltaTime);

        if (dwellerNPC.needs.IsFullyRested())
        {
            _currentRestStation.RemoveRestingNPC(dwellerNPC);
            _currentRestStation = null;
            ReturnToWorkOrIdle();
        }
    }

    private void ReturnToWorkOrIdle()
    {
        if (!IsValidAndAlive())
        {
            return;
        }

        if (dwellerNPC.assignedWorkStation != null)
        {
            AssignToWork(dwellerNPC.assignedWorkStation);
        }
        else
        {
            ChangeState(NPCState.Idle);
        }
    }

    private void StartMoveCoroutine(IEnumerator _routine)
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }

        _moveCoroutine = StartCoroutine(_routine);
    }

    private void ReleaseCurrentNeedStations()
    {
        if (_currentRestStation != null && dwellerNPC != null)
        {
            _currentRestStation.RemoveRestingNPC(dwellerNPC);
            _currentRestStation = null;
        }

        if (_currentFoodStation != null && dwellerNPC != null)
        {
            _currentFoodStation.RemoveEatingNPC(dwellerNPC);
            _currentFoodStation = null;
        }

        if (_currentWaterStation != null && dwellerNPC != null)
        {
            _currentWaterStation.RemoveDrinkingNPC(dwellerNPC);
            _currentWaterStation = null;
        }
    }

    private bool IsValidAndAlive()
    {
        return dwellerNPC != null && !dwellerNPC.IsDead;
    }

    private void ChangeState(NPCState _newState)
    {
        if (_currentState == _newState)
        {
            return;
        }

        _currentState = _newState;
        _stateTimer = 0f;

        if (dwellerNPC == null || dwellerNPC.IsDead)
        {
            return;
        }

        switch (_newState)
        {
            case NPCState.Idle:
                dwellerNPC.currentState = DwellerState.Idle;
                break;
            case NPCState.MovingToWork:
                dwellerNPC.currentState = DwellerState.MovingToWork;
                break;
            case NPCState.Working:
                dwellerNPC.currentState = DwellerState.Working;
                break;
            case NPCState.Eating:
                dwellerNPC.currentState = DwellerState.Eating;
                break;
            case NPCState.Drinking:
                dwellerNPC.currentState = DwellerState.Drinking;
                break;
            case NPCState.Resting:
                dwellerNPC.currentState = DwellerState.Resting;
                break;
            default:
                // Estados de movimiento a necesidades se reflejan como Idle o MovingToWork (no existe enum específico).
                if (_newState == NPCState.MovingToEat || _newState == NPCState.MovingToDrink || _newState == NPCState.MovingToRest)
                {
                    dwellerNPC.currentState = DwellerState.Idle;
                }
                break;
        }
    }

    public bool IsWorking() => _currentState == NPCState.Working;
    public bool IsResting() => _currentState == NPCState.Resting;
    public bool IsEating() => _currentState == NPCState.Eating;
    public bool IsDrinking() => _currentState == NPCState.Drinking;

    public bool IsRecoveringOrGoingToRecover()
    {
        return _currentState == NPCState.MovingToEat ||
               _currentState == NPCState.Eating ||
               _currentState == NPCState.MovingToDrink ||
               _currentState == NPCState.Drinking ||
               _currentState == NPCState.MovingToRest ||
               _currentState == NPCState.Resting;
    }

    public bool IsAvailableForReassignment()
    {
        if (!IsValidAndAlive())
        {
            return false;
        }

        if (dwellerNPC != null && dwellerNPC.needs != null && dwellerNPC.needs.IsCritical())
        {
            return false;
        }

        return _currentState == NPCState.Idle ||
               _currentState == NPCState.MovingToWork ||
               _currentState == NPCState.Working;
    }


    public void RestartStateMachine()
    {
        if (dwellerNPC == null || dwellerNPC.IsDead)
        {
            return;
        }

        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        ReleaseCurrentNeedStations();
        _stateTimer = 0f;
        _workTimer = 0f;
        ChangeState(NPCState.Idle);

        if (_needsRoutine != null)
        {
            StopCoroutine(_needsRoutine);
        }
        _needsRoutine = StartCoroutine(NeedsCheckRoutine());
    }
}
