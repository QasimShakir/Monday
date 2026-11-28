using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TaskItem
{
    public string id;
    public string title;
    public bool unlockedAtStart;
    [HideInInspector] public bool unlocked;
    [HideInInspector] public bool done;
}

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [SerializeField] List<TaskItem> tasks = new List<TaskItem>();
    public IReadOnlyList<TaskItem> Tasks => tasks;

    public event Action Changed;
    public event Action<string> TaskCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        foreach (var t in tasks) { t.unlocked = t.unlockedAtStart; t.done = false; }
    }

    TaskItem Find(string id) => tasks.Find(t => t.id == id);

    public void Unlock(string id)
    {
        var t = Find(id);
        if (t == null || t.unlocked) return;
        t.unlocked = true;
        Changed?.Invoke();
    }

    public void Complete(string id)
    {
        var t = Find(id);
        if (t == null || !t.unlocked || t.done) return;
        t.done = true;
        Changed?.Invoke();
        TaskCompleted?.Invoke(id);
    }
}