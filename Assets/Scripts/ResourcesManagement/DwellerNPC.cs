using System.Collections;
using UnityEngine;

public class DwellerNPC : MonoBehaviour
{
    [Header("Configuración NPC")]
    public string dwellerName = "Habitante";
    public DwellerState currentState = DwellerState.Idle;

    [Header("Asignación")]
    public WorkStation assignedWorkStation;
    public bool isAssigned = false;

    [Header("Estadísticas")]
    public float workEfficiency = 1f;
    public float moveSpeed = 2f;

    [Header("Sistema de Necesidades")]
    public NPCNeeds needs = new NPCNeeds();
    [SerializeField] private NPCStateMachine stateMachine;

    [Header("Visual (opcional)")]
    [SerializeField] private Renderer[] _renderersToTint;
    [SerializeField] private Color _deadColor = Color.gray;

    [Header("Inicialización")]
    [SerializeField] private bool _randomizeNeedsOnStart = true;
    [SerializeField] private Vector2 _randomHungerRange = new Vector2(0f, 30f);
    [SerializeField] private Vector2 _randomThirstRange = new Vector2(0f, 30f);
    [SerializeField] private Vector2 _randomFatigueRange = new Vector2(0f, 20f);

    private Coroutine _fallbackMoveCoroutine;
    private bool _isInitialized;
    private bool _isHandlingDeath;
    private Collider[] _3dColliders;
    private Collider2D[] _2dColliders;
    private Color[] _originalColors;

    public bool IsDead { get; private set; }
    public System.Action<DwellerNPC> OnDeath;

    [System.Serializable]
    public class DwellerSaveData
    {
        public string dwellerName;
        public Vector3 position;
        public string assignedStationId;
        public float workEfficiency;
        public NPCNeeds needs;
        public bool isDead;
    }

    private void Awake()
    {
        CacheComponents();
        EnsureInitialized();
    }

    private void Start()
    {
        EnsureInitialized();

        if (_randomizeNeedsOnStart && needs != null)
        {
            // Solo randomizar si parece un NPC recién puesto (todo a 0).
            if (Mathf.Approximately(needs.hunger, 0f) && Mathf.Approximately(needs.thirst, 0f) && Mathf.Approximately(needs.fatigue, 0f))
            {
                needs.hunger = Random.Range(_randomHungerRange.x, _randomHungerRange.y);
                needs.thirst = Random.Range(_randomThirstRange.x, _randomThirstRange.y);
                needs.fatigue = Random.Range(_randomFatigueRange.x, _randomFatigueRange.y);
            }
        }
    }

    private void OnDestroy()
    {
        if (needs != null)
        {
            needs.OnDeath -= HandleDeath;
        }
    }

    private void CacheComponents()
    {
        if (stateMachine == null)
        {
            stateMachine = GetComponent<NPCStateMachine>();
        }

        _3dColliders = GetComponentsInChildren<Collider>(true);
        _2dColliders = GetComponentsInChildren<Collider2D>(true);

        if (_renderersToTint == null || _renderersToTint.Length == 0)
        {
            _renderersToTint = GetComponentsInChildren<Renderer>(true);
        }

        _originalColors = new Color[_renderersToTint != null ? _renderersToTint.Length : 0];
        for (int i = 0; i < _originalColors.Length; i++)
        {
            _originalColors[i] = _renderersToTint[i] != null && _renderersToTint[i].material != null
                ? _renderersToTint[i].material.color
                : Color.white;
        }
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (needs == null)
        {
            needs = new NPCNeeds();
        }

        needs.SetOwner(this);
        needs.OnDeath -= HandleDeath;
        needs.OnDeath += HandleDeath;
    }

