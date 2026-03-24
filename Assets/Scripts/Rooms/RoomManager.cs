using DG.Tweening;
using LineworkLite.FreeOutline;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private List<Transform> _railPoints = new();
    [SerializeField] private IntersectionSplineExpander _railOwner;

    [HideInInspector] public CornerInteractionType cornerInteractionType;

    private bool _focusedRoom;
    private bool _railRegistered;
    private GameObject _currentMachine;
    public GameObject originalIntersectionFloor;

    public int RailPointCount => _railPoints != null ? _railPoints.Count : 0;

    private void Awake()
    {
        if (typeOfRoom == RoomKind.Intersection && _railOwner == null)
        {
            _railOwner = GetComponent<IntersectionSplineExpander>();
        }
    }

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
        if (machine.GetComponent<MachineManager>() != null)
        {
            if (!_occupied)
            {
                _occupied = true;
                Instantiate(machine, _objectPlacePosition);
            }
        }

        if (_cornerDetector.GetForeignRoom() != null)
        {
            var foreign = _cornerDetector.GetForeignRoom();
            if (foreign.GetComponent<RoomManager>().originalIntersectionFloor != null)
            {

            }

            if (_cornerDetector.GetForeignRoom().GetComponent<RoomManager>().typeOfRoom == RoomKind.Intersection)
            {
                originalIntersectionFloor = _cornerDetector.GetForeignRoom();
                if (originalIntersectionFloor.GetComponent<IntersectionSplineExpander>() != null)
                {

                }
            }
        } else if (originalIntersectionFloor != null)
        {

        }
    }

    public Transform GetRailPoint(int index)
    {
        if (_railPoints == null || index < 0 || index >= _railPoints.Count)
            return null;

        return _railPoints[index];
    }

    public IntersectionSplineExpander GetRailOwner()
    {
        return _railOwner;
    }

    public void SetRailOwner(IntersectionSplineExpander railOwner)
    {
        _railOwner = railOwner;
    }

    public void ResolveRailAfterPlacement()
    {
        if (_railRegistered || typeOfRoom == RoomKind.Intersection)
            return;

        GameObject foreignRoomObject = _cornerDetector != null ? _cornerDetector.GetForeignRoom() : null;

        if (foreignRoomObject == null)
            return;

        RoomManager foreignRoomManager = foreignRoomObject.GetComponent<RoomManager>();

        if (foreignRoomManager == null)
            return;

        IntersectionSplineExpander owner = foreignRoomManager.GetRailOwner();

        if (owner == null && foreignRoomManager.typeOfRoom == RoomKind.Intersection)
        {
            owner = foreignRoomManager.GetComponent<IntersectionSplineExpander>();
        }

        if (owner == null)
            return;

        _railOwner = owner;

        if (RailPointCount == 0)
            return;

        bool reverseOrder = ShouldAppendReversed(owner);
        owner.AppendRoom(this, reverseOrder);
        _railRegistered = true;
    }

    private bool ShouldAppendReversed(IntersectionSplineExpander owner)
    {
        if (owner == null || RailPointCount == 0)
            return false;

        if (!owner.HasAnyKnot() || RailPointCount == 1)
            return false;

        Vector3 railEndPosition = owner.GetLastWorldPosition();
        Vector3 firstPointPosition = _railPoints[0].position;
        Vector3 lastPointPosition = _railPoints[RailPointCount - 1].position;

        float distanceToFirst = Vector3.Distance(railEndPosition, firstPointPosition);
        float distanceToLast = Vector3.Distance(railEndPosition, lastPointPosition);

        return distanceToLast < distanceToFirst;
    }
}
