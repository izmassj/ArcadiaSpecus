using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UISelectionRecoverer : MonoBehaviour
{
    GameObject lastSelectedObj;
    private void Update()
    {
        if (lastSelectedObj && !EventSystem.current.currentSelectedGameObject)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedObj);
        }
        else
        {
            lastSelectedObj = EventSystem.current.currentSelectedGameObject;
        }
    }
}
