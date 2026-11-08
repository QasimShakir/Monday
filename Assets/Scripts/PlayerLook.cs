using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Transform playerBody; 
    public float sensitivity = 2f;
    public bool isDrowsy = true; 
    public float drowsySluggishness = 0.5f; 

    public float yRotation = 0f; 
    private float xRotation = 0f;

    private float currentMouseX, currentMouseY;
    private float velocityX, velocityY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float targetMouseX = Input.GetAxisRaw("Mouse X") * sensitivity;
        float targetMouseY = Input.GetAxisRaw("Mouse Y") * sensitivity;

        if (isDrowsy)
        {
            currentMouseX = Mathf.SmoothDamp(currentMouseX, targetMouseX, ref velocityX, drowsySluggishness);
            currentMouseY = Mathf.SmoothDamp(currentMouseY, targetMouseY, ref velocityY, drowsySluggishness);
            yRotation += currentMouseX;
        }
        else
        {
            currentMouseX = targetMouseX;
            currentMouseY = targetMouseY;
            playerBody.Rotate(Vector3.up * currentMouseX);
        }

        xRotation -= currentMouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Z-axis is hardcoded to 0f so the head never tilts
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}