    private void HandleDeath(DwellerNPC _deadNPC)
    {
        if (_deadNPC != this)
        {
            return;
        }

        if (IsDead || _isHandlingDeath)
        {
            return;
        }

        _isHandlingDeath = true;
        IsDead = true;
        currentState = DwellerState.Dead;

        if (_fallbackMoveCoroutine != null)
        {
            StopCoroutine(_fallbackMoveCoroutine);
            _fallbackMoveCoroutine = null;
        }

        if (assignedWorkStation != null)
        {
            assignedWorkStation.RemoveWorker(this);
            assignedWorkStation = null;
        }

        isAssigned = false;

        if (stateMachine != null)
        {
            stateMachine.enabled = false;
        }

        SetCollidersEnabled(false);
        ApplyDeadVisuals();

        // Comentario audio: aquí iría sonido/voz de muerte del NPC (si existe).
        OnDeath?.Invoke(this);

        _isHandlingDeath = false;
    }

    public void AssignToWorkStation(WorkStation _station)
    {
        if (IsDead)
        {
            return;
        }

        // Quitar del trabajo anterior.
        if (assignedWorkStation != null && assignedWorkStation != _station)
        {
            assignedWorkStation.RemoveWorker(this);
        }

        assignedWorkStation = _station;
        isAssigned = assignedWorkStation != null;

        if (!isAssigned)
        {
            currentState = DwellerState.Idle;
            return;
        }

        assignedWorkStation.AssignWorker(this);

        if (stateMachine != null && stateMachine.enabled)
        {
            stateMachine.AssignToWork(_station);
        }
        else
        {
            if (_fallbackMoveCoroutine != null)
            {
                StopCoroutine(_fallbackMoveCoroutine);
            }

            _fallbackMoveCoroutine = StartCoroutine(MoveToWorkStationFallback());
        }
    }

