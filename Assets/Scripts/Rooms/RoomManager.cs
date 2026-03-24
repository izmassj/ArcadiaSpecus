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
    [SerializeField] private bool _skipFirstRailPointOnAppend = true;
    [SerializeField] private IntersectionSplineExpander _railOwner;
    [SerializeField] private bool _registeredToRail;

    [HideInInspector] public CornerInteractionType cornerInteractionType;

    private bool _focusedRoom;
    private GameObject _currentMachine;
    public GameObject _currentFloor;

    public IReadOnlyList<Transform> RailPoints => _railPoints;

    private void Start()
    {
        _focusedRoom = false;
        _currentMachine = null;
        _currentFloor = null;

        if (typeOfRoom == RoomKind.Intersection && _railOwner == null)
        {
            _railOwner = GetComponent<IntersectionSplineExpander>();
        }
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

                    if (_cornerDetector.GetForeignRoom() != null)
                    {
                        _currentFloor = _cornerDetector.GetForeignRoom();

                        if (_cornerDetector.GetForeignRoom().GetComponent<RoomManager>().typeOfRoom == RoomKind.Intersection)
                        {
                            _railOwner = _currentFloor.GetComponent<IntersectionSplineExpander>();
                        }
                    }

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
    }

    public bool HasRailPoints()
    {
        return _railPoints != null && _railPoints.Count > 0;
    }

    public bool SkipFirstRailPointOnAppend()
    {
        return _skipFirstRailPointOnAppend;
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
        if (_registeredToRail)
            return;

        if (typeOfRoom == RoomKind.Intersection)
        {
            if (_railOwner == null)
            {
                _railOwner = GetComponent<IntersectionSplineExpander>();
            }

            return;
        }

        if (_railOwner == null)
        {
            _railOwner = FindRailOwnerFromConnectedRoom();
        }

        if (_railOwner == null || !HasRailPoints())
            return;

        _railOwner.AppendRoom(this, _skipFirstRailPointOnAppend);
        _registeredToRail = true;
    }

    private IntersectionSplineExpander FindRailOwnerFromConnectedRoom()
    {
        if (_cornerDetector == null)
            return null;

        if (!_cornerDetector.TryGetSnapCorners(out CornerTrigger myCorner, out CornerTrigger otherCorner))
            return null;

        if (otherCorner == null)
            return null;

        RoomManager connectedRoom = otherCorner.transform.root.GetComponent<RoomManager>();

        if (connectedRoom == null)
            return null;

        IntersectionSplineExpander connectedRailOwner = connectedRoom.GetRailOwner();

        if (connectedRailOwner == null && connectedRoom.typeOfRoom == RoomKind.Intersection)
        {
            connectedRailOwner = connectedRoom.GetComponent<IntersectionSplineExpander>();
            connectedRoom.SetRailOwner(connectedRailOwner);
        }

        return connectedRailOwner;
    }
}
