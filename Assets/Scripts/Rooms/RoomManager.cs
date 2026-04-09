using DG.Tweening;
using LineworkLite.FreeOutline;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] public RoomKind typeOfRoom;

    [Header("Post-Processing - Outline")]
    [SerializeField] public FreeOutlineSettings outlineSettings;
    [SerializeField] private int _outlineWidth;

    [Header("Positioning")]
    [SerializeField] private Transform _objectPlacePosition;

    [Header("Refs")]
    [SerializeField] private CornerDetector _cornerDetector;

    [Header("Materials")]
    [SerializeField] Renderer _modelRenderer;

    [Header("Materials")]
    [SerializeField] private Material _onRoomBuildMat;
    [SerializeField] private List<Material> _roomMats;

    [Header("Material On Room Build - Colors")]
    [SerializeField] Color _buildableColor;
    [SerializeField] Color _nonBuildableColor;
    [SerializeField] Color _staticColor;

    [Header("Occupation")]
    [SerializeField] private bool _placed;
    [SerializeField] private bool _occupied;

    [Header("Rail")]
    [SerializeField] private List<Transform> _railPoints;
    [SerializeField] private IntersectionSplineExpander _railOwner;
    [SerializeField] private GameObject _originalIntersectionFloor;
    [SerializeField] private int _branchIndex = -1;
    [SerializeField] private bool _railRegistered;
    [SerializeField] private int _floorIndex;

    [HideInInspector] public CornerInteractionType cornerInteractionType;

    private bool _focusedRoom;
    private MachineManager _currentMachine;

    private void Start()
    {
        _focusedRoom = false;
        _currentMachine = GetComponentInChildren<MachineManager>();
    }

    private void Update()
    {
        OnRoomBuild();
    }

    private void OnRoomBuild()
    {
        if (!_placed)
        {
            switch (_cornerDetector.EvaluateDetectedCorners())
            {
                case CornerInteractionType.Buildable:
                    cornerInteractionType = CornerInteractionType.Buildable;
                    _modelRenderer.material.DOColor(_buildableColor, 0.5f);
                    break;
                case CornerInteractionType.NonBuildable:
                    cornerInteractionType = CornerInteractionType.NonBuildable;
                    _modelRenderer.material.DOColor(_nonBuildableColor, 0.5f);
                    break;
                case CornerInteractionType.Static:
                    cornerInteractionType = CornerInteractionType.Static;
                    _modelRenderer.material.DOColor(_staticColor, 0.5f);
                    break;
            }
        }
    }

    public void SetOnRoomBuildMaterial()
    {
        if (_modelRenderer != null)
            _modelRenderer.material = _onRoomBuildMat;
    }

    public void SetOriginalMaterial()
    {
        if (_roomMats.Count > 1)
        {
            for (int i = 0; i < _roomMats.Count; i++)
                _modelRenderer.materials[i] = _roomMats[i];
        }
        else
        {
            _modelRenderer.material = _roomMats[0];
        }
    }

    public bool GetPlaced()
    {
        return _placed;
    }

    public void SetPlaced()
    {
        _placed = !_placed;
    }

    public void ActivateOutline(LayerMask layerMask)
    {
        int layer = Mathf.RoundToInt(Mathf.Log(layerMask.value, 2));

        if (gameObject.transform.GetChild(0).gameObject.layer != layer)
            gameObject.transform.GetChild(0).gameObject.layer = layer;

        outlineSettings.Outlines[0].width = _outlineWidth;
    }

    public void DeactivateOutline(LayerMask layer)
    {
        if (gameObject.transform.GetChild(0).gameObject.layer != layer)
            gameObject.transform.GetChild(0).gameObject.layer = (int)layer;

        outlineSettings.Outlines[0].width = _outlineWidth;
    }

    public bool IsRoomFocused()
    {
        return _focusedRoom;
    }

    public bool IsRoomOccupied()
    {
        if (_currentMachine == null)
            _occupied = false;

        return _occupied;
    }

    public MachineManager GetCurrentMachine()
    {
        if (_currentMachine == null)
            _currentMachine = GetComponentInChildren<MachineManager>();

        if (_currentMachine == null)
            _occupied = false;

        return _currentMachine;
    }

    public void FocusRoom()
    {
        _focusedRoom = true;
        CameraBunkerManager.Instance.MoveCameraTo(transform.GetChild(1).transform);
    }

    public void UnFocusRoom()
    {
        _focusedRoom = false;
        CameraBunkerManager.Instance.MoveCameraToOriginalPos();
    }

    public void InstatiateMachine(GameObject machine)
    {
        InstantiateMachineAndGet(machine);
    }

    public MachineManager InstantiateMachineAndGet(GameObject machine)
    {
        if (machine == null)
            return null;

        MachineManager prefabMachine = machine.GetComponent<MachineManager>();

        if (prefabMachine == null)
            return null;

        if (IsRoomOccupied())
            return null;

        _occupied = true;
        GameObject machineInstance = Instantiate(machine, _objectPlacePosition);
        MachineManager machineManager = machineInstance.GetComponent<MachineManager>();

        if (machineManager != null)
        {
            machineManager.AssignOwnerRoom(this);
            _currentMachine = machineManager;
        }

        return machineManager;
    }

    public void ClearMachineInstant()
    {
        MachineManager machine = GetCurrentMachine();
        if (machine != null)
            Destroy(machine.gameObject);

        _currentMachine = null;
        _occupied = false;
    }

    public List<Transform> GetRailPoints()
    {
        return _railPoints;
    }

    public IntersectionSplineExpander GetRailOwner()
    {
        return _railOwner;
    }

    public GameObject GetOriginalIntersectionFloor()
    {
        return _originalIntersectionFloor;
    }

    public int GetBranchIndex()
    {
        return _branchIndex;
    }

    public int GetFloorIndex()
    {
        return _floorIndex;
    }

    public Vector3 GetRailCenterWorldPosition()
    {
        if (_railPoints != null && _railPoints.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            int validCount = 0;

            for (int i = 0; i < _railPoints.Count; i++)
            {
                if (_railPoints[i] == null)
                    continue;

                sum += _railPoints[i].position;
                validCount++;
            }

            if (validCount > 0)
                return sum / validCount;
        }

        if (_objectPlacePosition != null)
            return _objectPlacePosition.position;

        return transform.position;
    }

    public void RegisterToRail()
    {
        if (_railRegistered)
            return;

        if (_cornerDetector == null)
            return;

        GameObject foreignRoom = _cornerDetector.GetForeignRoom();

        if (foreignRoom == null)
            return;

        RoomManager foreignRoomManager = foreignRoom.GetComponent<RoomManager>();

        if (foreignRoomManager == null)
            return;

        if (foreignRoomManager.typeOfRoom == RoomKind.Intersection)
        {
            _originalIntersectionFloor = foreignRoom;
            _railOwner = foreignRoom.GetComponent<IntersectionSplineExpander>();

            if (_railOwner == null)
                return;

            _branchIndex = _railOwner.GetOrCreateBranchIndex(transform.position);
        }
        else
        {
            _originalIntersectionFloor = foreignRoomManager.GetOriginalIntersectionFloor();
            _railOwner = foreignRoomManager.GetRailOwner();
            _branchIndex = foreignRoomManager.GetBranchIndex();
        }

        if (_railOwner == null || _branchIndex < 0)
            return;

        if (_railPoints == null || _railPoints.Count == 0)
            return;

        _railOwner.AppendRoomToBranch(this, _branchIndex);
        _railRegistered = true;
    }
}
