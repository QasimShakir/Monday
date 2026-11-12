using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    public float interactionDistance = 3f;
    public float holdDistance = 1.5f;

    [Header("UI Elements")]
    public Image crosshair;
    public TextMeshProUGUI promptText;

    private Pickupable held;
    private Transform holdPoint;

    void Start()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);

        // Empty child of the camera where carried objects sit
        holdPoint = new GameObject("HoldPoint").transform;
        holdPoint.SetParent(transform);
        holdPoint.localPosition = Vector3.forward * holdDistance;
        holdPoint.localRotation = Quaternion.identity;
    }

    void Update()
    {
        // Reset UI every frame
        if (crosshair != null) crosshair.color = Color.white;
        if (promptText != null) promptText.gameObject.SetActive(false);

        // Carrying something: click to drop
        if (held != null)
        {
            ShowPrompt("[Left Click] Drop");
            if (Input.GetMouseButtonDown(0)) Drop();
            return;
        }

        // Look for something to interact with (trigger volumes are ignored so they can't block the ray)
        Ray ray = new Ray(transform.position, transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance,
                             Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
        Pickupable pickupable = hit.collider.GetComponentInParent<Pickupable>();

        if (interactable != null)
        {
            ShowPrompt(pickupable != null ? "[E] Inspect   [Left Click] Pick up" : "[E] Inspect");
            if (Input.GetKeyDown(KeyCode.E)) interactable.TriggerInteraction();
        }
        else if (pickupable != null)
        {
            ShowPrompt("[Left Click] Pick up");
        }

        if (pickupable != null && Input.GetMouseButtonDown(0))
        {
            held = pickupable;
            held.PickUp(holdPoint);
        }
    }

    void Drop()
    {
        held.Drop();
        held = null;
    }

    void ShowPrompt(string message)
    {
        if (crosshair != null) crosshair.color = Color.red;
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
        }
    }
}