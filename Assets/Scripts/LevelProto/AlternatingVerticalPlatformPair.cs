using DG.Tweening;
using UnityEngine;

public class AlternatingVerticalPlatformPair : MonoBehaviour
{
    [SerializeField] private Transform _platformA;
    [SerializeField] private Transform _platformB;

    [SerializeField] private Transform _platformABottom;
    [SerializeField] private Transform _platformATop;

    [SerializeField] private Transform _platformBBottom;
    [SerializeField] private Transform _platformBTop;

    [SerializeField] private float _secondsPerHalfCycle = 2f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private bool _startAAtBottom = true;

    private void Start()
    {
        if (_platformA == null || _platformB == null)
            return;

        _platformA.DOKill();
        _platformB.DOKill();

        _platformA.position = _startAAtBottom ? _platformABottom.position : _platformATop.position;
        _platformB.position = _startAAtBottom ? _platformBTop.position : _platformBBottom.position;

        Vector3 aFirstTarget = _startAAtBottom ? _platformATop.position : _platformABottom.position;
        Vector3 bFirstTarget = _startAAtBottom ? _platformBBottom.position : _platformBTop.position;

        _platformA
            .DOMove(aFirstTarget, _secondsPerHalfCycle)
            .SetEase(_ease)
            .SetLoops(-1, LoopType.Yoyo);

        _platformB
            .DOMove(bFirstTarget, _secondsPerHalfCycle)
            .SetEase(_ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        if (_platformA != null)
            _platformA.DOKill();

        if (_platformB != null)
            _platformB.DOKill();
    }
}