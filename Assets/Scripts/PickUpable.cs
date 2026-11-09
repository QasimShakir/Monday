using UnityEngine;


//on any object the player can pick up (books, cans, clothes...).
//Needs a Collider. Unity adds the Rigidbody automatically.
//while carried: physics and colliders are switched off, so the object can't fling away, push the player, or block the interaction ray.

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Pickupable : MonoBehaviour
{
    private Rigidbody body;
    private Transform originalParent;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void PickUp(Transform holdPoint)
    {
        originalParent = transform.parent;

        // Center the visible mesh on the hold point (imported models often have off-center pivots)
        Vector3 offset = GetBounds().center - transform.position;

        body.isKinematic = true;
        SetColliders(false);
        transform.SetParent(holdPoint, true);
        transform.position = holdPoint.position - offset;
    }

    public void Drop()
    {
        transform.SetParent(originalParent, true);
        SetColliders(true);
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void SetColliders(bool enabled)
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = enabled;
    }

    private Bounds GetBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}