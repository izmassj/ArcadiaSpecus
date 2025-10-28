using UnityEngine;
using UnityEngine.InputSystem;


public class ClickableObjectManager : MonoBehaviour
{
    [SerializeField] MeshRenderer rend;
    [SerializeField] Color hoverColor;
    [SerializeField] Color clickColor;

    private InputAction clickInputAction; 

    private Color originalEmission;

    private bool isClicked;

    private void Awake()
    {

    }

    void Start()
    {
        rend.material.EnableKeyword("_EMISSION");

        originalEmission = rend.material.GetColor("_EmissionColor");
    }

    void OnMouseEnter()
    {
        if (!isClicked)
        {
            rend.material.SetColor("_EmissionColor", hoverColor);
        }
    }

    void OnMouseExit()
    {
        if (!isClicked)
        {
            rend.material.SetColor("_EmissionColor", originalEmission);
        }
    }

    void OnMouseDown()
    {
        isClicked = !isClicked;

        if (isClicked)
        {
            rend.material.SetColor("_EmissionColor", clickColor);
        }
    }

    private void OnMouseUp()
    {
        if (isClicked)
        {
            isClicked = !isClicked;
            rend.material.SetColor("_EmissionColor", hoverColor);
        }
    }
}
