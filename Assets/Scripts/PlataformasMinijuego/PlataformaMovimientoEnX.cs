using UnityEngine;

public class PlataformaMovimientoEnX : MonoBehaviour
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
        puntoFinal = puntoInicial + new Vector3(distancia, 0f, 0f); // Movimiento en el eje Z
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
            // También podemos hacerlo solo en el eje Z, sin afectar X o Y del jugador
            Vector3 nuevaPosicion = collision.transform.position;
            nuevaPosicion.z = transform.position.z; // Sin cambiar el resto de la posición
            collision.transform.position = nuevaPosicion;

            // Si el jugador tiene un Rigidbody, evitamos que se resbale usando "isKinematic"
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Ponemos el Rigidbody en kinematic temporalmente para que no se resbale
                rb.isKinematic = true;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Si el jugador sale de la plataforma, restablecemos el Rigidbody a su estado original
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Restablecemos el Rigidbody para que la física vuelva a ser controlada normalmente
                rb.isKinematic = false;
            }
        }
    }
}
