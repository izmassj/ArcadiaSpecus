using DG.Tweening;
using UnityEngine;

public class TimedRotatingPlatform360 : MonoBehaviour
{
    [SerializeField] private Transform _platformRoot;
    [SerializeField] private Vector3 _rotationPerCycle = new Vector3(0f, 360f, 0f);
    [SerializeField] private float _secondsPerCycle = 4f;
    [SerializeField] private Ease _ease = Ease.Linear;
    [SerializeField] private bool _useLocalRotation = true;

    private void Awake()
    {
        if (_platformRoot == null)
            _platformRoot = transform;
    }

    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        if (_platformRoot == null)
            return;

        _platformRoot.DOKill();

        if (_useLocalRotation)
        {
            _platformRoot
                .DOLocalRotate(_rotationPerCycle, _secondsPerCycle, RotateMode.FastBeyond360)
                .SetEase(_ease)
                .SetLoops(-1, LoopType.Incremental);
        }
        else
        {
            _platformRoot
                .DORotate(_rotationPerCycle, _secondsPerCycle, RotateMode.FastBeyond360)
                .SetEase(_ease)
                .SetLoops(-1, LoopType.Incremental);
        }
    }

    private void OnDisable()
    {
        if (_platformRoot != null)
            _platformRoot.DOKill();
    }
}