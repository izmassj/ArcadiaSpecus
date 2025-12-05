using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class CursorController : MonoBehaviour
{
    public float speed = 1000f;           // cursor speed in screen pixels/sec
    public Canvas canvas;                 // assign your canvas (optional - auto finds)
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private RectTransform rt;
    private Vector2 screenPos;            // cursor position in screen pixels
    private PointerEventData pointer;

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            Debug.LogError("GamepadUICursorFixed: No Canvas found in parents. Assign Canvas in inspector.");

        raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            Debug.LogError("GamepadUICursorFixed: Canvas needs a GraphicRaycaster.");

        eventSystem = EventSystem.current;
        if (eventSystem == null)
            Debug.LogError("GamepadUICursorFixed: No EventSystem in scene.");

        // start cursor roughly in center of screen
        screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);

        pointer = new PointerEventData(eventSystem);
        Cursor.visible = false; // hide OS cursor
    }

    void Update()
    {
        // read stick / dpad
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        // move screen-space cursor
        screenPos += new Vector2(x, y) * speed * Time.deltaTime;

        // clamp to screen
        screenPos.x = Mathf.Clamp(screenPos.x, 0f, Screen.width);
        screenPos.y = Mathf.Clamp(screenPos.y, 0f, Screen.height);

        // convert screen pos -> canvas local pos for UI image
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPoint);

        rt.anchoredPosition = localPoint;

        // prepare pointer for raycast
        pointer.position = screenPos;
        pointer.delta = Vector2.zero;
        pointer.pressPosition = screenPos;

        // raycast UI under cursor
        var results = new List<RaycastResult>();
        raycaster.Raycast(pointer, results);

        if (results.Count > 0)
        {
            GameObject top = results[0].gameObject;
            eventSystem.SetSelectedGameObject(top);

            // click on Submit (A) or left mouse button
            if (Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
            {
                ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerClickHandler);
            }
        }
        else
        {
            // nothing under cursor
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
