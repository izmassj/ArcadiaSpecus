using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIRecompensas : MonoBehaviour
{
    [SerializeField] public GameObject menu;

    public void CerrarMenu()
    {
        menu.SetActive(false);
    }
    

}
