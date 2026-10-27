using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactionDistance = 3f;

    void Update()
    {
        // Shoots the raycast when the player presses the 'E' key
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            {
                // Checks if the object we looked at has our Interactable script
                Interactable interactableTarget = hit.collider.GetComponent<Interactable>();

                if (interactableTarget != null)
                {
                    interactableTarget.TriggerInteraction();
                }
            }
        }
    }
}