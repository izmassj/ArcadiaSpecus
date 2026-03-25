using System.Collections;
using UnityEngine;

public class NPCMachineTestSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject _npcPrefab;
    [SerializeField] private RoomManager _startRoom;
    [SerializeField] private MachineManager _targetMachine;

    [Header("Spawn")]
    [SerializeField] private bool _spawnOnStart = true;
    [SerializeField] private bool _destroyPreviousNpc = true;
    [SerializeField] private float _assignDelay = 0.1f;

    [Header("Debug")]
    [SerializeField] private GameObject _spawnedNpc;

    private Coroutine _spawnRoutine;

    private void Start()
    {
        if (_spawnOnStart)
            SpawnAndSendToMachine();
    }

    [ContextMenu("Spawn And Send To Machine")]
    public void SpawnAndSendToMachine()
    {
        if (_spawnRoutine != null)
            StopCoroutine(_spawnRoutine);

        _spawnRoutine = StartCoroutine(SpawnAndSendRoutine());
    }

    private IEnumerator SpawnAndSendRoutine()
    {
        if (_npcPrefab == null || _startRoom == null || _targetMachine == null)
        {
            Debug.LogWarning("NPCMachineTestSpawner: faltan referencias.", this);
            yield break;
        }

        if (_destroyPreviousNpc && _spawnedNpc != null)
            Destroy(_spawnedNpc);

        Vector3 spawnPosition = _startRoom.GetRailCenterWorldPosition();
        _spawnedNpc = Instantiate(_npcPrefab, spawnPosition, Quaternion.identity);

        NPCRailWalker railWalker = _spawnedNpc.GetComponent<NPCRailWalker>();
        NPCBunkerWorker bunkerWorker = _spawnedNpc.GetComponent<NPCBunkerWorker>();

        if (railWalker == null || bunkerWorker == null)
        {
            Debug.LogWarning("NPCMachineTestSpawner: el prefab necesita NPCRailWalker y NPCBunkerWorker.", _spawnedNpc);
            yield break;
        }

        railWalker.InitializeFromRoom(_startRoom);
        yield return null;

        if (_assignDelay > 0f)
            yield return new WaitForSeconds(_assignDelay);

        bunkerWorker.AssignMachine(_targetMachine);
        _spawnRoutine = null;
    }

    public GameObject GetSpawnedNpc()
    {
        return _spawnedNpc;
    }
}
