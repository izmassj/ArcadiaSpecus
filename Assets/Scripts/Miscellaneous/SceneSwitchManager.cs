using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitchManager : MonoBehaviour
{
    public static SceneSwitchManager instance;

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
    }
}
