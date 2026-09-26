using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    private CharacterController controller;
    private Transform cameraTransform;
    private float verticalVelocity;
    private float cameraPitch;
    private float yawRotation;
    private bool wasInputActive;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        yawRotation = transform.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        bool inputActive = !DebugChatUI.IsChatActive
                           && Cursor.lockState == CursorLockMode.Locked;

        // Skip one frame after any state change to avoid input spikes
        bool stateChanged = inputActive != wasInputActive;
        wasInputActive = inputActive;

        if (inputActive && !stateChanged)
        {
            // Unlock/lock cursor with left Alt
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            HandleMouseLook();
            HandleMovement();
        }
        else
        {
            // Re-lock cursor with left Alt when chat is not active
            if (!DebugChatUI.IsChatActive && Input.GetKeyDown(KeyCode.LeftAlt))
                Cursor.lockState = CursorLockMode.Locked;

            // Keep gravity running so the player doesn't float
            ApplyGravity();
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Horizontal rotation on player body
        yawRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yawRotation, 0f);

        // Vertical rotation on camera only
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move *= moveSpeed;

        // Add gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
    }
}
