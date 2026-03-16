using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RoomManager : MonoBehaviour 
{
    [Header("Parameters")]
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

    public CornerInteractionType cornerInteractionType;

    // Update is called once per frame
    void Update()
    {
        OnRoomBuild();
    }

    private void OnRoomBuild()
    {
        if (!_placed)
        {
            Debug.Log(_cornerDetector.EvaluateDetectedCorners());

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
}
