using UnityEngine;

public class GuillotinaController : MonoBehaviour
{
    public float velocidad = 3f;  // Velocidad de movimiento
    public float distancia = 5f;  // Distancia máxima hacia adelante y hacia atrás
    private Vector3 puntoInicial; // Posición inicial de la plataforma
    private Vector3 puntoFinal;   // Posición final de la plataforma
    private bool moviendoHaciaDelante = true;  // Dirección del movimiento

    void Start()
    {
        // Guardamos la posición inicial
        puntoInicial = transform.position;
        puntoFinal = puntoInicial + new Vector3(0f, distancia, 0f); // Movimiento en el eje y
    }

    void Update()
    {
        // Movimiento entre los dos puntos en el eje y
        if (moviendoHaciaDelante)
        {
            // Mover hacia adelante (en el eje y)
            transform.position = Vector3.MoveTowards(transform.position, puntoFinal, velocidad * Time.deltaTime);

            // Si alcanzamos el punto final, cambiamos la dirección
            if (transform.position == puntoFinal)
            {
                moviendoHaciaDelante = false;
            }
        }

        else
        {
            // Mover hacia atrás (en el eje y)
            transform.position = Vector3.MoveTowards(transform.position, puntoInicial, velocidad * Time.deltaTime);

            // Si alcanzamos el punto inicial, cambiamos la dirección
            if (transform.position == puntoInicial)
            {
                moviendoHaciaDelante = true;
            }
        }
    }

}
