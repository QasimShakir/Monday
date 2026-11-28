using UnityEngine;

public class TaskTrigger : MonoBehaviour
{
    public string taskId;
    public void Unlock() { TaskManager.Instance.Unlock(taskId); }
    public void Complete() { TaskManager.Instance.Complete(taskId); }
}