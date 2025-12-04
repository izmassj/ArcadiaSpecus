using UnityEngine;

public class PlataformaMovimientoEnY : MonoBehaviour
{
    public float velocidad = 3f;  // Velocidad de movimiento
    public float distancia = 5f;  // Distancia máxima hacia adelante y hacia atrás
    private Vector3 puntoInicial; // Posición inicial de la plataforma
    private Vector3 puntoFinal;   // Posición final de la plataforma
    private bool moviendoHaciaDelante = true;  // Dirección del movimiento

    // Para llevar un registro del jugador en la plataforma
    private Transform playerOnPlatform;
    private Vector3 lastPlatformPosition;

    void Start()
    {
        // Guardamos la posición inicial
        puntoInicial = transform.position;
        puntoFinal = puntoInicial + new Vector3(0f, distancia, 0f); // Movimiento en el eje Z
        lastPlatformPosition = transform.position;
    }

    void Update()
    {
        // Guardar posición anterior antes de mover
        Vector3 previousPosition = transform.position;

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

        // Actualizar última posición para el próximo frame
        lastPlatformPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Si hay un jugador en la plataforma, moverlo con ella en FixedUpdate para mejor física
        if (playerOnPlatform != null)
        {
            // Calcular el movimiento de la plataforma este frame
            Vector3 platformMovement = transform.position - lastPlatformPosition;

            // Aplicar el mismo movimiento al jugador
            playerOnPlatform.position += platformMovement;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Cuando el jugador entra en contacto con la plataforma
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = collision.transform;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Mantener el jugador como "en la plataforma" si sigue en contacto
        if (collision.gameObject.CompareTag("Player"))
        {
            // Asegurarnos de que seguimos registrando al jugador
            if (playerOnPlatform == null)
            {
                playerOnPlatform = collision.transform;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Cuando el jugador sale de la plataforma
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnPlatform = null;
        }
    }

    void LateUpdate()
    {
        // Actualizar la última posición para el próximo frame
        lastPlatformPosition = transform.position;
    }
}