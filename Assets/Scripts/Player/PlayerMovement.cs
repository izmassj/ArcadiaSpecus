using UnityEngine;

/*
    Este script controla el movimiento y salto del jugador en 3D - Gatsby
*/

public class PlayerMovement : MonoBehaviour
{
    // ========== CONFIGURACI�N DE C�MARA ==========
    public float sensibilidadMouse = 2f;       // Sensibilidad del rat�n para rotaci�n
    private float rotacionVertical = 0f;        // Rotaci�n vertical acumulada de la c�mara
    [SerializeField] private Transform transformCamara; // Referencia al transform de la c�mara
    [SerializeField] private string tagSuelo = "Ground"; // Tag para identificar el suelo

    // ========== MOVIMIENTO TERRESTRE ==========
    private Rigidbody rb;                       // Referencia al Rigidbody del jugador
    public float velocidadBase = 5f;            // Velocidad de movimiento normal
    public float velocidadSprint = 8f;          // Velocidad al correr (sprint)
    private float velocidadActual;              // Velocidad actual aplicada
    private float movimientoHorizontal;         // Input horizontal (A/D o flechas)
    private float movimientoAdelante;           // Input vertical (W/S o flechas)

    // ========== CONFIGURACI�N DE SALTO ==========
    public float fuerzaSalto = 10f;             // Fuerza inicial del salto
    public float multiplicadorCaida = 2.5f;     // Multiplica la gravedad al caer
    public float multiplicadorAscenso = 2f;     // Multiplica la gravedad al ascender
    private bool enSuelo = true;                // Indica si el jugador est� en el suelo
    public LayerMask capaSuelo;                 // Capas consideradas como suelo
    private float temporizadorChequeoSuelo = 0f; // Temporizador para chequeo de suelo
    private float retrasoChequeoSuelo = 0.3f;   // Retraso entre chequeos de suelo
    private float alturaJugador;                // Altura del collider del jugador
    private float distanciaRaycast;             // Distancia del raycast para detectar suelo

    // ========== MOVIMIENTO EN PLATAFORMAS ==========
    private Transform plataformaActual;         // Referencia a la plataforma actual
    private Vector3 ultimaPosicionPlataforma;   // �ltima posici�n de la plataforma
    private Vector3 velocidadPlataforma;        // Velocidad calculada de la plataforma
    private bool enPlataforma = false;          // Indica si est� sobre una plataforma m�vil

    private float rotacionHorizontal; // Rotaci�n horizontal acumulada del jugador

    void Start()
    {
        // Inicializaci�n del Rigidbody
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Congelar rotaci�n para evitar ca�das indeseadas

        // Obtener referencia a la c�mara principal si no est� asignada
        if (transformCamara == null)
            transformCamara = Camera.main.transform;

        // Configuraci�n del raycast para detecci�n de suelo
        alturaJugador = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        distanciaRaycast = (alturaJugador / 2) + 0.2f; // Ligeramente por debajo de los pies

        // Configuraci�n inicial de velocidad
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

        // Detectar salto cuando se presiona espacio y est� en el suelo
        if (Input.GetButtonDown("Jump") && enSuelo)
        {
            Saltar();
        }

        // Chequeo peri�dico de contacto con el suelo
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
        // Calcular velocidad de plataforma m�vil (si est� sobre una)
        if (enPlataforma && plataformaActual != null)
        {
            velocidadPlataforma = (plataformaActual.position - ultimaPosicionPlataforma) / Time.fixedDeltaTime;
            ultimaPosicionPlataforma = plataformaActual.position;
        }
        else
        {
            velocidadPlataforma = Vector3.zero; // Sin plataforma, sin velocidad adicional
        }

        // Rotaci�n horizontal del jugador usando Rigidbody
        Quaternion nuevaRotacion = Quaternion.Euler(0, rotacionHorizontal, 0);
        rb.MoveRotation(nuevaRotacion);

        // Aplicar movimiento y f�sicas de salto
        MoverJugador();
        AplicarFisicasSalto();
    }

