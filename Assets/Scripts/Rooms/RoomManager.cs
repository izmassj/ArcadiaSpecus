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

    [Header("NPC")]
    [SerializeField] private int _floorIndex;

    [HideInInspector] public CornerInteractionType cornerInteractionType;

    private bool _focusedRoom;
    private GameObject _currentMachine;

    private void Start()
    {
        _focusedRoom = false;
        _currentMachine = null;
    }

    void Update()
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
        {
            _modelRenderer.material = _onRoomBuildMat;
        }
    }

    public void SetOriginalMaterial()
    {
        if (_roomMats.Count > 1)
        {
            for (int i = 0; i < _roomMats.Count; i++)
            {
                _modelRenderer.materials[i] = _roomMats[i];
            }
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
        _placed = _placed ? false : true;
    }

    public void ActivateOutline(LayerMask layerMask)
    {
        int layer = Mathf.RoundToInt(Mathf.Log(layerMask.value, 2));

        if (gameObject.transform.GetChild(0).gameObject.layer != layer)
        {
            gameObject.transform.GetChild(0).gameObject.layer = layer;
        }

        outlineSettings.Outlines[0].width = _outlineWidth;
    }

    public void DeactivateOutline(LayerMask layer)
    {
        if (gameObject.transform.GetChild(0).gameObject.layer != layer)
        {
            gameObject.transform.GetChild(0).gameObject.layer = (int)layer;
        }

        outlineSettings.Outlines[0].width = _outlineWidth;
    }

    public bool IsRoomFocused()
    {
        return _focusedRoom;
    }

    public bool IsRoomOccupied()
    {
        return _occupied;
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
        if (machine == null)
            return;

        if (machine.GetComponent<MachineManager>() != null)
        {
            if (!_occupied)
            {
                _occupied = true;
                GameObject instance = Instantiate(machine, _objectPlacePosition);
                _currentMachine = instance;

                MachineManager machineManager = instance.GetComponent<MachineManager>();

                if (machineManager != null)
                    machineManager.AssignOwnerRoom(this);
            }
        }
    }

    public List<Transform> GetRailPoints()
    {
        return _railPoints;
    }

    public Vector3 GetRailCenterWorldPosition()
    {
        if (_railPoints == null || _railPoints.Count == 0)
            return transform.position;

        int centerIndex = _railPoints.Count / 2;
        centerIndex = Mathf.Clamp(centerIndex, 0, _railPoints.Count - 1);

        if (_railPoints[centerIndex] == null)
            return transform.position;

        return _railPoints[centerIndex].position;
    }

    public Transform GetObjectPlacePosition()
    {
        return _objectPlacePosition;
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

    public GameObject GetCurrentMachine()
    {
        return _currentMachine;
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
