using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CursorController : MonoBehaviour
{
    public float speed = 600f; // cursor speed
    RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        Cursor.visible = false; // hide system cursor
    }

    void Update()
    {
        // Read joystick/dpad movement
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        // Move UI cursor
        rt.anchoredPosition += new Vector2(x, y) * speed * Time.deltaTime;

        // Click with (A) or Left Mouse
        if (Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
        {
            PointerEventData click = new PointerEventData(EventSystem.current);
            click.position = rt.position;
            ExecuteEvents.ExecuteHierarchy(
                EventSystem.current.currentSelectedGameObject,
                click,
                ExecuteEvents.pointerClickHandler
            );
        }
    }
}
