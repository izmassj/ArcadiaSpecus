using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    // MOVIMIENTO
    [SerializeField] float speed = 5f;
    [SerializeField] float maxHorizontalSpeed = 20f;
    Rigidbody rb;
    Vector3 movementInput;
    Vector3 directionInput;

    // SALTO
    [SerializeField] float propulsionForce;
    [SerializeField] float groundCheckDistance = 1.2f;
    [SerializeField] LayerMask groundLayer;

    // CAM
    [SerializeField] Camera firstPersonCamera;
    [SerializeField] Camera thirdPersonCamera;
    [SerializeField] Transform cameraPivot;
    [SerializeField] float turretRotationSpeed = 120f;
    [SerializeField] float rotationSpeed = 120f;
    [SerializeField] float verticalSpeed = 120f;
    [SerializeField] float minVerticalAngle = -60f;
    [SerializeField] float maxVerticalAngle = 60f;
    [SerializeField] Transform turretPivot;
    [SerializeField] Transform cameraVerticalPivot;
    float verticalRotation = 0f;
    bool isThirdPerson = false;

    // ZOOM
    [SerializeField] float normalFOV = 60f;
    [SerializeField] float zoomFOV = 25f;
    [SerializeField] float zoomSpeed = 8f;
    bool isZooming = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        firstPersonCamera.gameObject.SetActive(true); // para asegurar que empieza siempre en 1st person
        thirdPersonCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        movementInput = Vector3.zero;

        float verticalInput = 0f;
        float bodyRotationInput = 0f;
        float turretInput = 0f;

        if (Input.GetKey(KeyCode.W)) // Movimiento Horizontal
        {
            movementInput.z = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            movementInput.z = -1;
        }

        if (Input.GetKey(KeyCode.D))
        {
            bodyRotationInput = 1f;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            bodyRotationInput = -1f;
        }

        directionInput = movementInput;

        if (Input.GetKey(KeyCode.Space) && CheckGround()) // SALTO
        {
            rb.AddForce(Vector3.up * propulsionForce, ForceMode.Impulse);
        }

        // MOVIMIENTO CAM CON FLECHAS

        if (Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput = 1f;
        }
        
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            turretInput = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            turretInput = 1f;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            isThirdPerson = !isThirdPerson; // alternar entre 1st person y 3rd person

            firstPersonCamera.gameObject.SetActive(!isThirdPerson);
            thirdPersonCamera.gameObject.SetActive(isThirdPerson);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            isZooming = !isZooming; // para el zoom
        }

        // Rotación horizontal (Cam)
        turretPivot.Rotate(Vector3.up * turretInput * turretRotationSpeed * Time.deltaTime, Space.Self);

        // Rotación horizontal (Player)
        transform.Rotate(Vector3.up * bodyRotationInput * rotationSpeed * Time.deltaTime);

        // Rotación vertical (CameraPivot)
        verticalRotation += verticalInput * verticalSpeed * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

        cameraPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        if (firstPersonCamera.gameObject.activeSelf) // todavia no entiendo mucho como va esto, lo he visto en un video (falta entrenderlo)
        {
            float targetFOV = isZooming ? zoomFOV : normalFOV;
            firstPersonCamera.fieldOfView = Mathf.Lerp(
                firstPersonCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * zoomSpeed
            );
        }
    }

    private void FixedUpdate()
    {
        Move(movementInput);
    }

    bool CheckGround() // metodo para comprovar el suelo
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    void Move(Vector3 direction)
    {
        float forwardInput = direction.z; // Movimiento adelante y atras

        Vector3 forwardForce = transform.forward * forwardInput * speed;
        rb.AddForce(forwardForce, ForceMode.Force);

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Clampeo velocidad horizontal 

        if (horizontalVelocity.magnitude > maxHorizontalSpeed)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxHorizontalSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }

        // Debug.Log(rb.velocity);
    }
}

// ----- TO DO ------ //
// # Aplicar movimiento cam con ratón
// # Max fuerza Horizontal

// ---- OPCIONAL ---- //
// # Camara 3ra perosona
// # Zoom

// por donde nos hemos quedado
// a partir de la linea 149-150 separar le movimiento de tirar para adelante con el de rotación