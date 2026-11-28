using UnityEngine;

public class GuideMarker : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] float bobHeight = 0.08f;
    [SerializeField] float bobSpeed = 3f;

    Transform target;

    void Awake() { gameObject.SetActive(false); }

    public void SetTarget(Transform t)
    {
        target = t;
        gameObject.SetActive(t != null);
    }

    public void Hide() { SetTarget(null); }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 p = target.position + offset;
        p.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = p;

        var cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(cam.transform.forward);
    }
}