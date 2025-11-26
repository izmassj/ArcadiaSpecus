using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinMiniGame : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            Debug.Log("Funciona el collider");
            SceneManager.LoadScene("MainScene");
        }
    }
}

