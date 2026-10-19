using UnityEngine;
using UnityEngine.Events;

/// <summary>วัตถุกด E ทั่วไปที่ผูกกับ Task (กดพระพุทธรูป, จุดธูป ฯลฯ) ใช้ได้เมื่อ Task ที่กำหนดเปิดอยู่เท่านั้น</summary>
public class TaskInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string prompt = "[E] ใช้งาน";
    [SerializeField] string requiredTaskId = "";
    [SerializeField] string progressTaskId = "";
    [SerializeField] bool once = true;
    [SerializeField] AudioClip sound;
    public UnityEvent onInteract;

    bool used;

    public string Prompt { get { return prompt; } }

    public bool CanInteract
    {
        get
        {
            if (used && once) return false;
            if (string.IsNullOrEmpty(requiredTaskId)) return true;
            return TaskManager.Instance != null && TaskManager.Instance.IsActive(requiredTaskId);
        }
    }

    public void Interact()
    {
        if (!CanInteract) return;
        used = true;

        if (sound != null) GameAudio.PlaySfx(sound, transform.position);
        onInteract.Invoke();

        if (!string.IsNullOrEmpty(progressTaskId) && TaskManager.Instance != null)
            TaskManager.Instance.AddProgress(progressTaskId);
    }
}
