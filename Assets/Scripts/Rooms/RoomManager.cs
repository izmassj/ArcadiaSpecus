using DG.Tweening;
using LineworkLite.FreeOutline;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class RoomManager : MonoBehaviour 
{
    [Header("Parameters")]
    [SerializeField] public RoomKind typeOfRoom;

    [Header("Post-Processing - Outline")]
    [SerializeField] public FreeOutlineSettings outlineSettings;
    [SerializeField] private int _outlineWidth;

    [Header("Positioning")]
    [SerializeField] private bool _placed;

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

    [HideInInspector] public CornerInteractionType cornerInteractionType;

    private bool _focusedRoom;

    private void Start()
    {
        _focusedRoom = false;
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
}
