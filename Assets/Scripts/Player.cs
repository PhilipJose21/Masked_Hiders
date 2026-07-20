using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    private Rigidbody _rb;
    private Camera _camera;

    private float turnSmoothVelocity;
    private bool _jumpQueued;
    private bool _isHoldingJump;
    public Collider _mainCollider;

    [Header("Movement Settings")]
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float speed = 5f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float gravity = 19.62f; // ~2x standard gravity for crisp physics
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpHoldBoost = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck; // Assign an empty GameObject at player's feet
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;  // Set this to your "Ground" layer in the Inspector

    public override void OnNetworkSpawn()
    {
        _rb = GetComponent<Rigidbody>();
        _camera = GetComponentInChildren<Camera>();

        // Disable standard Rigidbody gravity so your custom gravity takes full control
        if (_rb != null)
        {
            _rb.useGravity = false;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (!IsOwner && _camera != null)
        {
            _camera.gameObject.SetActive(false);
        }
    }

    private void Awake()
    {
        // Grab the active collider (or re-fetch when transforming)
        _mainCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            GetComponent<DetectObject>()?.detectObject();
        }

        // Capture jump inputs during Update (render frame)
        if (Input.GetButtonDown("Jump"))
        {
            _jumpQueued = true;
        }

        _isHoldingJump = Input.GetButton("Jump");
    }

    private void FixedUpdate()
    {
        if (!IsOwner || _rb == null) return;

        bool isGrounded = IsGrounded();
        Vector3 velocity = _rb.linearVelocity;

        // Ground Snapping
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // Execute Jump
        if (_jumpQueued && isGrounded)
        {
            // Formula: v = sqrt(2 * g * h)
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
            _jumpQueued = false;
        }
        else if (isGrounded)
        {
            _jumpQueued = false;
        }

        // Variable Jump Height (Hold button to float/jump higher)
        if (_isHoldingJump && velocity.y > 0f)
        {
            velocity.y += jumpHoldBoost * Time.fixedDeltaTime;
        }

        // Horizontal Movement
        Vector2 move2d = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (move2d.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(move2d.x, move2d.y) * Mathf.Rad2Deg + _camera.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            _rb.MoveRotation(Quaternion.Euler(0f, angle, 0f));

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            velocity.x = moveDir.normalized.x * speed;
            velocity.z = moveDir.normalized.z * speed;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        // Apply Custom Gravity
        velocity.y -= gravity * Time.fixedDeltaTime;
        _rb.linearVelocity = velocity;
    }

    private bool IsGrounded()
    {
        // Always query the player's current collider (handles collider swapping dynamically)
        Collider currentCollider = GetComponent<Collider>();
        
        if (currentCollider == null) 
        {
            currentCollider = GetComponentInChildren<Collider>();
        }

        if (currentCollider == null) return false;

        // Get the dynamic bottom-center position of whichever collider is currently active
        Vector3 bottomCenter = new Vector3(
            currentCollider.bounds.center.x,
            currentCollider.bounds.min.y + 0.05f, // Tiny offset upward to prevent starting inside the ground
            currentCollider.bounds.center.z
        );

        return Physics.CheckSphere(bottomCenter, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (col == null) return;

        Vector3 bottomCenter = new Vector3(
            col.bounds.center.x, 
            col.bounds.min.y + 0.05f, 
            col.bounds.center.z
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bottomCenter, groundCheckRadius);
    }

}