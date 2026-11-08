using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    // This creates a customizable event list in your Unity Inspector
    public UnityEvent onInteract; 

    public void TriggerInteraction()
    {
        onInteract.Invoke();
    }
}