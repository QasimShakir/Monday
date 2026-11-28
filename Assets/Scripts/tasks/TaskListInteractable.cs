using System.Collections;
using UnityEngine;

public class TaskListInteractable : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer scroll;
    [SerializeField] float unrollSeconds = 1.5f;   // how long the unroll takes

    bool opened, busy;
    int shapeCount;

    void Awake()
    {
        if (scroll == null) scroll = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    void Start()
    {
        shapeCount = scroll != null && scroll.sharedMesh != null ? scroll.sharedMesh.blendShapeCount : 0;
        Debug.Log("TaskList scroll has " + shapeCount + " blendshapes");
        SetRoll(1f);   // start rolled up
    }

    // Call this from your Interactable's On Interact event
    public void Interact()
    {
        if (busy) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        busy = true;
        if (TutorialManager.Instance != null) TutorialManager.Instance.NotifyTaskListInteracted();

        if (!opened)
        {
            opened = true;
            float t = 0f;
            while (t < unrollSeconds)
            {
                t += Time.deltaTime;
                SetRoll(1f - Mathf.Clamp01(t / unrollSeconds));
                yield return null;
            }
            SetRoll(0f);
        }

        TaskListPopup.Instance.Open();
        busy = false;
    }

    // 0 = flat, 1 = fully rolled
    void SetRoll(float roll)
    {
        if (scroll == null || shapeCount < 2) return;

        float pos = Mathf.Clamp01(roll) * (shapeCount - 1);
        int i = Mathf.Min(Mathf.FloorToInt(pos), shapeCount - 2);
        float frac = pos - i;

        for (int k = 0; k < shapeCount; k++) scroll.SetBlendShapeWeight(k, 0f);
        scroll.SetBlendShapeWeight(i, (1f - frac) * 100f);
        scroll.SetBlendShapeWeight(i + 1, frac * 100f);
    }
}