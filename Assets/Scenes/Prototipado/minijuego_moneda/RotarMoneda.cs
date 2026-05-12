using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RotarMoneda : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textoTokens;
    public TMP_Text textoApuesta;
    public TMP_Text resultadoTexto;

    private int tokens = 500;
    private int apuesta = 50;

    private bool jugando = false;

    void Start()
    {
        if (resultadoTexto != null)
            resultadoTexto.gameObject.SetActive(false);

        ActualizarUI();
    }

    public void Apostar50()
    {
        if (jugando) return;

        apuesta = 50;
        ActualizarUI();
    }

    public void Apostar100()
    {
        if (jugando) return;

        apuesta = 100;
        ActualizarUI();
    }

    public void Apostar200()
    {
        if (jugando) return;

        apuesta = 200;
        ActualizarUI();
    }

    public void Jugar()
    {
        if (jugando) return;

        if (tokens < apuesta)
        {
            resultadoTexto.gameObject.SetActive(true);
            resultadoTexto.text = "No tienes suficientes tokens";
            return;
        }

        tokens -= apuesta;
        ActualizarUI();

        StartCoroutine(TirarMoneda());
    }

    System.Collections.IEnumerator TirarMoneda()
    {
        jugando = true;

        resultadoTexto.gameObject.SetActive(true);
        resultadoTexto.text = "Girando...";

        yield return new WaitForSeconds(2f);

        int resultado = Random.Range(0, 2);

        if (resultado == 1)
        {
            int premio = apuesta * 2;
            tokens += premio;

            resultadoTexto.text = "¡GANASTE! +" + premio;
        }
        else
        {
            resultadoTexto.text = "Perdiste";
        }

        ActualizarUI();

        jugando = false;
    }

    void ActualizarUI()
    {
        textoTokens.text = "Tokens: " + tokens;
        textoApuesta.text = "Apuesta: " + apuesta;
    }
}