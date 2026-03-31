using System.Collections;
using UnityEngine;

public class IntersectionElevator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _elevatorAnimator;

    [Header("Animation")]
    [SerializeField] private string _openTriggerName;
    [SerializeField] private string _closeTriggerName;
    [SerializeField] private string _openStateName;
    [SerializeField] private string _closeStateName;
    [SerializeField] private int _animatorLayer;

    private void Awake()
    {
        ResolveAnimator();
    }

    public void OpenDoors()
    {
        ResolveAnimator();

        if (_elevatorAnimator == null)
            return;

        _elevatorAnimator.ResetTrigger(_closeTriggerName);
        _elevatorAnimator.SetTrigger(_openTriggerName);
    }

    public void CloseDoors()
    {
        ResolveAnimator();

        if (_elevatorAnimator == null)
            return;

        _elevatorAnimator.ResetTrigger(_openTriggerName);
        _elevatorAnimator.SetTrigger(_closeTriggerName);
    }

    public IEnumerator PlayOpenAndWait()
    {
        OpenDoors();
        yield return WaitForStateFinished(_openStateName);
    }

    public IEnumerator PlayCloseAndWait()
    {
        CloseDoors();
        yield return WaitForStateFinished(_closeStateName);
    }

    public IEnumerator WaitForOpenFinished()
    {
        yield return WaitForStateFinished(_openStateName);
    }

    public IEnumerator WaitForCloseFinished()
    {
        yield return WaitForStateFinished(_closeStateName);
    }

    private IEnumerator WaitForStateFinished(string stateName)
    {
        ResolveAnimator();

        if (_elevatorAnimator == null || string.IsNullOrWhiteSpace(stateName))
            yield break;

        yield return null;

        while (!_elevatorAnimator.GetCurrentAnimatorStateInfo(_animatorLayer).IsName(stateName))
            yield return null;

        while (_elevatorAnimator.GetCurrentAnimatorStateInfo(_animatorLayer).IsName(stateName) &&
               _elevatorAnimator.GetCurrentAnimatorStateInfo(_animatorLayer).normalizedTime < 1f)
        {
            yield return null;
        }
    }

    private void ResolveAnimator()
    {
        if (_elevatorAnimator != null)
            return;

        _elevatorAnimator = GetComponent<Animator>();

        if (_elevatorAnimator == null)
            _elevatorAnimator = GetComponentInChildren<Animator>(true);
    }
}
