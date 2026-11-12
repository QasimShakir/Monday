using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MorningSequenceManager : MonoBehaviour
{
    [Header("Scene References")]
    public AudioSource alarmClockAudio;
    public RectTransform topLid;
    public RectTransform bottomLid;
    public Volume globalVolume;
    public PlayerLook playerLookScript;

    [Header("Movement References")]
    public Transform playerBody;
    public Transform bedPosition;
    public Transform standingPosition;
    public MonoBehaviour playerMovementScript;

    [Header("Blinking Settings")]
    public float blinkSpeed = 0.5f;
    public float blinkOpenScale = 0.95f;
    public float initialBlackoutTime = 2f;

    [Header("Wake Up Settings")]
    public float fadeDuration = 3f;

    private bool isWakingUp = false;
    private bool canBlink = false;

    // Cached Depth of Field override (the Volume itself is never touched)
    private DepthOfField dof;

    void Start()
    {
        // Cache the DoF override once
        if (globalVolume == null)
        {
            Debug.LogError("[Morning] globalVolume is not assigned!");
        }
        else if (!globalVolume.profile.TryGet(out dof))
        {
            Debug.LogWarning("[Morning] No DepthOfField override found on the Volume profile.");
        }

        // Snap player to bed and lock movement
        playerBody.position = bedPosition.position;
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        // Force eyelids completely shut (100% black screen)
        topLid.localScale = Vector3.one;
        bottomLid.localScale = Vector3.one;

        StartCoroutine(InitialBlackoutRoutine());
    }

    private IEnumerator InitialBlackoutRoutine()
    {
        yield return new WaitForSeconds(initialBlackoutTime);
        canBlink = true;
    }

    void Update()
    {
        if (canBlink && !isWakingUp)
        {
            float pingPong = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            float currentScaleY = Mathf.Lerp(1f, blinkOpenScale, pingPong);

            Vector3 currentScale = new Vector3(1f, currentScaleY, 1f);
            topLid.localScale = currentScale;
            bottomLid.localScale = currentScale;
        }
    }

    public void StopAlarmAndWakeUp()
    {
        if (isWakingUp) return;

        isWakingUp = true;
        canBlink = false;
        alarmClockAudio.Stop();

        StartCoroutine(WakeUpSequenceRoutine());
    }

    private IEnumerator WakeUpSequenceRoutine()
    {
        // 1. Post-alarm blinks before standing up
        int wakeUpBlinks = 2;
        for (int i = 0; i < wakeUpBlinks; i++)
        {
            yield return StartCoroutine(LerpEyelids(topLid.localScale.y, 1f, 0.15f));
            yield return new WaitForSeconds(0.1f);

            yield return StartCoroutine(LerpEyelids(1f, 0.7f, 0.25f));
            yield return new WaitForSeconds(0.15f);
        }

        yield return StartCoroutine(LerpEyelids(topLid.localScale.y, 1f, 0.15f));
        yield return new WaitForSeconds(0.2f);

        // 2. Awake: blur goes off instantly (eyelids are fully shut here,
        //    so there's no visible pop when they start opening)
        KillBlur();

        // 3. Stand up and open eyes
        float elapsedTime = 0f;
        Vector3 closed = Vector3.one;
        Vector3 open = new Vector3(1f, 0f, 1f);

        Vector3 startPos = bedPosition.position;
        Vector3 endPos = standingPosition.position;

        float startSluggishness = playerLookScript.drowsySluggishness;
        float startYRotation = playerLookScript.yRotation;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);

            // Eyelids open fully
            topLid.localScale = Vector3.Lerp(closed, open, t);
            bottomLid.localScale = Vector3.Lerp(closed, open, t);

            // Move player capsule to standing position & restore camera response
            playerLookScript.drowsySluggishness = Mathf.Lerp(startSluggishness, 0f, t);
            playerBody.position = Vector3.Lerp(startPos, endPos, t);
            playerLookScript.yRotation = Mathf.Lerp(startYRotation, 0f, t);

            yield return null;
        }

        // Final snap
        playerLookScript.isDrowsy = false;
        playerLookScript.drowsySluggishness = 0f;
        playerBody.position = endPos;

        topLid.gameObject.SetActive(false);
        bottomLid.gameObject.SetActive(false);

        if (playerMovementScript != null) playerMovementScript.enabled = true;

        // Safety net: re-apply in case another script re-enabled DoF this frame
        KillBlur();
        yield return new WaitForEndOfFrame();
        KillBlur();
    }

    // Only turns off the Depth of Field override. The Volume stays ticked at
    // weight 1, so Bloom, Vignette, etc. keep working.
    private void KillBlur()
    {
        if (dof == null) return;

        dof.active = false;
    }

    private IEnumerator LerpEyelids(float startY, float endY, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentY = Mathf.Lerp(startY, endY, t);
            Vector3 scale = new Vector3(1f, currentY, 1f);
            topLid.localScale = scale;
            bottomLid.localScale = scale;
            yield return null;
        }
        Vector3 finalScale = new Vector3(1f, endY, 1f);
        topLid.localScale = finalScale;
        bottomLid.localScale = finalScale;
    }
}