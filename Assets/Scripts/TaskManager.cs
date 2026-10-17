using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TaskData
{
    public string id;           // เช่น "collect_key"
    public string title;        // เช่น "เก็บกุญแจ"
    public int targetAmount = 1;
    [NonSerialized] public int currentAmount;
    [NonSerialized] public bool completed;
}

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [SerializeField] List<TaskData> tasks = new List<TaskData>();

    public IReadOnlyList<TaskData> Tasks { get { return tasks; } }

    public event Action<TaskData> TaskUpdated;
    public event Action<TaskData> TaskCompleted;
    public event Action<TaskData> TaskAdded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddTask(TaskData task)
    {
        tasks.Add(task);
        TaskAdded?.Invoke(task);
    }

    // เรียกตอนเก็บของ/ทำสำเร็จ เช่น TaskManager.Instance.AddProgress("collect_key");
    public void AddProgress(string id, int amount = 1)
    {
        TaskData task = tasks.Find(t => t.id == id);
        if (task == null || task.completed) return;

        task.currentAmount = Mathf.Min(task.currentAmount + amount, task.targetAmount);
        TaskUpdated?.Invoke(task);

        if (task.currentAmount >= task.targetAmount)
        {
            task.completed = true;
            TaskCompleted?.Invoke(task);
        }
    }
}