using UnityEngine;

public class PilarMovementOld : MonoBehaviour
{
    public float velocidad = 3f;       // Velocidad de movimiento
    public float distancia = 5f;       // Distancia que baja
    private Vector3 puntoInicial;      // Posición inicial
    private Vector3 puntoFinal;        // Posición final (más abajo)
    private bool activada = false;     // Solo baja cuando el player activa

    void Start()
    {
        // Guardamos la posición inicial y calculamos la final (hacia abajo)
        puntoInicial = transform.position;
        puntoFinal = puntoInicial + new Vector3(0f, -distancia, 0f);
    }

    void Update()
    {
        // Si está activada, mover hacia abajo
        if (activada)
        {
            transform.position = Vector3.MoveTowards(transform.position, puntoFinal, velocidad * Time.deltaTime);
        }
    }

    // Cuando el jugador entra en el trigger, la guillotina se activa
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activada = true;
        }
    }
}
