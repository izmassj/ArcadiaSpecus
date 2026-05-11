using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*****************************************************************************************************/
// Ir a Edit -> Project Settings -> Input Manager -> Submit -> Alt Positive Button: (borrar "space") // CAMBI DE PLANES, LETRA E YA QUE ESPACIO CUENTA PARA SELECCIONAR BOTONES TAMBIEN
/*****************************************************************************************************/

public class StartScreenController : MonoBehaviour
{
    [SerializeField] private GameObject startScreen;
    [SerializeField] private TMP_Text startText;
    [SerializeField] private MenuManager mainMenu;

    [SerializeField] private float timeInterval = 0.5f;

    private bool started = false;
    private Coroutine blinkRoutine;

    void Start()
    {
        if (startText != null)
            blinkRoutine = StartCoroutine(Blink());
    }

    void Update()
    {
        if (!started && Input.GetKeyDown(KeyCode.E))
        {
            started = true;

            if (blinkRoutine != null)
                StopCoroutine(blinkRoutine);

            if (startScreen != null)
                startScreen.SetActive(false);

            if (mainMenu != null)
            {
                mainMenu.ResetToFirstButton();
                mainMenu.OpenMenu();
            }
        }
    }

    private IEnumerator Blink()
    {
        while (true)
        {
            Color c = startText.color;
            c.a = c.a > 0.5f ? 0f : 1f;
            startText.color = c;

            yield return new WaitForSeconds(timeInterval);
        }
    }
}
