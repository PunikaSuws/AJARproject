using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TaskData
{
    public string id;
    public string title;
    public int targetAmount = 1;
    public bool startActive = true;   // ติ๊กออก = ซ่อนไว้ก่อน
    [NonSerialized] public int currentAmount;
    [NonSerialized] public bool completed;
    [NonSerialized] public bool active;
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

        foreach (TaskData t in tasks)
            t.active = t.startActive;
    }

    // เพิ่ม Task ด้วยโค้ด (กรณีไม่ได้ใส่ไว้ใน Inspector)
    public void AddTask(TaskData task)
    {
        if (tasks.Exists(t => t.id == task.id)) return;
        task.active = true;
        tasks.Add(task);
        TaskAdded?.Invoke(task);
    }

    // เปิด Task ที่ซ่อนไว้ใน Inspector
    public void ActivateTask(string id)
    {
        TaskData task = tasks.Find(t => t.id == id);
        if (task == null)
        {
            Debug.LogWarning($"ActivateTask: ไม่พบ Task id = {id}");
            return;
        }
        if (task.active || task.completed) return;

        task.active = true;
        TaskAdded?.Invoke(task);
    }

    public void AddProgress(string id, int amount = 1)
    {
        TaskData task = tasks.Find(t => t.id == id);
        if (task == null || !task.active || task.completed) return;

        task.currentAmount = Mathf.Min(task.currentAmount + amount, task.targetAmount);
        TaskUpdated?.Invoke(task);

        if (task.currentAmount >= task.targetAmount)
        {
            task.completed = true;
            TaskCompleted?.Invoke(task);
        }
    }
}