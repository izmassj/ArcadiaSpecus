using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MachinePlacementSystem : MonoBehaviour
{
    public static MachinePlacementSystem Instance;

    [Header("Machine Setup")]
    [SerializeField] private List<MachinePrefab> machinePrefabs;
    [SerializeField] private LayerMask machineLayerMask;
    [SerializeField] private float cornerCheckSize;
    [SerializeField] private float snapDistance;

    [Header("Machine Parameters")]
    public float machinePlacementDistance;

    [Header("Debug Corner Markers")]
    [SerializeField] private bool showCornerDebug;
    [SerializeField] private float cornerMarkerSize;
    [SerializeField] private GameObject cornerMarkerPrefab;

    private Transform[] cornerMarkers = new Transform[8];

    private GameObject currentMachine;
    private Collider possibleMachine;
    private bool isBuilding = false;
    private MachineKind machineKind;
    private bool canPlaceMachine = false;

    private Dictionary<MachineKind, GameObject> prefabDict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        prefabDict = new Dictionary<MachineKind, GameObject>();
        foreach (var p in machinePrefabs)
        {
            if (!prefabDict.ContainsKey(p.kind))
                prefabDict.Add(p.kind, p.prefab);
        }
    }

    private void Update()
    {
        if (!isBuilding || currentMachine == null) return;

        FollowMouse();
        TryDetectOtherMachines();

        if (showCornerDebug)
            UpdateCornerMarkerPositions();

        if ((Mouse.current.leftButton.wasPressedThisFrame || Input.GetButtonDown("Submit")) && canPlaceMachine)
        {
            PlaceMachine();
        }

    }

    public void SpawnMachinePrefab(MachineKind machine)
    {
        GameObject prefab = GetPrefab(machine);
        if (prefab == null) return;

        if (currentMachine != null)
            Destroy(currentMachine);

        Vector3 spawnPos = GetMouseWorldPosition();
        currentMachine = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Disable all machine scripts and behaviors during placement
        DisableMachineComponents(currentMachine);

        if (showCornerDebug)
            CreateCornerMarkers(currentMachine);

        BoxCollider collider = currentMachine.GetComponentInChildren<BoxCollider>();
        if (collider != null)
            collider.isTrigger = true;

        machineKind = machine;
        isBuilding = true;
        canPlaceMachine = false;

        SetMachineColor(Color.red);
    }

    private void DisableMachineComponents(GameObject machine)
    {
        // Disable all MonoBehaviour scripts except this one and transform
        MonoBehaviour[] scripts = machine.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != this && !(script is MachinePlacementSystem))
            {
                script.enabled = false;
            }
        }

        // Optionally disable other components that might cause issues during placement
        Rigidbody[] rigidbodies = machine.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = true; // Keep kinematic to prevent physics during placement
            }
        }

        // Disable any particle systems, audio sources, etc. that shouldn't play during placement
        ParticleSystem[] particleSystems = machine.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
                ps.Clear();
            }
        }

        AudioSource[] audioSources = machine.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.enabled = false;
            }
        }
    }

    private void EnableMachineComponents(GameObject machine)
    {
        // Enable all MonoBehaviour scripts
        MonoBehaviour[] scripts = machine.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != this && !(script is MachinePlacementSystem))
            {
                script.enabled = true;
            }
        }

        // Re-enable other components
        Rigidbody[] rigidbodies = machine.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
            {
                // You might want to set isKinematic based on your game's needs
                rb.isKinematic = false;
            }
        }

        // Re-enable particle systems, audio sources, etc.
        ParticleSystem[] particleSystems = machine.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Play();
            }
        }

        AudioSource[] audioSources = machine.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.enabled = true;
            }
        }
    }

    private void CreateCornerMarkers(GameObject machine)
    {
        if (cornerMarkerPrefab == null)
        {
            cornerMarkerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cornerMarkerPrefab.GetComponent<Collider>());
        }

        for (int i = 0; i < 8; i++)
        {
            GameObject marker = Instantiate(cornerMarkerPrefab, machine.transform);
            marker.name = "CornerMarker_" + i;
            marker.transform.localScale = Vector3.one * cornerMarkerSize;

            cornerMarkers[i] = marker.transform;
        }
    }

    private void UpdateCornerMarkerPositions()
    {
        if (currentMachine == null || cornerMarkers == null) return;

        BoxCollider col = currentMachine.GetComponentInChildren<BoxCollider>();
        Bounds b = col.bounds;

        Vector3[] corners = GetCorners(b);

        for (int i = 0; i < 8; i++)
        {
            if (cornerMarkers[i] != null)
                cornerMarkers[i].position = corners[i];
        }
    }

    public void PlaceMachine()
    {
        if (!isBuilding || currentMachine == null || !canPlaceMachine) return;

        // Enable all machine components before finalizing placement
        EnableMachineComponents(currentMachine);

        SetMachineColor(Color.white);

        BoxCollider collider = currentMachine.GetComponentInChildren<BoxCollider>();
        if (collider != null)
            collider.isTrigger = false;

        if (possibleMachine != null)
        {
            BoxCollider targetCollider = possibleMachine.GetComponent<BoxCollider>();
            BoxCollider selfCollider = currentMachine.GetComponentInChildren<BoxCollider>();

            Vector3[] targetCorners = GetCorners(targetCollider.bounds);
            Vector3[] selfCorners = GetCorners(selfCollider.bounds);

            float minDistance = float.MaxValue;
            Vector3 bestSelfCorner = Vector3.zero;
            Vector3 bestTargetCorner = Vector3.zero;

            foreach (var selfCorner in selfCorners)
            {
                foreach (var targetCorner in targetCorners)
                {
                    float dist = Vector3.Distance(selfCorner, targetCorner);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestSelfCorner = selfCorner;
                        bestTargetCorner = targetCorner;
                    }
                }
            }

            Vector3 offset = bestSelfCorner - currentMachine.transform.position;
            currentMachine.transform.position = bestTargetCorner - offset;
        }

        currentMachine = null;
        isBuilding = false;
        canPlaceMachine = false;
    }

    void FollowMouse()
    {
        Vector3 pos = GetMouseWorldPosition();
        pos.z = machinePlacementDistance;
        currentMachine.transform.position = pos;
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector2 cursorPos;

        // CHANGED: Use CursorController.Instance instead of CursorController.instance
        // CHANGED: Since UsingRealMouse is now private in the refactored CursorController,
        // we'll need to use Input.mousePosition as fallback
        if (CursorController.Instance != null)
        {
            // We'll use Input.mousePosition since we can't access the private cursor position
            cursorPos = Input.mousePosition;
        }
        else
        {
            cursorPos = Input.mousePosition;
        }

        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(cursorPos.x, cursorPos.y, 500f));
        world.z = 0f;
        return world;
    }

    void TryDetectOtherMachines()
    {
        BoxCollider self = currentMachine.GetComponentInChildren<BoxCollider>();
        Bounds b = self.bounds;

        Vector3[] corners = GetCorners(b);
        bool foundValidPlacement = false;

        foreach (var corner in corners)
        {
            Collider[] hits = Physics.OverlapBox(
                corner,
                Vector3.one * cornerCheckSize,
                Quaternion.identity,
                machineLayerMask
            );

            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(currentMachine.transform))
                    continue;

                if (CanPlaceNextToMachine(hit.bounds, b))
                {
                    possibleMachine = hit;
                    foundValidPlacement = true;
                    break;
                }
                else
                {
                    possibleMachine = null;
                }
            }

            if (foundValidPlacement)
                break;
        }

        canPlaceMachine = foundValidPlacement;
        SetMachineColor(foundValidPlacement ? Color.green : Color.red);
    }

    bool CanPlaceNextToMachine(Bounds target, Bounds self)
    {
        float verticalDistance = Mathf.Abs(self.center.y - target.center.y);
        if (verticalDistance > snapDistance) return false;

        float horizontalDistance = Mathf.Abs(self.center.x - target.center.x);
        float totalWidth = self.extents.x + target.extents.x;

        return horizontalDistance - totalWidth <= snapDistance;
    }

    Vector3[] GetCorners(Bounds b)
    {
        return new Vector3[]
        {
            new Vector3(b.min.x, b.min.y, b.min.z),
            new Vector3(b.min.x, b.min.y, b.max.z),
            new Vector3(b.min.x, b.max.y, b.min.z),
            new Vector3(b.min.x, b.max.y, b.max.z),
            new Vector3(b.max.x, b.min.y, b.min.z),
            new Vector3(b.max.x, b.min.y, b.max.z),
            new Vector3(b.max.x, b.max.y, b.min.z),
            new Vector3(b.max.x, b.max.y, b.max.z)
        };
    }

    void SetMachineColor(Color c)
    {
        foreach (Renderer r in currentMachine.GetComponentsInChildren<Renderer>())
            r.material.color = c;
    }

    public void SetCurrentMachine(GameObject newMachine)
    {
        if (newMachine == null)
        {
            Destroy(currentMachine);
            currentMachine = null;
            isBuilding = false;
            canPlaceMachine = false;
        }
        else
        {
            currentMachine = newMachine;
        }
    }

    GameObject GetPrefab(MachineKind kind)
    {
        return prefabDict.ContainsKey(kind) ? prefabDict[kind] : null;
    }
}