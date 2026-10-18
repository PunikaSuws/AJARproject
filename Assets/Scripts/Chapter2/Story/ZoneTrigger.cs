using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// พื้นที่ Trigger (Collider ติ๊ก Is Trigger) ผู้เล่นเดินเข้าแล้ว: นับ Task + เรียก Event
/// ผู้เล่นต้องมี Rigidbody และ Tag = "Player"
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [Tooltip("Task ที่เพิ่มความคืบหน้า (เว้นว่าง = ไม่นับ)")]
    [SerializeField] string progressTaskId = "";
    [Tooltip("ทำงานเฉพาะตอน Task นี้เปิดอยู่ (เว้นว่าง = ทำงานเสมอ)")]
    [SerializeField] string requiredTaskId = "";
    [SerializeField] bool once = true;
    public UnityEvent onEnter;

    bool fired;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (fired && once) return;
        if (!other.CompareTag(playerTag)) return;

        TaskManager tm = TaskManager.Instance;
        if (!string.IsNullOrEmpty(requiredTaskId) && (tm == null || !tm.IsActive(requiredTaskId))) return;

        fired = true;
        if (!string.IsNullOrEmpty(progressTaskId) && tm != null) tm.AddProgress(progressTaskId);
        onEnter.Invoke();
    }
}
