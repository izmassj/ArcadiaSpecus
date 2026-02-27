using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cursor híbrido:
/// - Ratón real cuando se detecta mouse
/// - Cursor virtual cuando se usa mando/teclado
/// Compatible con UI y con clicks en ClickableMachine.
/// </summary>
public class CursorController : MonoBehaviour
{
    public static CursorController Instance { get; private set; }

    [Header("Cursor Settings")]
    [SerializeField] private RectTransform _cursorTransform;
    [SerializeField] private Canvas _cursorCanvas;
    [SerializeField] private float _cursorSpeed = 1000f;
    [SerializeField] private float _mouseMovementThreshold = 1f;
    [SerializeField] private float _mouseInactivityTimeout = 0.5f;

    [Header("Input (New Input System)")]
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private string _navigateActionName = "Navigate";
    [SerializeField] private string _pointActionName = "Point";
    [SerializeField] private string _clickActionName = "Click";
    [SerializeField] private string _submitActionName = "Submit";

    [Header("Debug")]
    [SerializeField] private bool _debugMode = false;

    private Vector2 _screenPosition;
    private PointerEventData _pointerEventData;
    private EventSystem _eventSystem;
    private Image _cursorImage;

    private GameObject _currentDragTarget;
    private Vector2 _lastMousePosition;
    private bool _usingRealMouse = true;
    private float _timeSinceMouseMovement;
    private bool _followOSCursor;

    private bool _isPointerOverUI;

    private InputAction _navigateAction;
    private InputAction _pointAction;
    private InputAction _clickAction;
    private InputAction _submitAction;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);
