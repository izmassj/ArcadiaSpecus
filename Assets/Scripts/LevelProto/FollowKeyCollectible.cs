using DG.Tweening;
using UnityEngine;

public class FollowKeyCollectible : MonoBehaviour, ILevelResettable
{
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private Transform _playerFollowTarget;
    [SerializeField] private Vector3 _followOffset = new Vector3(0f, 0.6f, -0.9f);
    [SerializeField] private float _followSmoothTime = 0.15f;
    [SerializeField] private float _rotateSpeed = 8f;
    [SerializeField] private Collider _pickupCollider;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private GameObject _pickupFeedback;

    private Transform _followRoot;
    private Vector3 _followVelocity;
    private bool _collected;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Transform _initialParent;
    private bool _isCollected;

    public bool IsCollected => _collected;

    private void Reset()
    {
        _pickupCollider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _initialParent = transform.parent;
    }

    private void LateUpdate()
    {
        if (!_collected || _followRoot == null)
            return;

        Vector3 targetPosition = _followRoot.TransformPoint(_followOffset);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _followVelocity, _followSmoothTime);

        Vector3 lookDir = (_followRoot.position - transform.position);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotateSpeed * Time.deltaTime);
        }
    }

    public void ResetLevelState()
    {
        _isCollected = false;
        transform.SetParent(_initialParent, true);
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected || !other.CompareTag(_playerTag))
            return;

        _followRoot = _playerFollowTarget != null ? _playerFollowTarget : other.transform;
        Collect();
    }

    private void Collect()
    {
        _collected = true;

        if (_pickupCollider != null)
            _pickupCollider.enabled = false;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        if (_pickupFeedback != null)
            _pickupFeedback.SetActive(true);

        transform.DOScale(transform.localScale * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
    }
}
