using System.Collections;
using UnityEngine;

// Put this on WindowCurtains_Trigger, next to a (non-trigger) BoxCollider and an Interactable.
// Interactable > On Interact -> CurtainInteraction.OpenCurtains()
public class CurtainInteraction : MonoBehaviour
{
    [Header("Curtains (pivots at the outer window edges)")]
    [SerializeField] private Transform leftCurtain;
    [SerializeField] private Transform rightCurtain;
    [SerializeField, Range(0.01f, 1f)] private float openScaleX = 0.2f;
    [SerializeField] private float openDuration = 2f;

    [Header("Light")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Color dimColor = new Color(0.55f, 0.65f, 0.85f);
    [SerializeField] private float dimIntensity = 0.3f;
    [SerializeField] private Color brightColor = new Color(1f, 0.85f, 0.65f);
    [SerializeField] private float brightIntensity = 1.3f;

    [Header("Player")]
    [Tooltip("Curtains can't be opened while this is still drowsy (in bed).")]
    [SerializeField] private PlayerLook playerLookScript;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource curtainAudio;

    private bool isOpen;

    void Start()
    {
        if (playerLookScript == null) playerLookScript = FindFirstObjectByType<PlayerLook>();

        if (directionalLight == null)
        {
            Debug.LogWarning("[Curtains] directionalLight is not assigned.", this);
            return;
        }

        directionalLight.color = dimColor;
        directionalLight.intensity = dimIntensity;
    }

    public void OpenCurtains()
    {
        if (isOpen) return;

        // Still in bed or standing up
        if (playerLookScript != null && playerLookScript.isDrowsy) return;

        isOpen = true;

        // Already used: stop showing the "[E] Inspect" prompt
        if (TryGetComponent(out Collider trigger)) trigger.enabled = false;

        if (curtainAudio != null) curtainAudio.Play();
        if (playerLookScript != null) playerLookScript.drowsySluggishness = 0f;

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        float leftStart = leftCurtain != null ? leftCurtain.localScale.x : 1f;
        float rightStart = rightCurtain != null ? rightCurtain.localScale.x : 1f;

        float elapsed = 0f;
        float t = 0f;
        while (t < 1f)
        {
            elapsed += Time.deltaTime;
            t = openDuration > 0f ? Mathf.Clamp01(elapsed / openDuration) : 1f;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            // openScaleX is relative to each pivot's closed scale
            SetScaleX(leftCurtain, Mathf.Lerp(leftStart, leftStart * openScaleX, eased));
            SetScaleX(rightCurtain, Mathf.Lerp(rightStart, rightStart * openScaleX, eased));

            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(dimColor, brightColor, eased);
                directionalLight.intensity = Mathf.Lerp(dimIntensity, brightIntensity, eased);
            }

            yield return null;
        }
    }

    private static void SetScaleX(Transform target, float x)
    {
        if (target == null) return;

        Vector3 scale = target.localScale;
        scale.x = x;
        target.localScale = scale;
    }
}
