using DG.Tweening;
using UnityEngine;

public class MovingWall : MonoBehaviour, ILevelResettable
{
    [SerializeField] private Transform _wallTransform;
    [SerializeField] private Vector3 _raisedPosition;
    [SerializeField] private Vector3 _loweredPosition;
    [SerializeField] private float _moveDuration = 0.5f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private bool _startsLowered;

    private Vector3 _initialLocalPosition;

    private Tween _moveTween;

    private void Reset()
    {
        _wallTransform = transform;
    }

    private void Awake()
    {
        if (_wallTransform == null)
            _wallTransform = transform;

        _wallTransform.position = _startsLowered ? _loweredPosition : _raisedPosition;

        _initialLocalPosition = transform.localPosition;
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }

    public void ResetLevelState()
    {
        transform.DOKill();
        transform.localPosition = _initialLocalPosition;
    }

    public void SetLowered(bool lowered)
    {
        if (_wallTransform == null)
            return;

        _moveTween?.Kill();
        Vector3 target = lowered ? _loweredPosition : _raisedPosition;
        _moveTween = _wallTransform.DOMove(target, _moveDuration).SetEase(_ease);
    }
}
