using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ClickableObjectManager : MonoBehaviour
{
    // variables serializadas para customizacion de colores a parte de necesitar la referencia al MeshRenderer adyacente a la jerarquia
    [SerializeField] MeshRenderer rend;
    [SerializeField] Color hoverColor;
    [SerializeField] Color clickColor;

    // el valor de emission original del objeto
    private Color originalEmission;
    // variables booleanas para saber si esta clicando o haciendo hover
    private bool isClicked;
    private bool isHovering;

    private void Start()
    {
        // habilitamos la emission en el material del GameObject
        rend.material.EnableKeyword("_EMISSION");
        // guardamos el color original
        originalEmission = rend.material.GetColor("_EmissionColor");
    }

    void Update()
    {
        if (IsPlayerDragging()) return;

        /*
         * Si el jugador está clicando el objeto fuera del propio collider del objeto,
         * no dejes que tenga ni el color de hover ni el color de clicked.
         */
        if (isClicked && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit) || hit.transform != transform)
            {
                isClicked = false;
                rend.material.SetColor("_EmissionColor", originalEmission);
            }
        }
    }

    // logica restante al hacer click sobre el objeto

    void OnMouseEnter()
    {
        if (IsPlayerDragging()) return;

        isHovering = true;
        if (!isClicked)
            rend.material.SetColor("_EmissionColor", hoverColor);
    }

    void OnMouseExit()
    {
        if (IsPlayerDragging()) return;

        isHovering = false;
        if (!isClicked)
        {
            rend.material.SetColor("_EmissionColor", originalEmission);
        }
        else if (isClicked) 
        { 
            rend.material.SetColor("_EmissionColor", originalEmission);
        }
    }

    void OnMouseDown()
    {
        if (IsPlayerDragging()) return;

        isClicked = true;
        rend.material.SetColor("_EmissionColor", clickColor);
    }

    void OnMouseUp()
    {
        if (IsPlayerDragging()) return;

        if (isHovering)
        {
            isClicked = false;
            rend.material.SetColor("_EmissionColor", hoverColor);
        }
    }

    // chapuzeria

    private bool IsPlayerDragging()
    {
        if (PlayerManager.Instance.GetCurrentPlayerState() == PlayerManager.PlayerStates.BUNKER_DRAGGING && (isHovering || isClicked)) 
        {
            rend.material.SetColor("_EmissionColor", originalEmission);
            return true;
        }
        else if (PlayerManager.Instance.GetCurrentPlayerState() == PlayerManager.PlayerStates.BUNKER_DRAGGING)
        {
            return true;
        }
        return false;
    }
}
