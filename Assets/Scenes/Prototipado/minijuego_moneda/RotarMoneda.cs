using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RotarMoneda : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textoTokens;
    public TMP_Text resultadoTexto;

    private int tokens = 500;
    private int apuesta = 50;

    private bool jugando = false;

    void Start()
    {
        ActualizarUI();
    }

 

    public void Apostar50()
    {
        apuesta = 50;
    }

    public void Apostar100()
    {
        apuesta = 100;
    }

    public void Apostar200()
    {
        apuesta = 200;
    }

    

    public void Jugar()
    {
        if (jugando) return;

        if (tokens < apuesta)
        {
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
    }
}