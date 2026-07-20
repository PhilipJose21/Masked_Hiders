using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform player;       

    [Header("Camera Settings")]
    public float mouseSensitivity = 200f;

    [Header("Constraints")]
    public float minVerticalAngle = -20f; 
    public float maxVerticalAngle = 60f;  

    // These will now automatically capture your Inspector layout
    private float distanceToPlayer;   
    private float cameraHeight;      

    private float rotationX = 0f;
    private float rotationY = 0f;

    // Separate variable to lock the player's horizontal rotation
    private float playerRotationY = 0f;

    // Toggle flag to lock/unlock player rotation
    private bool isPlayerRotationLocked = false;

    void Start()
    {
        // Locks the cursor to the center of the screen and hides it
        player = this.transform.parent; // Assuming the camera is a child of the player
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player != null)
        {
            // 1. Calculate height difference between camera and player
            cameraHeight = transform.position.y - player.position.y;

            // 2. Calculate horizontal distance (flattening the Y axis)
            Vector3 playerFlatPos = new Vector3(player.position.x, 0, player.position.z);
            Vector3 cameraFlatPos = new Vector3(transform.position.x, 0, transform.position.z);
            distanceToPlayer = Vector3.Distance(playerFlatPos, cameraFlatPos);

            // 3. Initialize rotation variables based on current camera angles
            rotationY = transform.eulerAngles.y;
            rotationX = transform.eulerAngles.x;
            playerRotationY = rotationY;
            
            // Normalize rotationX if it's wrapping around 360 degrees
            if (rotationX > 180) rotationX -= 360;
        }
    }

    void Update()
    {
        // Toggle player rotation lock state when 'Q' is pressed
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isPlayerRotationLocked = !isPlayerRotationLocked;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Camera always processes mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationY += mouseX;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle); 

        // Only update the player's target rotation if NOT locked
        if (!isPlayerRotationLocked)
        {
            playerRotationY = rotationY;
        }

        // 1. Rotate the Player using playerRotationY (stays fixed if Q was toggled)
        player.rotation = Quaternion.Euler(0f, playerRotationY, 0f);

        // 2. Position and rotate the Camera around the player using active camera rotationY
        Quaternion cameraRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        
        Vector3 targetOffset = new Vector3(0, cameraHeight, -distanceToPlayer);
        Vector3 targetPosition = player.position + (cameraRotation * targetOffset);

        // Apply final position and rotation to the camera
        transform.position = targetPosition;
        transform.LookAt(player.position + Vector3.up * cameraHeight);
    }
}