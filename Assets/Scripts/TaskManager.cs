using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TaskData
{
    public string id;                 // เช่น "collect_items"
    public string title;              // ข้อความที่โชว์
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
        {
            t.currentAmount = 0;
            t.completed = false;
            t.active = t.startActive;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddTask(TaskData task)
    {
        if (tasks.Exists(t => t.id == task.id)) return;
        task.active = true;
        tasks.Add(task);
        TaskAdded?.Invoke(task);
    }

    public void ActivateTask(string id)
    {
        TaskData task = tasks.Find(t => t.id == id);
        if (task == null)
        {
            Debug.LogWarning("ActivateTask: cannot find Task id = " + id);
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

    /// <summary>true ถ้า Task นี้ถูกเปิดแล้วและยังไม่เสร็จ</summary>
    public bool IsActive(string id)
    {
        TaskData task = tasks.Find(t => t.id == id);
        return task != null && task.active && !task.completed;
    }

    public bool IsCompleted(string id)
    {
        TaskData task = tasks.Find(t => t.id == id);
        return task != null && task.completed;
    }

    /// <summary>ทำให้ Task เสร็จทันที (ใช้ปิด Task ที่ไม่จำเป็นแล้ว เช่น ขว้างผีค้างอยู่)</summary>
    public void ForceComplete(string id)
    {
        TaskData task = tasks.Find(t => t.id == id);
        if (task == null || !task.active || task.completed) return;

        task.currentAmount = task.targetAmount;
        TaskUpdated?.Invoke(task);
        task.completed = true;
        TaskCompleted?.Invoke(task);
    }
}
