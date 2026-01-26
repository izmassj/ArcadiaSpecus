using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class CursorController : MonoBehaviour
{
    public static CursorController Instance { get; private set; }
    
    [Header("Cursor Settings")]
    [SerializeField] private RectTransform _cursorTransform;
    [SerializeField] private Canvas _cursorCanvas;
    [SerializeField] private float _cursorSpeed = 1000f;
    [SerializeField] private float _mouseMovementThreshold = 1f;
    [SerializeField] private float _mouseInactivityTimeout = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool _debugMode = false;
    
    private Vector2 _screenPosition;
    private PointerEventData _pointerEventData;
    private EventSystem _eventSystem;
    private Image _cursorImage;
    
    private GameObject _currentDragTarget = null;
    
    private Vector2 _lastMousePosition;
    private bool _usingRealMouse = false;
    private float _timeSinceMouseMovement = 0f;
    private bool _followOSCursor = false;
    
    // Windows API
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);
    
    private void Awake()
    {
        InitializeSingleton();
        InitializeComponents();
        InitializeCursor();
    }
    
    private void Start()
    {
        HideSystemCursor();
    }
    
    private void Update()
    {
        DetectRealMouseUsage();
        UpdateCursorMode();
        HandleWorldObjectClicks();
        UpdateOSCursorIfFollowing();
        ProcessUIInteractions();
    }
    
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void InitializeComponents()
    {
        _cursorCanvas.overrideSorting = true;
        _eventSystem = EventSystem.current;
        
        if (_eventSystem == null)
            Debug.LogError("CursorController: No EventSystem in scene.");
        
        _pointerEventData = new PointerEventData(_eventSystem);
        
        if (_cursorTransform != null)
            _cursorImage = _cursorTransform.GetComponent<Image>();
    }
    
    private void InitializeCursor()
    {
        _screenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        _lastMousePosition = Input.mousePosition;
    }
    
    private void DetectRealMouseUsage()
    {
        Vector2 currentMousePos = Input.mousePosition;
        Vector2 mouseDelta = currentMousePos - _lastMousePosition;
        
        if (mouseDelta.magnitude > _mouseMovementThreshold)
        {
            _usingRealMouse = true;
            _timeSinceMouseMovement = 0f;
        }
        else
        {
            _timeSinceMouseMovement += Time.deltaTime;
            
            if (HasControllerInput())
            {
                _usingRealMouse = false;
            }
            else if (_timeSinceMouseMovement > _mouseInactivityTimeout)
            {
                _usingRealMouse = false;
            }
        }
        
        _lastMousePosition = currentMousePos;
    }
    
    private bool HasControllerInput()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        
        return Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f ||
               Input.GetButtonDown("Submit") || Input.GetButtonDown("Cancel") ||
               Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
    }
    
    private void UpdateCursorMode()
    {
        if (_usingRealMouse)
        {
            EnableRealMouseMode();
        }
        else
        {
            EnableVirtualCursorMode();
            MoveVirtualCursor();
        }
    }
    
    private void EnableRealMouseMode()
    {
        if (_cursorImage != null && _cursorImage.enabled)
            _cursorImage.enabled = false;
        
        Cursor.visible = true;
        _screenPosition = Input.mousePosition;
    }
    
    private void EnableVirtualCursorMode()
    {
        if (_cursorImage != null && !_cursorImage.enabled)
            _cursorImage.enabled = true;
        
        Cursor.visible = false;
    }
    
    private void MoveVirtualCursor()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        
        if (Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f)
        {
            _screenPosition += new Vector2(x, y) * _cursorSpeed * Time.unscaledDeltaTime;
            _screenPosition.x = Mathf.Clamp(_screenPosition.x, 0, Screen.width);
            _screenPosition.y = Mathf.Clamp(_screenPosition.y, 0, Screen.height);
            
            if (_cursorTransform != null)
                _cursorTransform.position = _screenPosition;
        }
    }
    
    private void HandleWorldObjectClicks()
    {
        if (!Input.GetButtonDown("Submit") && !(_usingRealMouse && Input.GetMouseButtonDown(0))) 
            return;
        
        Ray ray = Camera.main.ScreenPointToRay(_screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) 
            return;
        
        ClickableMachine machine = hit.collider.GetComponent<ClickableMachine>();
        if (machine != null)
        {
            machine.OnMouseDown();
        }
    }
    
    private void ProcessUIInteractions()
    {
        _pointerEventData.position = _screenPosition;
        List<RaycastResult> results = RaycastAllCanvases();
        
        if (results.Count > 0)
        {
            HandleRaycastResults(results);
        }
        else
        {
            ClearSelection();
            HandleDragRelease();
        }
    }
    
    private List<RaycastResult> RaycastAllCanvases()
    {
        List<RaycastResult> results = new List<RaycastResult>();
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        
        foreach (var canvas in allCanvases)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.Raycast(_pointerEventData, results);
        }
        
        return results;
    }
    
    private void HandleRaycastResults(List<RaycastResult> results)
    {
        results.Sort((a, b) => b.depth.CompareTo(a.depth));
        GameObject topElement = results[0].gameObject;
        
        _eventSystem.SetSelectedGameObject(topElement);
        HandleDropdownInteraction(topElement);
        HandleDragInteraction(topElement);
    }
    
    private void HandleDropdownInteraction(GameObject target)
    {
        Dropdown dropdown = target.GetComponent<Dropdown>();
        if (dropdown == null) return;
        
        if (Input.GetButtonDown("Submit") || (_usingRealMouse && Input.GetMouseButtonDown(0)))
        {
            ExecuteEvents.ExecuteHierarchy(target, _pointerEventData, ExecuteEvents.pointerClickHandler);
        }
    }
    
    private void HandleDragInteraction(GameObject target)
    {
        bool isClickDown = Input.GetButtonDown("Submit") || (_usingRealMouse && Input.GetMouseButtonDown(0));
        bool isClickHeld = Input.GetButton("Submit") || (_usingRealMouse && Input.GetMouseButton(0));
        bool isClickUp = Input.GetButtonUp("Submit") || (_usingRealMouse && Input.GetMouseButtonUp(0));
        
        if (isClickDown && _currentDragTarget == null)
        {
            _currentDragTarget = target;
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerDownHandler);
        }
        
        if (_currentDragTarget != null && isClickHeld)
        {
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.dragHandler);
        }
        
        if (isClickUp && _currentDragTarget != null)
        {
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerClickHandler);
            _currentDragTarget = null;
        }
    }
    
    private void ClearSelection()
    {
        _eventSystem.SetSelectedGameObject(null);
    }
    
    private void HandleDragRelease()
    {
        bool isClickUp = Input.GetButtonUp("Submit") || (_usingRealMouse && Input.GetMouseButtonUp(0));
        
        if (_currentDragTarget != null && isClickUp)
        {
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerUpHandler);
            _currentDragTarget = null;
        }
    }
    
    private void UpdateOSCursorIfFollowing()
    {
        if (!_followOSCursor || !Application.isFocused || _usingRealMouse) 
            return;
        
        SetCursorPos((int)_screenPosition.x, (int)(Screen.height - _screenPosition.y));
    }
    
    public void StartFollowingOSCursor()
    {
        _followOSCursor = true;
    }
    
    public void StopFollowingOSCursor()
    {
        _followOSCursor = false;
    }
    
    public void ForceVirtualCursorMode()
    {
        _usingRealMouse = false;
        Cursor.visible = false;
        if (_cursorImage != null)
            _cursorImage.enabled = true;
    }
    
    public void ForceRealMouseMode()
    {
        _usingRealMouse = true;
        Cursor.visible = true;
        if (_cursorImage != null)
            _cursorImage.enabled = false;
    }
    
    public void HideSystemCursor()
    {
        Cursor.visible = false;
    }
    
    public void ShowSystemCursor()
    {
        Cursor.visible = true;
    }
    
}