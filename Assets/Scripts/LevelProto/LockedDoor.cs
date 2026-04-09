using DG.Tweening;
using UnityEngine;

public class LockedDoor : MonoBehaviour, ILevelResettable
{
    [SerializeField] private FollowKeyCollectible _requiredKey;
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private Vector3 _closedPosition;
    [SerializeField] private Vector3 _openPosition;
    [SerializeField] private float _openDuration = 0.55f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private string _playerTag = "Player";

    private Tween _moveTween;
    private bool _isOpen;

    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;


    private void Reset()
    {
        _doorTransform = transform;
    }

    private void Awake()
    {
        if (_doorTransform == null)
            _doorTransform = transform;

        _doorTransform.position = _closedPosition;

        _initialLocalPosition = transform.localPosition;
        _initialLocalRotation = transform.localRotation;
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isOpen || !other.CompareTag(_playerTag))
            return;

        if (_requiredKey == null || !_requiredKey.IsCollected)
            return;

        OpenDoor();
    }

    public void ResetLevelState()
    {
        transform.DOKill();
        transform.localPosition = _initialLocalPosition;
        transform.localRotation = _initialLocalRotation;
        _isOpen = false;
    }

    public void OpenDoor()
    {
        if (_isOpen || _doorTransform == null)
            return;

        _isOpen = true;
        _moveTween?.Kill();
        _moveTween = _doorTransform.DOMove(_openPosition, _openDuration).SetEase(_ease);

        Destroy(_requiredKey.gameObject);
    }

    public void CloseDoor()
    {
        if (!_isOpen || _doorTransform == null)
            return;

        _isOpen = false;
        _moveTween?.Kill();
        _moveTween = _doorTransform.DOMove(_closedPosition, _openDuration).SetEase(_ease);
    }
}