    private IEnumerator MoveToWorkStationFallback()
    {
        currentState = DwellerState.MovingToWork;

        if (assignedWorkStation == null)
        {
            yield break;
        }

        Vector3 _target = assignedWorkStation.GetWorkerPosition();
        while (!IsDead && assignedWorkStation != null && Vector3.Distance(transform.position, _target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (!IsDead)
        {
            currentState = DwellerState.Working;
        }
    }

    public bool IsRecovering()
    {
        return currentState == DwellerState.Eating || currentState == DwellerState.Drinking || currentState == DwellerState.Resting;
    }

    public bool NeedsRecovery()
    {
        return needs != null && needs.IsCritical();
    }

    public ResourceType GetMostCriticalNeed()
    {
        if (needs == null)
        {
            return ResourceType.Materials;
        }

        return needs.GetMostCriticalNeed();
    }

    public bool CanWorkEffectively()
    {
        // Mantener lógica compatible: si necesita recuperación, su rendimiento efectivo es 0.
        return !IsDead && assignedWorkStation != null && !NeedsRecovery();
    }

    public bool IsBusyRecoveringOrGoingToRecover()
    {
        if (IsDead)
        {
            return true;
        }

        if (stateMachine != null && stateMachine.enabled)
        {
            return stateMachine.IsRecoveringOrGoingToRecover();
        }

        return IsRecovering();
    }

    public bool CanBeReassignedToProductionNow()
    {
        if (IsDead)
        {
            return false;
        }

        if (needs != null && needs.IsCritical())
        {
            return false;
        }

        if (stateMachine != null && stateMachine.enabled)
        {
            return stateMachine.IsAvailableForReassignment();
        }

        return !IsRecovering();
    }


    public void Revive()
    {
        EnsureInitialized();

        IsDead = false;
        currentState = DwellerState.Idle;
        isAssigned = false;
        assignedWorkStation = null;

        if (needs != null)
        {
            needs.ResetNeeds();
            needs.SetOwner(this);
        }

        SetCollidersEnabled(true);
        RestoreAliveVisuals();

        if (stateMachine != null)
        {
            stateMachine.enabled = true;
            stateMachine.RestartStateMachine();
        }

        if (AssignmentManager.Instance != null)
        {
            AssignmentManager.Instance.NotifyNPCRevived(this);
        }
    }

    [ContextMenu("Debug Necesidades")]
    public void DebugNeeds()
    {
        if (needs == null)
        {
            Debug.Log($"{dwellerName} - needs NULL");
            return;
        }

        Debug.Log($"{dwellerName} | Estado:{currentState} | Muerto:{IsDead} | {needs.GetNeedsStatus()}");
    }

    [ContextMenu("💀 Matar NPC Instantáneamente")]
    public void KillInstantly()
    {
        if (IsDead)
        {
            return;
        }

        if (needs != null)
        {
            needs.KillInstantly();
        }
        else
        {
            HandleDeath(this);
        }
    }

    [ContextMenu("⚡ Acelerar Necesidades")]
    public void AccelerateNeeds()
    {
        if (!IsDead && needs != null)
        {
            needs.AccelerateNeedsWithoutKilling();
        }
    }

    public DwellerSaveData GetSaveData()
    {
        return new DwellerSaveData
        {
            dwellerName = dwellerName,
            position = transform.position,
            assignedStationId = assignedWorkStation != null ? assignedWorkStation.stationId : string.Empty,
            workEfficiency = workEfficiency,
            needs = needs,
            isDead = IsDead
        };
    }

    public void LoadData(DwellerSaveData _data)
    {
        if (_data == null)
        {
            return;
        }

        EnsureInitialized();

        dwellerName = string.IsNullOrWhiteSpace(_data.dwellerName) ? dwellerName : _data.dwellerName;
        transform.position = _data.position;
        workEfficiency = _data.workEfficiency;

        if (_data.needs != null)
        {
            needs.hunger = _data.needs.hunger;
            needs.thirst = _data.needs.thirst;
            needs.fatigue = _data.needs.fatigue;
            needs.criticalHunger = _data.needs.criticalHunger;
            needs.criticalThirst = _data.needs.criticalThirst;
            needs.criticalFatigue = _data.needs.criticalFatigue;
            needs.hungerRate = _data.needs.hungerRate;
            needs.thirstRate = _data.needs.thirstRate;
            needs.fatigueRate = _data.needs.fatigueRate;
            needs.eatingRecovery = _data.needs.eatingRecovery;
            needs.drinkingRecovery = _data.needs.drinkingRecovery;
            needs.restingRecovery = _data.needs.restingRecovery;
        }

        if (_data.isDead)
        {
            // Aplicar estado de muerto sin disparar múltiples eventos.
            IsDead = false;
            HandleDeath(this);
        }
        else
        {
            IsDead = false;
            currentState = DwellerState.Idle;
            SetCollidersEnabled(true);
            RestoreAliveVisuals();
        }
    }

    private void SetCollidersEnabled(bool _enabled)
    {
        if (_3dColliders != null)
        {
            for (int i = 0; i < _3dColliders.Length; i++)
            {
                if (_3dColliders[i] != null)
                {
                    _3dColliders[i].enabled = _enabled;
                }
            }
        }

        if (_2dColliders != null)
        {
            for (int i = 0; i < _2dColliders.Length; i++)
            {
                if (_2dColliders[i] != null)
                {
                    _2dColliders[i].enabled = _enabled;
                }
            }
        }
    }

    private void ApplyDeadVisuals()
    {
        if (_renderersToTint == null)
        {
            return;
        }

        for (int i = 0; i < _renderersToTint.Length; i++)
        {
            if (_renderersToTint[i] == null || _renderersToTint[i].material == null)
            {
                continue;
            }

            _renderersToTint[i].material.color = _deadColor;
        }
    }

    private void RestoreAliveVisuals()
    {
        if (_renderersToTint == null || _originalColors == null)
        {
            return;
        }

        for (int i = 0; i < _renderersToTint.Length && i < _originalColors.Length; i++)
        {
            if (_renderersToTint[i] == null || _renderersToTint[i].material == null)
            {
                continue;
            }

            _renderersToTint[i].material.color = _originalColors[i];
        }
    }
}
