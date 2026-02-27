using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detecta de forma simple el último tipo de input usado.
/// Útil para UI (mostrar prompts de mando/teclado) y para cursor virtual.
/// </summary>
public class InputDeviceTracker : MonoBehaviour
{
    public enum InputDeviceKind
    {
        Unknown,
        KeyboardMouse,
        Gamepad
    }

    public static InputDeviceTracker Instance { get; private set; }

    public static event Action<InputDeviceKind> OnInputDeviceChanged;

    [SerializeField] private float _stickDeadzone = 0.15f;
    [SerializeField] private float _mouseDeltaThreshold = 0.1f;

    private InputDeviceKind _currentInputDevice = InputDeviceKind.Unknown;

    public InputDeviceKind CurrentInputDevice => _currentInputDevice;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static InputDeviceTracker EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        InputDeviceTracker found = FindObjectOfType<InputDeviceTracker>();
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        GameObject go = new GameObject("[InputDeviceTracker]");
        return go.AddComponent<InputDeviceTracker>();
    }

    private void Update()
    {
        // Ratón
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            if (mouseDelta.sqrMagnitude > _mouseDeltaThreshold * _mouseDeltaThreshold)
            {
                SetCurrentDevice(InputDeviceKind.KeyboardMouse);
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
            {
                SetCurrentDevice(InputDeviceKind.KeyboardMouse);
                return;
            }
        }

        // Teclado
        if (Keyboard.current != null)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                SetCurrentDevice(InputDeviceKind.KeyboardMouse);
                return;
            }
        }

        // Mando
        if (Gamepad.current != null)
        {
            Vector2 left = Gamepad.current.leftStick.ReadValue();
            Vector2 right = Gamepad.current.rightStick.ReadValue();

            if (left.sqrMagnitude > _stickDeadzone * _stickDeadzone ||
                right.sqrMagnitude > _stickDeadzone * _stickDeadzone ||
                Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.startButton.wasPressedThisFrame)
            {
                SetCurrentDevice(InputDeviceKind.Gamepad);
            }
        }
    }

    private void SetCurrentDevice(InputDeviceKind newDevice)
    {
        if (_currentInputDevice == newDevice)
            return;

        _currentInputDevice = newDevice;
        OnInputDeviceChanged?.Invoke(_currentInputDevice);
    }
}