using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;    // Velocidad de movimiento horizontal
    public float jumpForce = 10f;   // Fuerza del salto
    private Rigidbody2D rb;         // Componente de física del jugador

    // Se ejecuta al iniciar, obtiene el componente Rigidbody2D
    void Start() => rb = GetComponent<Rigidbody2D>();

    // Se ejecuta en cada frame del juego
    void Update()
    {
        // Si el juego está pausado, no hace nada
        if (Time.timeScale == 0) return;

        // Obtiene entrada horizontal (teclas A/D o flechas)
        float move = Input.GetAxis("Horizontal");
        // Aplica velocidad horizontal manteniendo la velocidad vertical actual
        rb.velocity = new Vector2(move * moveSpeed, rb.velocity.y);

        // Si se presiona Espacio, aplica fuerza de salto
        if (Input.GetKeyDown(KeyCode.Space))
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
}