    private void LateUpdate()
    {
        // Rotaci�n vertical de la c�mara (solo eje X)
        transformCamara.localRotation = Quaternion.Euler(rotacionVertical, 0, 0);
    }

    void MoverJugador()
    {
        // Calcular direcci�n de movimiento basada en la rotaci�n del jugador
        Vector3 direccion = (transform.right * movimientoHorizontal + transform.forward * movimientoAdelante).normalized;
        Vector3 velocidadObjetivo = direccion * velocidadActual;

        // Obtener velocidad actual del Rigidbody
        Vector3 velocidad = rb.linearVelocity;

        // Aplicar movimiento horizontal seg�n si est� en plataforma o no
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

        rb.linearVelocity = velocidad; // Aplicar velocidad calculada

        // Detener deslizamiento cuando no hay input y est� en el suelo
        if (enSuelo && movimientoHorizontal == 0 && movimientoAdelante == 0)
        {
            if (enPlataforma)
            {
                // Mantener velocidad de plataforma pero no del jugador
                rb.linearVelocity = new Vector3(velocidadPlataforma.x, rb.linearVelocity.y, velocidadPlataforma.z);
            }
            else
            {
                // Detener completamente el movimiento horizontal
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
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

        // Rotaci�n horizontal (cuerpo del jugador)
        float rotacionHorizontal = (mouseX + stickX) * sensibilidadMouse;
        transform.Rotate(0, rotacionHorizontal, 0);

        // Rotaci�n vertical (s�lo c�mara, con l�mites)
        rotacionVertical -= (mouseY + stickY) * sensibilidadMouse;
        rotacionVertical = Mathf.Clamp(rotacionVertical, -90f, 90f);

        transformCamara.localRotation = Quaternion.Euler(rotacionVertical, 0, 0);
    }


    void Saltar()
    {
        enSuelo = false; // Ya no est� en el suelo
        temporizadorChequeoSuelo = retrasoChequeoSuelo; // Iniciar temporizador de chequeo

        // Crear vector de salto manteniendo velocidad horizontal
        Vector3 velocidadSalto = new Vector3(rb.linearVelocity.x, fuerzaSalto, rb.linearVelocity.z);
        rb.linearVelocity = velocidadSalto;

        // Dejar de considerar que est� en plataforma al saltar
        enPlataforma = false;
        plataformaActual = null;
    }

    void AplicarFisicasSalto()
    {
        // Aplicar gravedad modificada seg�n fase del salto
        if (rb.linearVelocity.y < 0)
        {
            // Fase de ca�da: aplicar multiplicador para caer m�s r�pido
            rb.linearVelocity += Vector3.up * Physics.gravity.y * multiplicadorCaida * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            // Fase de ascenso (sin mantener bot�n): aplicar multiplicador para ascenso m�s corto
            rb.linearVelocity += Vector3.up * Physics.gravity.y * multiplicadorAscenso * Time.fixedDeltaTime;
        }
    }

    private void OnCollisionEnter(Collision colision)
    {
        if (colision.gameObject.CompareTag(tagSuelo))
        {
            enSuelo = true; // Contacto con el suelo
            temporizadorChequeoSuelo = 0; // Reiniciar temporizador

            // Verificar si es una plataforma m�vil
            PlataformaMovimiento plataforma = colision.gameObject.GetComponent<PlataformaMovimiento>();
            if (plataforma != null)
            {
                // Verificar que se est� aterrizando en la parte superior
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
        // Mantener estado "en suelo" mientras est� en contacto
        if (colision.gameObject.CompareTag(tagSuelo))
        {
            enSuelo = true;
        }
    }

    private void OnCollisionExit(Collision colision)
    {
        if (colision.gameObject.CompareTag(tagSuelo))
        {
            // Verificar si est� abandonando una plataforma espec�fica
            if (colision.transform == plataformaActual)
            {
                enPlataforma = false;
                plataformaActual = null;
            }
            enSuelo = false; // Ya no est� en contacto con el suelo
        }
    }
}