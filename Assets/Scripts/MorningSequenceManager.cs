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

    void Start()
    {
        playerBody.position = bedPosition.position;
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        Vector3 shutScale = new Vector3(1f, 1f, 1f);
        topLid.localScale = shutScale;
        bottomLid.localScale = shutScale;

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
        // 1. POST-ALARM BLINKS: Blink heavily a couple of times before standing up
        int wakeUpBlinks = 2;
        for (int i = 0; i < wakeUpBlinks; i++)
        {
            // Slam eyes shut
            yield return StartCoroutine(LerpEyelids(topLid.localScale.y, 1f, 0.15f));
            yield return new WaitForSeconds(0.1f);
            
            // Open them slightly wider
            yield return StartCoroutine(LerpEyelids(1f, 0.7f, 0.25f));
            yield return new WaitForSeconds(0.15f);
        }
        
        // Final heavy blink shut before the big reveal
        yield return StartCoroutine(LerpEyelids(topLid.localScale.y, 1f, 0.15f));
        yield return new WaitForSeconds(0.2f);

        // 2. STAND UP AND CLEAR VISION: Smoothly fade everything over 3 seconds
        float elapsedTime = 0f;
        Vector3 targetScale = new Vector3(1f, 0f, 1f);

        globalVolume.profile.TryGet(out DepthOfField dof);
        float startBlur = dof != null ? dof.gaussianMaxRadius.value : 0f;
        
        Vector3 startPos = bedPosition.position;
        Vector3 endPos = standingPosition.position;
        
        float startSluggishness = playerLookScript.drowsySluggishness;
        float startYRotation = playerLookScript.yRotation; 

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration; 

            // Open eyes wide and fade blur
            topLid.localScale = Vector3.Lerp(new Vector3(1f, 1f, 1f), targetScale, t);
            bottomLid.localScale = Vector3.Lerp(new Vector3(1f, 1f, 1f), targetScale, t);
            if (dof != null) dof.gaussianMaxRadius.value = Mathf.Lerp(startBlur, 0f, t);
            
            // Move body to floor and fix rotation
            playerLookScript.drowsySluggishness = Mathf.Lerp(startSluggishness, 0f, t);
            playerBody.position = Vector3.Lerp(startPos, endPos, t); 
            playerLookScript.yRotation = Mathf.Lerp(startYRotation, 0f, t); 

            yield return null; 
        }

        // Cleanup
        if (dof != null) dof.active = false;
        playerLookScript.isDrowsy = false;
        topLid.gameObject.SetActive(false);
        bottomLid.gameObject.SetActive(false);
        
        if (playerMovementScript != null) playerMovementScript.enabled = true;
    }

    // Helper function to animate the eyelids manually without the Update loop
    private IEnumerator LerpEyelids(float startY, float endY, float duration)
    {
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentY = Mathf.Lerp(startY, endY, t);
            Vector3 scale = new Vector3(1f, currentY, 1f);
            topLid.localScale = scale;
            bottomLid.localScale = scale;
            yield return null;
        }
        // Snap to exact final value
        Vector3 finalScale = new Vector3(1f, endY, 1f);
        topLid.localScale = finalScale;
        bottomLid.localScale = finalScale;
    }
}