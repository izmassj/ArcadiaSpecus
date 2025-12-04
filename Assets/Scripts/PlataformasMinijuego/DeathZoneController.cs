using UnityEngine;

public class DeathZoneController : MonoBehaviour
{
    public float velocidad = 3f;  // Velocidad de movimiento
    public float distancia = 5f;  // Distancia máxima hacia adelante
    private Vector3 puntoInicial; // Posición inicial de la plataforma
    private Vector3 puntoFinal;   // Posición final de la plataforma

    void Start()
    {
        // Guardamos la posición inicial
        puntoInicial = transform.position;
        puntoFinal = puntoInicial + new Vector3(0f, 0f, distancia); // Movimiento en el eje X
    }

    void Update()
    {
        // Mover siempre hacia adelante (en el eje X)
        transform.position = Vector3.MoveTowards(transform.position, puntoFinal, velocidad * Time.deltaTime);

        // Si alcanzamos el punto final, teletransportar al punto inicial
        if (Vector3.Distance(transform.position, puntoFinal) < 0.01f)
        {   
            transform.position = puntoInicial;
        }
    }
}