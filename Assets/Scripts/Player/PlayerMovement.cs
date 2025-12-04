using UnityEngine;

/*
    This script provides jumping and movement in Unity 3D - Gatsby
*/

public class PlayerMovement : MonoBehaviour
{
    // Camera Rotation
    public float mouseSensitivity = 2f;
    private float verticalRotation = 0f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private string groundTag = "Ground";

    // Ground Movement
    private Rigidbody rb;
    public float MoveSpeed = 5f;
    private float moveHorizontal;
    private float moveForward;

    // Jumping
    public float jumpForce = 10f;
    public float fallMultiplier = 2.5f; // Multiplies gravity when falling down
    public float ascendMultiplier = 2f; // Multiplies gravity for ascending to peak of jump
    private bool isGrounded = true;
    public LayerMask groundLayer;
    private float groundCheckTimer = 0f;
    private float groundCheckDelay = 0.3f;
    private float playerHeight;
    private float raycastDistance;

    // Platform movement
    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;
    private Vector3 platformVelocity;
    private bool isOnPlatform = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraTransform = Camera.main.gameObject.transform;

        // Set the raycast to be slightly beneath the player's feet
        playerHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        raycastDistance = (playerHeight / 2) + 0.2f;

        // Hides the mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveForward = Input.GetAxisRaw("Vertical");

        RotateCamera();

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // Checking when we're on the ground and keeping track of our ground check delay
        if (!isGrounded && groundCheckTimer <= 0f)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, raycastDistance, groundLayer);
        }
        else
        {
            groundCheckTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        // Calculate platform movement before applying player movement
        if (isOnPlatform && currentPlatform != null)
        {
            platformVelocity = (currentPlatform.position - lastPlatformPosition) / Time.fixedDeltaTime;
            lastPlatformPosition = currentPlatform.position;
        }
        else
        {
            platformVelocity = Vector3.zero;
        }

        MovePlayer();
        ApplyJumpPhysics();
    }

    void MovePlayer()
    {
        Vector3 movement = (transform.right * moveHorizontal + transform.forward * moveForward).normalized;
        Vector3 targetVelocity = movement * MoveSpeed;

        // Apply movement to the Rigidbody
        Vector3 velocity = rb.velocity;

        // Solo aplicar velocidad horizontal del jugador, mantener la plataforma en una capa separada
        if (!isOnPlatform)
        {
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
        }
        else
        {
            // Cuando está en plataforma, combinar movimiento del jugador con velocidad de plataforma
            velocity.x = targetVelocity.x + platformVelocity.x;
            velocity.z = targetVelocity.z + platformVelocity.z;
        }

        rb.velocity = velocity;

        // If we aren't moving and are on the ground, stop velocity so we don't slide
        // But preserve platform movement
        if (isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            if (isOnPlatform)
            {
                rb.velocity = new Vector3(platformVelocity.x, rb.velocity.y, platformVelocity.z);
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }

    void RotateCamera()
    {
        float horizontalRotation = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);

        verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void Jump()
    {
        isGrounded = false;
        groundCheckTimer = groundCheckDelay;

        // Preserve platform velocity when jumping
        Vector3 jumpVelocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        rb.velocity = jumpVelocity;

        // Dejar de estar en plataforma al saltar
        isOnPlatform = false;
        currentPlatform = null;
    }

    void ApplyJumpPhysics()
    {
        if (rb.velocity.y < 0)
        {
            // Falling: Apply fall multiplier to make descent faster
            rb.velocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.fixedDeltaTime;
        } // Rising
        else if (rb.velocity.y > 0)
        {
            // Rising: Change multiplier to make player reach peak of jump faster
            rb.velocity += Vector3.up * Physics.gravity.y * ascendMultiplier * Time.fixedDeltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = true;
            groundCheckTimer = 0;

            // Check if it's a moving platform
            PlataformaMovimiento platform = collision.gameObject.GetComponent<PlataformaMovimiento>();
            if (platform != null)
            {
                // Check if we're landing on top (normal pointing up)
                float dot = Vector3.Dot(collision.contacts[0].normal, Vector3.up);
                if (dot > 0.7f)
                {
                    isOnPlatform = true;
                    currentPlatform = collision.transform;
                    lastPlatformPosition = currentPlatform.position;
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            // Check if it's the platform we're leaving
            if (collision.transform == currentPlatform)
            {
                isOnPlatform = false;
                currentPlatform = null;
            }
            isGrounded = false;
        }
    }
}