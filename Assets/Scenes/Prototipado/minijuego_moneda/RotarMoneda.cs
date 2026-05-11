using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RotarMoneda : MonoBehaviour
{
    [Header("Moneda")]
    public Transform moneda;
    public float velocidadRotacion = 800f;

    [Header("UI")]
    public TMP_Text textoTokens;
    public TMP_Text resultadoTexto;

    private int tokens = 500;
    private int apuesta = 50;

    private bool girando = false;

    void Start()
    {
        ActualizarUI();
    }

    void Update()
    {
        if (girando)
        {
            moneda.Rotate(Vector3.right * velocidadRotacion * Time.deltaTime);
        }
    }

    // BOTONES DE APUESTA
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

    // BOTON PLAY
    public void Jugar()
    {
        if (girando) return;

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
        girando = true;

        yield return new WaitForSeconds(2f);

        girando = false;

        
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
    }

    void ActualizarUI()
    {
        textoTokens.text = "Tokens: " + tokens;
    }
}