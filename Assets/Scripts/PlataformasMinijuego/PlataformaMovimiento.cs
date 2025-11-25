using UnityEngine;

public class PlataformaMovimiento : MonoBehaviour
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
        puntoFinal = puntoInicial + new Vector3(0f, 0f, distancia); // Movimiento en el eje Z
    }

    void Update()
    {
        // Movimiento entre los dos puntos en el eje Z
        if (moviendoHaciaDelante)
        {
            // Mover hacia adelante (en el eje Z)
            transform.position = Vector3.MoveTowards(transform.position, puntoFinal, velocidad * Time.deltaTime);

            // Si alcanzamos el punto final, cambiamos la dirección
            if (transform.position == puntoFinal)
            {
                moviendoHaciaDelante = false;
            }
        }
        else
        {
            // Mover hacia atrás (en el eje Z)
            transform.position = Vector3.MoveTowards(transform.position, puntoInicial, velocidad * Time.deltaTime);

            // Si alcanzamos el punto inicial, cambiamos la dirección
            if (transform.position == puntoInicial)
            {
                moviendoHaciaDelante = true;
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Si el jugador o cualquier otro objeto está sobre la plataforma, lo mueve con ella
        if (collision.gameObject.CompareTag("Player"))
        {
            // Le asignamos la misma posición Z de la plataforma
            collision.transform.position = new Vector3(collision.transform.position.x, collision.transform.position.y, transform.position.z);
        }
    }
}
