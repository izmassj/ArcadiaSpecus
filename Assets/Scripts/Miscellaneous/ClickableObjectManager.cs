using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClickableObjectManager : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private InputActionAsset clickableInputActionMapping;

    private InputAction clickInputAction;
    private Color originalColor, hoverColor, clickedOnColor;
    public Ray ray;

    private void SetUpInputActions()
    {
        clickableInputActionMapping.Enable();
        clickInputAction = clickableInputActionMapping.FindActionMap("Screen").FindAction("Click");
    }

    private void Awake()
    {
        SetUpInputActions();

        if (material != null)
        {
            originalColor = material.color;
            hoverColor = originalColor * 1.5f;
            clickedOnColor = originalColor * 2f;
        }
    }

    void HoverOnObject()
    {
        if (Physics.Raycast(ray) && clickInputAction.ReadValue<float>() == 0)
        {
            material.color = Color.white * 1.5f;
        }
    }

    void ClickOnObject()
    {
        if (Physics.Raycast(ray) && clickInputAction.ReadValue<float>() != 0)
        {
            material.color = Color.white * 2f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        material.color = Color.white;

        HoverOnObject();

        ClickOnObject();
    }
}
