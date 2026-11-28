using System.Text;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TaskListPopup : MonoBehaviour
{
    public static TaskListPopup Instance { get; private set; }

    [SerializeField] GameObject panel;                  // the note image (child of this object)
    [SerializeField] TMP_Text taskText;                 // text inside the note
    [SerializeField] MonoBehaviour[] disableWhileOpen;  // player move + look scripts
    [SerializeField] GameObject[] hideWhileOpen;        // crosshair, interact prompt, etc.

    bool isOpen;
    public bool IsOpen => isOpen;
    bool[] hiddenPrevActive;
    int openedFrame = -1;
    CursorLockMode prevLock;
    bool prevVisible;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    void Start() { TaskManager.Instance.Changed += Refresh; }
    void OnDestroy() { if (TaskManager.Instance != null) TaskManager.Instance.Changed -= Refresh; }

    void Update()
    {
        if (Time.frameCount == openedFrame) return;
        if (isOpen && (KeyDown("esc") || KeyDown("tab"))) Close();
        else if (!isOpen && KeyDown("tab") &&
                 (TutorialManager.Instance == null || TutorialManager.Instance.QuickAccessUnlocked)) Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        openedFrame = Time.frameCount;
        Refresh();
        panel.SetActive(true);
        prevLock = Cursor.lockState;
        prevVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        foreach (var m in disableWhileOpen) if (m) m.enabled = false;

        // Hide crosshair / interact text, remembering their prior state
        hiddenPrevActive = new bool[hideWhileOpen.Length];
        for (int i = 0; i < hideWhileOpen.Length; i++)
        {
            var go = hideWhileOpen[i];
            if (!go) continue;
            hiddenPrevActive[i] = go.activeSelf;
            go.SetActive(false);
        }
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
        Cursor.lockState = prevLock;
        Cursor.visible = prevVisible;
        foreach (var m in disableWhileOpen) if (m) m.enabled = true;

        // Restore only what was visible before opening
        for (int i = 0; i < hideWhileOpen.Length; i++)
        {
            var go = hideWhileOpen[i];
            if (go && hiddenPrevActive != null && hiddenPrevActive[i])
                go.SetActive(true);
        }
    }

    void Refresh()
    {
        var sb = new StringBuilder();
        foreach (var t in TaskManager.Instance.Tasks)
        {
            if (!t.unlocked) continue;
            string line = "· " + t.title;
            sb.AppendLine(t.done ? "<s>" + line + "</s>" : line);
        }
        taskText.text = sb.ToString();
    }

    static bool KeyDown(string name)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return false;
        if (name == "esc") return kb.escapeKey.wasPressedThisFrame;
        if (name == "tab") return kb.tabKey.wasPressedThisFrame;
        return false;
#else
        if (name == "esc") return Input.GetKeyDown(KeyCode.Escape);
        if (name == "tab") return Input.GetKeyDown(KeyCode.Tab);
        return false;
#endif
    }
}