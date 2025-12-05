using UnityEngine;

/*
    Este script controla el movimiento y salto del jugador en 3D - Gatsby
*/

public class PlayerMovement : MonoBehaviour
{
    // ========== CONFIGURACIÓN DE CÁMARA ==========
    public float sensibilidadMouse = 2f;       // Sensibilidad del ratón para rotación
    private float rotacionVertical = 0f;        // Rotación vertical acumulada de la cámara
    [SerializeField] private Transform transformCamara; // Referencia al transform de la cámara
    [SerializeField] private string tagSuelo = "Ground"; // Tag para identificar el suelo

    // ========== MOVIMIENTO TERRESTRE ==========
    private Rigidbody rb;                       // Referencia al Rigidbody del jugador
    public float velocidadBase = 5f;            // Velocidad de movimiento normal
    public float velocidadSprint = 8f;          // Velocidad al correr (sprint)
    private float velocidadActual;              // Velocidad actual aplicada
    private float movimientoHorizontal;         // Input horizontal (A/D o flechas)
    private float movimientoAdelante;           // Input vertical (W/S o flechas)

    // ========== CONFIGURACIÓN DE SALTO ==========
    public float fuerzaSalto = 10f;             // Fuerza inicial del salto
    public float multiplicadorCaida = 2.5f;     // Multiplica la gravedad al caer
    public float multiplicadorAscenso = 2f;     // Multiplica la gravedad al ascender
    private bool enSuelo = true;                // Indica si el jugador está en el suelo
    public LayerMask capaSuelo;                 // Capas consideradas como suelo
    private float temporizadorChequeoSuelo = 0f; // Temporizador para chequeo de suelo
    private float retrasoChequeoSuelo = 0.3f;   // Retraso entre chequeos de suelo
    private float alturaJugador;                // Altura del collider del jugador
    private float distanciaRaycast;             // Distancia del raycast para detectar suelo

    // ========== MOVIMIENTO EN PLATAFORMAS ==========
    private Transform plataformaActual;         // Referencia a la plataforma actual
    private Vector3 ultimaPosicionPlataforma;   // Última posición de la plataforma
    private Vector3 velocidadPlataforma;        // Velocidad calculada de la plataforma
    private bool enPlataforma = false;          // Indica si está sobre una plataforma móvil

    private float rotacionHorizontal; // Rotación horizontal acumulada del jugador

    void Start()
    {
        // Inicialización del Rigidbody
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Congelar rotación para evitar caídas indeseadas

        // Obtener referencia a la cámara principal si no está asignada
        if (transformCamara == null)
            transformCamara = Camera.main.transform;

        // Configuración del raycast para detección de suelo
        alturaJugador = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        distanciaRaycast = (alturaJugador / 2) + 0.2f; // Ligeramente por debajo de los pies

        // Configuración inicial de velocidad
        velocidadActual = velocidadBase;

        // Configurar cursor para modo juego
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Capturar inputs de movimiento
        movimientoHorizontal = Input.GetAxisRaw("Horizontal");
        movimientoAdelante = Input.GetAxisRaw("Vertical");

        // Control de sprint (Shift izquierdo)
        if (Input.GetKey(KeyCode.LeftShift))
        {
            velocidadActual = velocidadSprint; // Velocidad de sprint
        }
        else
        {
            velocidadActual = velocidadBase;   // Velocidad normal
        }

        // Capturar input del mouse y stick
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");
        float stickX = Input.GetAxisRaw("RHorizontal");
        float stickY = Input.GetAxisRaw("RVertical");

        // Acumular rotaciones
        rotacionHorizontal += (mouseX + stickX) * sensibilidadMouse;
        rotacionVertical += (mouseY + stickY) * sensibilidadMouse;
        rotacionVertical = Mathf.Clamp(rotacionVertical, -90f, 90f);

        // Detectar salto cuando se presiona espacio y está en el suelo
        if (Input.GetButtonDown("Jump") && enSuelo)
        {
            Saltar();
        }

        // Chequeo periódico de contacto con el suelo
        if (!enSuelo && temporizadorChequeoSuelo <= 0f)
        {
            Vector3 origenRay = transform.position + Vector3.up * 0.1f;
            enSuelo = Physics.Raycast(origenRay, Vector3.down, distanciaRaycast, capaSuelo);
        }
        else
        {
            temporizadorChequeoSuelo -= Time.deltaTime; // Reducir temporizador
        }
    }

    void FixedUpdate()
    {
        // Calcular velocidad de plataforma móvil (si está sobre una)
        if (enPlataforma && plataformaActual != null)
        {
            velocidadPlataforma = (plataformaActual.position - ultimaPosicionPlataforma) / Time.fixedDeltaTime;
            ultimaPosicionPlataforma = plataformaActual.position;
        }
        else
        {
            velocidadPlataforma = Vector3.zero; // Sin plataforma, sin velocidad adicional
        }

        // Rotación horizontal del jugador usando Rigidbody
        Quaternion nuevaRotacion = Quaternion.Euler(0, rotacionHorizontal, 0);
        rb.MoveRotation(nuevaRotacion);

        // Aplicar movimiento y físicas de salto
        MoverJugador();
        AplicarFisicasSalto();
    }

