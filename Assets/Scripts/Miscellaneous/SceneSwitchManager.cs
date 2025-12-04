using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitchManager : MonoBehaviour
{
    private static SceneSwitchManager instance;

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
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (SceneManager.GetActiveScene().name) 
        {
            case "MiniGameDesign2":
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case "NPCBunkerNavigation":
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }
}