#endif

    private void Awake()
    {
        InitializeSingleton();
        InitializeComponents();
        InitializeCursor();
        CacheInputActions();
    }

    private void Start()
    {
        SceneFlowManager.EnsureInstance();
        InputDeviceTracker.EnsureInstance();

        // Empezamos en modo ratón real si hay ratón.
        ForceRealMouseMode();
    }

    private void OnEnable()
    {
        CacheInputActions();
    }

    private void Update()
    {
        RefreshEventSystemIfNeeded();

        DetectRealMouseUsage();
        UpdateCursorMode();
        HandleWorldObjectClicks();

        if (!_usingRealMouse)
        {
            // Solo simulamos interacción UI cuando usamos cursor virtual.
            ProcessUIInteractions();
        }
        else
        {
            _isPointerOverUI = _eventSystem != null && _eventSystem.IsPointerOverGameObject();
        }

        UpdateOSCursorIfFollowing();
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
        if (_cursorCanvas != null)
            _cursorCanvas.overrideSorting = true;

        _eventSystem = EventSystem.current;
        _pointerEventData = _eventSystem != null ? new PointerEventData(_eventSystem) : null;

        if (_cursorTransform != null)
            _cursorImage = _cursorTransform.GetComponent<Image>();
    }

    private void InitializeCursor()
    {
        _screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (Mouse.current != null)
            _lastMousePosition = Mouse.current.position.ReadValue();
        else
            _lastMousePosition = _screenPosition;

        if (_cursorTransform != null)
            _cursorTransform.position = _screenPosition;
    }

    private void RefreshEventSystemIfNeeded()
    {
        if (_eventSystem == null || EventSystem.current != _eventSystem)
        {
            _eventSystem = EventSystem.current;
            _pointerEventData = _eventSystem != null ? new PointerEventData(_eventSystem) : null;
        }
    }

    private void CacheInputActions()
    {
        if (_playerInput == null)
            _playerInput = FindObjectOfType<PlayerInput>();

        _navigateAction = null;
        _pointAction = null;
        _clickAction = null;
        _submitAction = null;

        if (_playerInput == null || _playerInput.actions == null)
            return;

        _navigateAction = _playerInput.actions.FindAction(_navigateActionName, throwIfNotFound: false);
        _pointAction = _playerInput.actions.FindAction(_pointActionName, throwIfNotFound: false);
        _clickAction = _playerInput.actions.FindAction(_clickActionName, throwIfNotFound: false);
        _submitAction = _playerInput.actions.FindAction(_submitActionName, throwIfNotFound: false);
    }

    private void DetectRealMouseUsage()
    {
        bool mouseMoved = false;

        if (Mouse.current != null)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePos - _lastMousePosition;

            if (mouseDelta.magnitude > _mouseMovementThreshold)
            {
                mouseMoved = true;
                _lastMousePosition = currentMousePos;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
                mouseMoved = true;
        }

        if (mouseMoved)
        {
            _usingRealMouse = true;
            _timeSinceMouseMovement = 0f;
            return;
        }

        _timeSinceMouseMovement += Time.unscaledDeltaTime;

        if (HasControllerNavigationInput())
        {
            _usingRealMouse = false;
            return;
        }

        if (_timeSinceMouseMovement > _mouseInactivityTimeout && Gamepad.current != null)
        {
            // Si hay mando y el ratón lleva un rato quieto, dejamos el cursor virtual disponible.
            _usingRealMouse = false;
        }
    }

    private bool HasControllerNavigationInput()
    {
        Vector2 nav = GetNavigationInput();

        if (nav.sqrMagnitude > 0.0001f)
            return true;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.0001f)
            {
                return true;
            }
        }

        if (Keyboard.current != null)
        {
            // Teclas típicas de navegación UI también fuerzan cursor virtual.
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.downArrowKey.wasPressedThisFrame ||
                Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateCursorMode()
    {
        if (_usingRealMouse)
        {
            EnableRealMouseMode();

            if (Mouse.current != null)
            {
                _screenPosition = Mouse.current.position.ReadValue();
            }
        }
        else
        {
            EnableVirtualCursorMode();
            MoveVirtualCursor();
        }

        if (_cursorTransform != null)
            _cursorTransform.position = _screenPosition;
    }

    private void EnableRealMouseMode()
    {
        Cursor.visible = true;

        if (_cursorImage != null)
            _cursorImage.enabled = false;
    }

    private void EnableVirtualCursorMode()
    {
        Cursor.visible = false;

        if (_cursorImage != null)
            _cursorImage.enabled = true;
    }

    private void MoveVirtualCursor()
    {
        Vector2 nav = GetNavigationInput();

        if (nav.sqrMagnitude <= 0.0001f)
            return;

        _screenPosition += nav * (_cursorSpeed * Time.unscaledDeltaTime);
        _screenPosition.x = Mathf.Clamp(_screenPosition.x, 0f, Screen.width);
        _screenPosition.y = Mathf.Clamp(_screenPosition.y, 0f, Screen.height);
    }

    private Vector2 GetNavigationInput()
    {
        if (_navigateAction != null && _navigateAction.enabled)
        {
            // Puede venir de stick, dpad o teclado según bindings.
            return _navigateAction.ReadValue<Vector2>();
        }

        // Fallback New Input System puro (sin Input Manager antiguo).
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.0001f)
                return stick;

            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            if (dpad.sqrMagnitude > 0.0001f)
                return dpad;
        }

        if (Keyboard.current != null)
        {
            Vector2 keyboard = Vector2.zero;
            if (Keyboard.current.leftArrowKey.isPressed) keyboard.x -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) keyboard.x += 1f;
            if (Keyboard.current.downArrowKey.isPressed) keyboard.y -= 1f;
            if (Keyboard.current.upArrowKey.isPressed) keyboard.y += 1f;
            return keyboard;
        }

        return Vector2.zero;
    }

    private bool GetClickDownThisFrame()
    {
        if (_usingRealMouse && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (_clickAction != null && _clickAction.enabled && _clickAction.WasPressedThisFrame())
            return true;

        if (_submitAction != null && _submitAction.enabled && _submitAction.WasPressedThisFrame())
            return true;

        if (!_usingRealMouse && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            return true;

        if (!_usingRealMouse && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            return true;

        return false;
    }

    private bool GetClickHeld()
    {
        if (_usingRealMouse && Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;

        if (_clickAction != null && _clickAction.enabled && _clickAction.IsPressed())
            return true;

        if (_submitAction != null && _submitAction.enabled && _submitAction.IsPressed())
            return true;

        if (!_usingRealMouse && Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
            return true;

        if (!_usingRealMouse && Keyboard.current != null &&
            (Keyboard.current.enterKey.isPressed || Keyboard.current.spaceKey.isPressed))
            return true;

        return false;
    }

    private bool GetClickUpThisFrame()
    {
        if (_usingRealMouse && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            return true;

        if (_clickAction != null && _clickAction.enabled && _clickAction.WasReleasedThisFrame())
            return true;

        if (_submitAction != null && _submitAction.enabled && _submitAction.WasReleasedThisFrame())
            return true;

        if (!_usingRealMouse && Gamepad.current != null && Gamepad.current.buttonSouth.wasReleasedThisFrame)
            return true;

        if (!_usingRealMouse && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasReleasedThisFrame || Keyboard.current.spaceKey.wasReleasedThisFrame))
            return true;

        return false;
    }

    private void HandleWorldObjectClicks()
    {
        if (!GetClickDownThisFrame())
            return;

        // Evita clicks "a través" de la UI
        bool pointerOverUiNow = _usingRealMouse
            ? (_eventSystem != null && _eventSystem.IsPointerOverGameObject())
            : _isPointerOverUI;

        if (pointerOverUiNow)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(_screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        ClickableMachine machine = hit.collider.GetComponent<ClickableMachine>();
        if (machine != null)
        {
            machine.OnMouseDown(); // Se mantiene la API existente del script de máquina.
            // Aquí iría SFX/VFX de recolección al conectarlo.
        }
    }

    private void ProcessUIInteractions()
    {
        if (_eventSystem == null || _pointerEventData == null)
            return;

        _pointerEventData.Reset();
        _pointerEventData.position = _screenPosition;
        _pointerEventData.button = PointerEventData.InputButton.Left;

        List<RaycastResult> results = RaycastAllCanvases();
        _isPointerOverUI = results.Count > 0;

        if (!_isPointerOverUI)
        {
            ClearSelection();
            HandleDragReleaseWithoutTarget();
            return;
        }

        GameObject topElement = results[0].gameObject;

        // Selección UI (útil para navegación visual)
        _eventSystem.SetSelectedGameObject(topElement);

        bool clickDown = GetClickDownThisFrame();
        bool clickHeld = GetClickHeld();
        bool clickUp = GetClickUpThisFrame();

        if (clickDown && _currentDragTarget == null)
        {
            _currentDragTarget = topElement;
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.beginDragHandler);
        }

        if (_currentDragTarget != null && clickHeld)
        {
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.dragHandler);
        }

        if (clickUp && _currentDragTarget != null)
        {
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerClickHandler);
            _currentDragTarget = null;
        }
    }

    private List<RaycastResult> RaycastAllCanvases()
    {
        List<RaycastResult> results = new List<RaycastResult>();
        GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>(true);

        for (int i = 0; i < raycasters.Length; i++)
        {
            GraphicRaycaster raycaster = raycasters[i];
            if (raycaster == null || !raycaster.isActiveAndEnabled)
                continue;

            raycaster.Raycast(_pointerEventData, results);
        }

        results.Sort((a, b) => b.depth.CompareTo(a.depth));
        return results;
    }

    private void ClearSelection()
    {
        if (_eventSystem != null)
            _eventSystem.SetSelectedGameObject(null);
    }

    private void HandleDragReleaseWithoutTarget()
    {
        if (_currentDragTarget == null)
            return;

        if (!GetClickUpThisFrame())
            return;

        ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.endDragHandler);
        ExecuteEvents.ExecuteHierarchy(_currentDragTarget, _pointerEventData, ExecuteEvents.pointerUpHandler);
        _currentDragTarget = null;
    }

    private void UpdateOSCursorIfFollowing()
    {
        if (!_followOSCursor || !Application.isFocused || _usingRealMouse)
            return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        SetCursorPos((int)_screenPosition.x, (int)(Screen.height - _screenPosition.y));
#endif
    }

    // =========================================================
    // API pública usada por otros sistemas
    // =========================================================

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