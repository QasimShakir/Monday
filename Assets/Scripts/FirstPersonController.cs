using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;

    private Transform playerCamera;
    private CharacterController controller;
    private float cameraVerticalAngle = 0f;
    private float velocityY = 0f;
    private float gravity = -9.81f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Automatically finds the Main Camera child object so you don't have to drag and drop it
        playerCamera = GetComponentInChildren<Camera>().transform;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. Camera Look
        float inputX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float inputY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraVerticalAngle -= inputY;
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -90f, 90f);
        playerCamera.localEulerAngles = Vector3.right * cameraVerticalAngle;
        transform.Rotate(Vector3.up * inputX);

        // 2. Movement & Collision
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // 3. Gravity
        if (controller.isGrounded && velocityY < 0)
        {
            velocityY = -2f;
        }
        velocityY += gravity * Time.deltaTime;
        move.y = velocityY;

        // Move uses the built-in collision engine
        controller.Move(move * speed * Time.deltaTime);
    }
}