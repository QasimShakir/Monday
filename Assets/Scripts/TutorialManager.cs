using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    enum Step { NotStarted, GoToDesk, GoToTaskList, DoFirstTask, Finished }
    Step step = Step.NotStarted;

    [Header("Guide")]
    [SerializeField] Transform player;
    [SerializeField] Transform deskPoint;
    [SerializeField] Transform taskListPoint;
    [SerializeField] float deskRadius = 2.5f;
    [SerializeField] GuideMarker marker;

    [Header("First task")]
    [SerializeField] string firstTaskId = "task1";

    [Header("UI")]
    [SerializeField] GameObject quickAccessButton;   // top-left button
    [SerializeField] TMP_Text tipText;
    [SerializeField] float tipSeconds = 8f;
    [TextArea]
    [SerializeField]
    string tipMessage =
        "The more objects you interact with, the more tasks will appear in your task list.\n" +
        "Open it any time with the button in the top-left corner (or press Tab).";

    public bool QuickAccessUnlocked { get; private set; }

    void Awake()
    {
        Instance = this;
        if (quickAccessButton) quickAccessButton.SetActive(false);
        if (tipText) tipText.gameObject.SetActive(false);
    }

    void Start() { TaskManager.Instance.TaskCompleted += OnTaskCompleted; }
    void OnDestroy() { if (TaskManager.Instance != null) TaskManager.Instance.TaskCompleted -= OnTaskCompleted; }

    // Call this when the morning routine ends
    public void BeginAfterMorningRoutine()
    {
        if (step != Step.NotStarted) return;
        step = Step.GoToDesk;
        marker.SetTarget(deskPoint);
    }

    void Update()
    {
        if (step == Step.GoToDesk && player != null)
        {
            Vector3 a = player.position, b = deskPoint.position;
            a.y = b.y = 0f;
            if (Vector3.Distance(a, b) < deskRadius)
            {
                step = Step.GoToTaskList;
                marker.SetTarget(taskListPoint);
            }
        }
    }

    public void NotifyTaskListInteracted()
    {
        if (step == Step.GoToTaskList)
        {
            marker.Hide();
            step = Step.DoFirstTask;
        }
    }

    void OnTaskCompleted(string id)
    {
        if (step != Step.DoFirstTask || id != firstTaskId) return;
        step = Step.Finished;
        QuickAccessUnlocked = true;
        if (quickAccessButton) quickAccessButton.SetActive(true);
        StartCoroutine(ShowTip());
    }

    IEnumerator ShowTip()
    {
        if (tipText == null) yield break;
        tipText.text = tipMessage;
        tipText.gameObject.SetActive(true);
        yield return new WaitForSeconds(tipSeconds);
        tipText.gameObject.SetActive(false);
    }
}