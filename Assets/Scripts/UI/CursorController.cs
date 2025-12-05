using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class CursorController : MonoBehaviour
{
    public static CursorController instance;

    [Header("Cursor Settings")]
    public RectTransform cursorTransform; // Your cursor GameObject
    public Canvas cursorCanvas;
    public float speed = 1000f;           // Cursor speed in pixels/sec

    public Vector2 screenPos;
    private PointerEventData pointer;
    private EventSystem eventSystem;

    private GameObject currentDragTarget = null;

    // Para detectar movimiento del ratón real
    public Vector2 lastMousePosition;
    public bool usingRealMouse = false;
    public float mouseMovementThreshold = 1f;
    public float timeSinceMouseMovement = 0f;
    public float mouseInactivityTimeout = 0.5f; // Tiempo antes de volver al cursor virtual

    // Componente de imagen para mostrar/ocultar el cursor virtual
    private Image cursorImage;

    // --- OS cursor follow flag ---
    private bool followOSCursor = false;

    // --- Windows API ---
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    void Awake()
    {
        // Singleton pattern - evitar duplicados
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe una instancia, destruir esta
            Destroy(gameObject);
            return;
        }

        cursorCanvas.overrideSorting = true;

        eventSystem = EventSystem.current;
        if (eventSystem == null)
            Debug.LogError("VirtualCursorController: No EventSystem in scene.");

        // Start cursor in center
        screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        lastMousePosition = Input.mousePosition;

        pointer = new PointerEventData(eventSystem);

        // Obtener componente Image del cursor
        if (cursorTransform != null)
            cursorImage = cursorTransform.GetComponent<Image>();

        // Ocultar cursor del sistema inicialmente
        Cursor.visible = false;
    }

    void Start()
    {
        // Asegurar que el cursor está oculto
        Cursor.visible = false;
        // Lock the cursor to center if needed
        // Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        // --- Detectar si se está usando el ratón real ---
        DetectRealMouseUsage();

        // --- Actualizar posición y visibilidad según el modo ---
        if (usingRealMouse)
        {
            // Modo ratón real: usar posición del ratón real, ocultar imagen del cursor virtual
            if (cursorImage != null && cursorImage.enabled)
                cursorImage.enabled = false;

            // Mostrar cursor del sistema
            Cursor.visible = true;

            // Actualizar posición de pantalla desde el ratón real
            screenPos = Input.mousePosition;
        }
        else
        {
            // Modo cursor virtual: mostrar imagen del cursor virtual, ocultar cursor del sistema
            if (cursorImage != null && !cursorImage.enabled)
                cursorImage.enabled = true;

            // Ocultar cursor del sistema
            Cursor.visible = false;

            // --- Mover cursor virtual ---
            MoveVirtualCursor();
        }

        // --- Detect clicks on world objects ---
        if (Input.GetButtonDown("Submit") || (usingRealMouse && Input.GetMouseButtonDown(0)))
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ClickableMachine machine = hit.collider.GetComponent<ClickableMachine>();
                if (machine != null)
                {
                    machine.OnMouseDown(); // Manually trigger the click
                }
            }
        }

        // --- Update OS cursor if following ---
        if (followOSCursor && Application.isFocused && !usingRealMouse)
        {
            // Flip Y-axis for Windows screen coordinates
            SetCursorPos((int)screenPos.x, (int)(Screen.height - screenPos.y));
        }

        // --- Preparar eventos de puntero ---
        pointer.position = screenPos;

        // --- Raycast all canvases ---
        List<RaycastResult> results = new List<RaycastResult>();
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (var c in allCanvases)
        {
            GraphicRaycaster gr = c.GetComponent<GraphicRaycaster>();
            if (gr != null)
                gr.Raycast(pointer, results);
        }

        // --- Procesar interacciones ---
        ProcessInteractions(results);
    }

    private void DetectRealMouseUsage()
    {
        Vector2 currentMousePos = Input.mousePosition;
        Vector2 mouseDelta = currentMousePos - lastMousePosition;

        // Verificar si el ratón se ha movido significativamente
        if (mouseDelta.magnitude > mouseMovementThreshold)
        {
            usingRealMouse = true;
            timeSinceMouseMovement = 0f;
        }
        else
        {
            timeSinceMouseMovement += Time.deltaTime;

            // Si hay input del teclado/joystick, cambiar a modo virtual
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            if (Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f ||
                Input.GetButtonDown("Submit") || Input.GetButtonDown("Cancel") ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                usingRealMouse = false;
            }
            // Cambiar automáticamente a virtual después de inactividad
            else if (timeSinceMouseMovement > mouseInactivityTimeout)
            {
                usingRealMouse = false;
            }
        }

        lastMousePosition = currentMousePos;
    }

    private void MoveVirtualCursor()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        // Solo mover el cursor virtual si hay input
        if (Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f)
        {
            screenPos += new Vector2(x, y) * speed * Time.deltaTime;
            screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
            screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);

            if (cursorTransform)
                cursorTransform.position = screenPos;
        }
    }

    private void ProcessInteractions(List<RaycastResult> results)
    {
        // --- Select topmost element ---
        if (results.Count > 0)
        {
            results.Sort((a, b) => b.depth.CompareTo(a.depth));
            GameObject top = results[0].gameObject;
            eventSystem.SetSelectedGameObject(top);

            Dropdown dropdown = top.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                if (Input.GetButtonDown("Submit") || (usingRealMouse && Input.GetMouseButtonDown(0)))
                    ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerClickHandler);
                return;
            }

            // --- Drag handling ---
            if ((Input.GetButtonDown("Submit") || (usingRealMouse && Input.GetMouseButtonDown(0))) && currentDragTarget == null)
            {
                currentDragTarget = top;
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.pointerDownHandler);
            }

            if (currentDragTarget != null && (Input.GetButton("Submit") || (usingRealMouse && Input.GetMouseButton(0))))
            {
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.dragHandler);
            }

            if ((Input.GetButtonUp("Submit") || (usingRealMouse && Input.GetMouseButtonUp(0))) && currentDragTarget != null)
            {
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.pointerClickHandler);
                currentDragTarget = null;
            }
        }
        else
        {
            eventSystem.SetSelectedGameObject(null);

            if (currentDragTarget != null && (Input.GetButtonUp("Submit") || (usingRealMouse && Input.GetMouseButtonUp(0))))
            {
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.pointerUpHandler);
                currentDragTarget = null;
            }
        }
    }

    // --- Public functions to control OS cursor following ---
    public void StartFollowingOSCursor()
    {
        followOSCursor = true;
    }

    public void StopFollowingOSCursor()
    {
        followOSCursor = false;
    }

    // Métodos para forzar modo
    public void ForceVirtualCursorMode()
    {
        usingRealMouse = false;
        Cursor.visible = false;
        if (cursorImage != null)
            cursorImage.enabled = true;
    }

    public void ForceRealMouseMode()
    {
        usingRealMouse = true;
        Cursor.visible = true;
        if (cursorImage != null)
            cursorImage.enabled = false;
    }
}