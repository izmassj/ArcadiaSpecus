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

    private Vector2 screenPos;
    private PointerEventData pointer;
    private EventSystem eventSystem;

    private GameObject currentDragTarget = null;

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
        }
        else
        {
            // Si ya existe una instancia, destruir esta
            Destroy(gameObject);
        }

        cursorCanvas.overrideSorting = true;

        eventSystem = EventSystem.current;
        if (eventSystem == null)
            Debug.LogError("VirtualCursorController: No EventSystem in scene.");

        // Start cursor in center
        screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);

        pointer = new PointerEventData(eventSystem);
    }

    void Update()
    {
        // --- Move virtual cursor ---
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        screenPos += new Vector2(x, y) * speed * Time.deltaTime;
        screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
        screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);

        if (cursorTransform)
            cursorTransform.position = screenPos;

        // --- Update OS cursor if following ---
        if (followOSCursor && Application.isFocused)
        {
            // Flip Y-axis for Windows screen coordinates
            SetCursorPos((int)screenPos.x, (int)(Screen.height - screenPos.y));
        }

        // --- Prepare pointer ---
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

        // --- Select topmost element ---
        if (results.Count > 0)
        {
            results.Sort((a, b) => b.depth.CompareTo(a.depth));
            GameObject top = results[0].gameObject;
            eventSystem.SetSelectedGameObject(top);

            Dropdown dropdown = top.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                if (Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
                    ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerClickHandler);
                return;
            }

            // --- Drag handling ---
            if ((Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0)) && currentDragTarget == null)
            {
                currentDragTarget = top;
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.pointerDownHandler);
            }

            if (currentDragTarget != null && (Input.GetButton("Submit") || Input.GetMouseButton(0)))
            {
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.dragHandler);
            }

            if ((Input.GetButtonUp("Submit") || Input.GetMouseButtonUp(0)) && currentDragTarget != null)
            {
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(currentDragTarget, pointer, ExecuteEvents.pointerClickHandler);
                currentDragTarget = null;
            }
        }
        else
        {
            eventSystem.SetSelectedGameObject(null);

            if (currentDragTarget != null && (Input.GetButtonUp("Submit") || Input.GetMouseButtonUp(0)))
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
}
