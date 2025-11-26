//PlayerMovement 
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
        CalculatePlatformMovement();

        MovePlayer();
        ApplyJumpPhysics();
    }

    void MovePlayer()
    {
        Vector3 movement = (transform.right * moveHorizontal + transform.forward * moveForward).normalized;
        Vector3 targetVelocity = movement * MoveSpeed;

        // Apply movement to the Rigidbody
        Vector3 velocity = rb.velocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;

        // Add platform velocity to maintain relative movement
        if (currentPlatform != null)
        {
            velocity.x += platformVelocity.x;
            velocity.z += platformVelocity.z;
        }

        rb.velocity = velocity;

        // If we aren't moving and are on the ground, stop velocity so we don't slide
        // But preserve platform movement
        if (isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            if (currentPlatform != null)
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

        // If on platform, add platform velocity to maintain momentum
        if (currentPlatform != null)
        {
            jumpVelocity.x += platformVelocity.x;
            jumpVelocity.z += platformVelocity.z;
        }

        rb.velocity = jumpVelocity;
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

    void CalculatePlatformMovement()
    {
        if (currentPlatform != null)
        {
            platformVelocity = (currentPlatform.position - lastPlatformPosition) / Time.fixedDeltaTime;
            lastPlatformPosition = currentPlatform.position;
        }
        else
        {
            platformVelocity = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            // Only set as parent if it's actually a moving platform
            Rigidbody platformRb = collision.gameObject.GetComponent<Rigidbody>();
            if (platformRb != null && !platformRb.isKinematic)
            {
                currentPlatform = collision.transform;
                lastPlatformPosition = currentPlatform.position;
            }
            else
            {
                // For static ground, just set transform parent normally
                transform.SetParent(collision.transform);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(groundTag))
        {
            // Only unparent if leaving the current platform
            if (currentPlatform == collision.transform)
            {
                currentPlatform = null;
                platformVelocity = Vector3.zero;
            }

            transform.SetParent(null);
        }
    }
}