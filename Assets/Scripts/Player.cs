using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    private Rigidbody _rb;
    private Camera _camera;

    private bool _jumpQueued;
    private bool _isHoldingJump;
    public Collider _mainCollider;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float gravity = 19.62f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpHoldBoost = 10f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;   // NEW: stronger pull once falling
    [SerializeField] private float lowJumpGravityMultiplier = 2f;  // NEW: extra pull if jump released early

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;

    public override void OnNetworkSpawn()
    {
        _rb = GetComponent<Rigidbody>();
        _camera = GetComponentInChildren<Camera>();

        if (_rb != null)
        {
            _rb.useGravity = false;
            _rb.interpolation = IsOwner ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
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
        _mainCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            GetComponent<DetectObject>()?.detectObject();
        }

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

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (_jumpQueued && isGrounded)
        {
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
            _jumpQueued = false;
        }
        else if (isGrounded)
        {
            _jumpQueued = false;
        }

        if (_isHoldingJump && velocity.y > 0f)
        {
            velocity.y += jumpHoldBoost * Time.fixedDeltaTime;
        }

        // Horizontal Movement — camera-relative, no rotation ownership here.
        Vector2 move2d = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (move2d.magnitude >= 0.1f)
        {
            float camY = _camera.transform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, camY, 0f) * new Vector3(move2d.x, 0f, move2d.y);
            velocity.x = moveDir.normalized.x * speed;
            velocity.z = moveDir.normalized.z * speed;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        // --- Variable gravity based on jump phase ---
        float appliedGravity = gravity;

        if (velocity.y < 0f)
        {
            // Falling — pull down harder so descent feels snappy instead of floaty.
            appliedGravity = gravity * fallGravityMultiplier;
        }
        else if (velocity.y > 0f && !_isHoldingJump)
        {
            // Rising but the player let go of Jump early — cut the arc short
            // for responsive variable-height jumps, rather than a full float to peak.
            appliedGravity = gravity * lowJumpGravityMultiplier;
        }
        // else: still rising and holding Jump — use base gravity for a smooth, full-height ascent.

        velocity.y -= appliedGravity * Time.fixedDeltaTime;
        _rb.linearVelocity = velocity;
    }

    private bool IsGrounded()
    {
        Collider currentCollider = GetComponent<Collider>();

        if (currentCollider == null)
        {
            currentCollider = GetComponentInChildren<Collider>();
        }

        if (currentCollider == null) return false;

        Vector3 bottomCenter = new Vector3(
            currentCollider.bounds.center.x,
            currentCollider.bounds.min.y + 0.05f,
            currentCollider.bounds.center.z
        );

        Collider[] hits = Physics.OverlapSphere(bottomCenter, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform) || hit.transform == transform)
            {
                continue;
            }

            return true;
        }

        return false;
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