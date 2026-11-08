using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

public class PlayerInteractor : MonoBehaviour
{
    public float interactionDistance = 3f;
    
    [Header("UI Elements")]
    public Image crosshair;
    public TextMeshProUGUI promptText;

    void Start()
    {
        // Hides the text prompt when the game first starts
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        // 1. Reset UI to default every frame
        if (crosshair != null) crosshair.color = Color.white;
        if (promptText != null) promptText.gameObject.SetActive(false);

        // 2. Shoot the Raycast
        Ray ray = new Ray(transform.position, transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Interactable interactableTarget = hit.collider.GetComponent<Interactable>();
            
            if (interactableTarget != null)
            {
                // 3. We are looking at a target! Change UI to active state.
                if (crosshair != null) crosshair.color = Color.red;
                if (promptText != null) 
                {
                    promptText.text = "[E] Inspect";
                    promptText.gameObject.SetActive(true);
                }

                // 4. Trigger the event if 'E' is pressed
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactableTarget.TriggerInteraction();
                }
            }
        }
    }
}