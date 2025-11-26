using UnityEngine;
using System.Collections;

public class BouncingBall : MonoBehaviour
{
    [Header("Ball Settings")]
    public float impulseForce = 8f;       // Fuerza con la que salta la pelota
    public float impulseInterval = 2f;    // Tiempo entre cada salto

    private Rigidbody2D rb;               // Componente de física de la pelota
    private bool isPaused = false;        // Si el juego está pausado
    private Coroutine impulseCoroutine;   // Referencia a la corrutina de salto

    // Se ejecuta cuando aparece la pelota en escena
    void Start()
    {
        // Obtiene el componente de física de la pelota
        rb = GetComponent<Rigidbody2D>();

        // Configura la gravedad y fricción de la pelota
        if (rb != null)
        {
            rb.gravityScale = 1f;  // Gravedad normal
            rb.drag = 0.5f;        // Un poco de fricción
        }

        // Inicia la corrutina que hace saltar la pelota cada cierto tiempo
        impulseCoroutine = StartCoroutine(ImpulseRoutine());
    }

    // Corrutina que se ejecuta continuamente
    IEnumerator ImpulseRoutine()
    {
        // Bucle infinito que se repite siempre
        while (true)
        {
            // Espera el tiempo configurado entre saltos
            yield return new WaitForSeconds(impulseInterval);

            // Si no está pausado y tiene componente de física, hace saltar la pelota
            if (!isPaused && rb != null)
            {
                // Aplica una fuerza hacia arriba de tipo impulso
                rb.AddForce(Vector2.up * impulseForce, ForceMode2D.Impulse);
            }
        }
    }

    // Método para pausar o reanudar la pelota
    public void SetPaused(bool paused)
    {
        isPaused = paused;  // Actualiza el estado de pausa

        // Si tiene componente de física
        if (rb != null)
        {
            if (paused)
            {
                // Si está pausado, congela toda la física
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                // Si se reanuda, quita las restricciones pero congela la rotación
                rb.constraints = RigidbodyConstraints2D.None;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    // Se ejecuta cuando la pelota es destruida
    void OnDestroy()
    {
        // Detiene la corrutina para evitar errores
        if (impulseCoroutine != null)
        {
            StopCoroutine(impulseCoroutine);
        }
    }
}