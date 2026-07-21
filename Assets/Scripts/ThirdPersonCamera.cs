using UnityEngine;
using Unity.Netcode;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Camera Settings")]
    public float mouseSensitivity = 200f;

    [Header("Constraints")]
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    private float distanceToPlayer;
    private float cameraHeight;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float playerRotationY = 0f;
    private bool isPlayerRotationLocked = false;

    private NetworkObject _networkObject;
    private Rigidbody _playerRb; // NEW

    void Start()
    {
        player = this.transform.parent;
        _networkObject = player != null ? player.GetComponent<NetworkObject>() : null;
        _playerRb = player != null ? player.GetComponent<Rigidbody>() : null; // NEW

        if (_networkObject != null && !_networkObject.IsOwner)
        {
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player != null)
        {
            cameraHeight = transform.position.y - player.position.y;

            Vector3 playerFlatPos = new Vector3(player.position.x, 0, player.position.z);
            Vector3 cameraFlatPos = new Vector3(transform.position.x, 0, transform.position.z);
            distanceToPlayer = Vector3.Distance(playerFlatPos, cameraFlatPos);

            rotationY = transform.eulerAngles.y;
            rotationX = transform.eulerAngles.x;
            playerRotationY = rotationY;

            if (rotationX > 180) rotationX -= 360;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isPlayerRotationLocked = !isPlayerRotationLocked;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationY += mouseX;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        if (!isPlayerRotationLocked)
        {
            playerRotationY = rotationY;
        }

        // Route rotation through the Rigidbody instead of writing transform directly,
        // so PhysX doesn't overwrite it on the next FixedUpdate.
        if (_playerRb != null)
        {
            _playerRb.MoveRotation(Quaternion.Euler(0f, playerRotationY, 0f));
        }
        else
        {
            player.rotation = Quaternion.Euler(0f, playerRotationY, 0f);
        }

        Quaternion cameraRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        Vector3 targetOffset = new Vector3(0, cameraHeight, -distanceToPlayer);
        Vector3 targetPosition = player.position + (cameraRotation * targetOffset);

        transform.position = targetPosition;
        transform.LookAt(player.position + Vector3.up * cameraHeight);
    }
}