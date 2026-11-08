using UnityEngine;

public class DoorController : MonoBehaviour
{
    public bool isOpen = false;
    public float openAngle = 90f; 
    public float swingSpeed = 5f;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = Quaternion.Euler(transform.localEulerAngles + Vector3.up * openAngle);
    }

    void Update()
    {
        // Smoothly rotates towards the open or closed position every frame
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * swingSpeed);
    }

    // We will trigger this function using your Interactable script
    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }
}