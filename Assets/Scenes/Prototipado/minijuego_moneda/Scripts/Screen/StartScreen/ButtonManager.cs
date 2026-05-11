using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public void StartGame()
    {
        Debug.Log("Start Game");

        SceneManager.LoadScene("Robot");
    }

    public void OpenOptions()
    {
        Debug.Log("Options");
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
    }
}