    private void LateUpdate()
    {
        // Rotación vertical de la cámara (solo eje X)
        transformCamara.localRotation = Quaternion.Euler(rotacionVertical, 0, 0);
    }

    void MoverJugador()
    {
        // Calcular dirección de movimiento basada en la rotación del jugador
        Vector3 direccion = (transform.right * movimientoHorizontal + transform.forward * movimientoAdelante).normalized;
        Vector3 velocidadObjetivo = direccion * velocidadActual;

        // Obtener velocidad actual del Rigidbody
        Vector3 velocidad = rb.velocity;

        // Aplicar movimiento horizontal según si está en plataforma o no
        if (!enPlataforma)
        {
            // Movimiento normal: aplicar velocidad del jugador
            velocidad.x = velocidadObjetivo.x;
            velocidad.z = velocidadObjetivo.z;
        }
        else
        {
            // En plataforma: combinar velocidad del jugador con velocidad de la plataforma
            velocidad.x = velocidadObjetivo.x + velocidadPlataforma.x;
            velocidad.z = velocidadObjetivo.z + velocidadPlataforma.z;
        }

        rb.velocity = velocidad; // Aplicar velocidad calculada

        // Detener deslizamiento cuando no hay input y está en el suelo
        if (enSuelo && movimientoHorizontal == 0 && movimientoAdelante == 0)
        {
            if (enPlataforma)
            {
                // Mantener velocidad de plataforma pero no del jugador
                rb.velocity = new Vector3(velocidadPlataforma.x, rb.velocity.y, velocidadPlataforma.z);
            }
            else
            {
                // Detener completamente el movimiento horizontal
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }

    void RotarCamara()
    {
        // --- Entradas del mouse ---
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // --- Entradas del stick derecho del mando ---
        float stickX = Input.GetAxisRaw("RHorizontal");
        float stickY = Input.GetAxisRaw("RVertical");

        // Rotación horizontal (cuerpo del jugador)
        float rotacionHorizontal = (mouseX + stickX) * sensibilidadMouse;
        transform.Rotate(0, rotacionHorizontal, 0);

        // Rotación vertical (sólo cámara, con límites)
        rotacionVertical -= (mouseY + stickY) * sensibilidadMouse;
        rotacionVertical = Mathf.Clamp(rotacionVertical, -90f, 90f);

        transformCamara.localRotation = Quaternion.Euler(rotacionVertical, 0, 0);
    }


    void Saltar()
    {
        enSuelo = false; // Ya no está en el suelo
        temporizadorChequeoSuelo = retrasoChequeoSuelo; // Iniciar temporizador de chequeo

        // Crear vector de salto manteniendo velocidad horizontal
        Vector3 velocidadSalto = new Vector3(rb.velocity.x, fuerzaSalto, rb.velocity.z);
        rb.velocity = velocidadSalto;

        // Dejar de considerar que está en plataforma al saltar
        enPlataforma = false;
        plataformaActual = null;
    }

    void AplicarFisicasSalto()
    {
        // Aplicar gravedad modificada según fase del salto
        if (rb.velocity.y < 0)
        {
            // Fase de caída: aplicar multiplicador para caer más rápido
            rb.velocity += Vector3.up * Physics.gravity.y * multiplicadorCaida * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            // Fase de ascenso (sin mantener botón): aplicar multiplicador para ascenso más corto
            rb.velocity += Vector3.up * Physics.gravity.y * multiplicadorAscenso * Time.fixedDeltaTime;
        }
    }

    private void OnCollisionEnter(Collision colision)
    {
        if (colision.gameObject.CompareTag(tagSuelo))
        {
            enSuelo = true; // Contacto con el suelo
            temporizadorChequeoSuelo = 0; // Reiniciar temporizador

            // Verificar si es una plataforma móvil
            PlataformaMovimiento plataforma = colision.gameObject.GetComponent<PlataformaMovimiento>();
            if (plataforma != null)
            {
                // Verificar que se está aterrizando en la parte superior
                float productoPunto = Vector3.Dot(colision.contacts[0].normal, Vector3.up);
                if (productoPunto > 0.7f) // Normal apuntando hacia arriba
                {
                    enPlataforma = true;
                    plataformaActual = colision.transform;
                    ultimaPosicionPlataforma = plataformaActual.position;
                }
            }
        }
    }

    private void OnCollisionStay(Collision colision)
    {
        // Mantener estado "en suelo" mientras está en contacto
        if (colision.gameObject.CompareTag(tagSuelo))
        {
            enSuelo = true;
        }
    }

    private void OnCollisionExit(Collision colision)
    {
        if (colision.gameObject.CompareTag(tagSuelo))
        {
            // Verificar si está abandonando una plataforma específica
            if (colision.transform == plataformaActual)
            {
                enPlataforma = false;
                plataformaActual = null;
            }
            enSuelo = false; // Ya no está en contacto con el suelo
        }
    }
}