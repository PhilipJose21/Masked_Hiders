using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    private CharacterController _controller;
    private Camera _camera;
    // private Animator animator;

    private float turnSmoothVelocity;
    private float verticalVelocity;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpHoldBoost = 8f;


    public override void OnNetworkSpawn()
    {
        _controller = GetComponent<CharacterController>();
        _camera = this.GetComponentInChildren<Camera>();
        // animator = GetComponent<Animator>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (!IsOwner)
        {
            _camera.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        Vector2 move2d = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        Vector3 move = new Vector3(move2d.x, 0f, move2d.y);

        if (_controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (Input.GetButtonDown("Jump") && _controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
        }

        if (Input.GetButton("Jump") && verticalVelocity > 0f)
        {
            verticalVelocity += jumpHoldBoost * Time.deltaTime;
        }

        if (move2d.magnitude >= 0.1f)
        {
            var TargetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + _camera.transform.eulerAngles.y;
            var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, TargetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, TargetAngle, 0f) * Vector3.forward;
            _controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        verticalVelocity -= gravity * Time.deltaTime;
        _controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
