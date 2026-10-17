using System.Collections.Generic;
using UnityEngine;

public class TaskListUI : MonoBehaviour
{
    [SerializeField] TaskItemUI itemPrefab;
    [SerializeField] Transform container;   // Panel ที่มี Vertical Layout Group

    readonly Dictionary<TaskData, TaskItemUI> items = new Dictionary<TaskData, TaskItemUI>();
    TaskManager manager;

    void Start()
    {
        if (itemPrefab == null || container == null)
        {
            Debug.LogError($"{name}: Assign Item Prefab and Container.", this);
            return;
        }

        manager = TaskManager.Instance;
        if (manager == null)
        {
            Debug.LogError($"{name}: TaskManager not found in scene!", this);
            return;
        }

        manager.TaskAdded += OnTaskAdded;
        manager.TaskUpdated += OnTaskUpdated;
        manager.TaskCompleted += OnTaskCompleted;

        // สร้างแถว
        foreach (TaskData t in manager.Tasks)
            if (t.active)
                OnTaskAdded(t);
    }

    void OnDestroy()
    {
        if (manager == null) return;
        manager.TaskAdded -= OnTaskAdded;
        manager.TaskUpdated -= OnTaskUpdated;
        manager.TaskCompleted -= OnTaskCompleted;
    }

    void OnTaskAdded(TaskData task)
    {
        if (items.ContainsKey(task) || task.completed)
            return;

        Debug.Log($"OnTaskAdded: {task.title}");

        TaskItemUI item = Instantiate(itemPrefab, container);
        item.Setup(task);
        items[task] = item;
    }

    void OnTaskUpdated(TaskData task)
    {
        if (items.TryGetValue(task, out TaskItemUI item) && item != null)
            item.Refresh();
    }

    void OnTaskCompleted(TaskData task)
    {
        if (items.TryGetValue(task, out TaskItemUI item) && item != null)
            item.PlayCompleted();

        items.Remove(task);
    }
}