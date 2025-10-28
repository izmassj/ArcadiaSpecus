using UnityEngine;
using UnityEngine.InputSystem;

public class ClickableObjectManager : MonoBehaviour
{
    [SerializeField] MeshRenderer rend;
    [SerializeField] Color hoverColor;
    [SerializeField] Color clickColor;

    private Color originalEmission;
    private bool isClicked;
    private bool isHovering;

    private void Start()
    {
        rend.material.EnableKeyword("_EMISSION");
        originalEmission = rend.material.GetColor("_EmissionColor");
    }

    void Update()
    {
        if (isClicked && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit) || hit.transform != transform)
            {
                // cursor released outside
                isClicked = false;
                rend.material.SetColor("_EmissionColor", originalEmission);
            }
        }
    }

    void OnMouseEnter()
    {
        isHovering = true;
        if (!isClicked)
            rend.material.SetColor("_EmissionColor", hoverColor);
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (!isClicked)
            rend.material.SetColor("_EmissionColor", originalEmission);
    }

    void OnMouseDown()
    {
        isClicked = true;
        rend.material.SetColor("_EmissionColor", clickColor);
    }

    void OnMouseUp()
    {
        if (isHovering)
        {
            isClicked = false;
            rend.material.SetColor("_EmissionColor", hoverColor);
        }
    }
}
