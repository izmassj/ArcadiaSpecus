using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIButtonsEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    private bool _isHovering;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        onHoverExit?.Invoke();
    }

    public bool IsHovering()
    {
        return _isHovering;
    }
}
