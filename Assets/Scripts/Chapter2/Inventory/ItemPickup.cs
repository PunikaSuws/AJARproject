using UnityEngine;
using UnityEngine.Events;

/// <summary>ไอเท็มที่เก็บได้ด้วยปุ่ม E ใส่ที่วัตถุในฉาก (ต้องมี Collider ที่ไม่ใช่ Trigger)</summary>
public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData item;
    [SerializeField] string promptFormat = "[E] เก็บ {0}";

    [Header("Task")]
    [Tooltip("Task ที่ต้องเปิดอยู่ถึงจะเก็บได้ (เว้นว่าง = เก็บได้เสมอ)")]
    [SerializeField] string requiredTaskId = "";
    [Tooltip("Task ที่จะเพิ่มความคืบหน้า +1 เมื่อเก็บ")]
    [SerializeField] string progressTaskId = "";

    [Header("Detail window")]
    [SerializeField] bool showDetailOnPickup = true;

    [Header("Events")]
    public UnityEvent onPicked;
    public UnityEvent onDetailClosed;

    bool taken;

    public string Prompt { get { return string.Format(promptFormat, item != null ? item.displayName : "?"); } }

    public bool CanInteract
    {
        get
        {
            if (taken || item == null) return false;
            if (string.IsNullOrEmpty(requiredTaskId)) return true;
            return TaskManager.Instance != null && TaskManager.Instance.IsActive(requiredTaskId);
        }
    }

    public void Interact()
    {
        if (!CanInteract) return;
        taken = true;

        if (InventoryManager.Instance != null) InventoryManager.Instance.Add(item);
        if (item.pickupSound != null) AudioSource.PlayClipAtPoint(item.pickupSound, transform.position);

        onPicked.Invoke();

        // ซ่อนวัตถุในฉากก่อน แล้วค่อยนับ Task (กันกดซ้ำ)
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        if (showDetailOnPickup && ItemDetailView.Instance != null)
            ItemDetailView.Instance.Show(item, OnClosed);
        else
            OnClosed();

        if (!string.IsNullOrEmpty(progressTaskId) && TaskManager.Instance != null)
            TaskManager.Instance.AddProgress(progressTaskId);
    }

    void OnClosed()
    {
        onDetailClosed.Invoke();
        gameObject.SetActive(false);
    }
}
