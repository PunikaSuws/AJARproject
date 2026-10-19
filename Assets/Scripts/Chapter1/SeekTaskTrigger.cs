using System.Collections;
using UnityEngine;

/// <summary>
/// ขึ้น Task "ตามหาอิง" ตอนผู้เล่นลืมตา (หลัง Seek Dialogue จบ + รอเวลานับ)
/// ขึ้นเฉยๆ ไม่ต้องหาจริง เพราะผู้เล่นต้องเดินเข้าห้องคัทซีน
/// ใส่บน Empty GameObject (เช่น CountSound) ไม่ใช่ตัวอิง เพราะอิงถูก Destroy
/// </summary>
public class SeekTaskTrigger : MonoBehaviour
{
    [SerializeField] NpcDialogue npcDialogue;

    [Header("Task")]
    [SerializeField] string taskId = "find_ing";
    [SerializeField] string taskTitle = "ตามหาอิง";
    [Tooltip("วินาทีที่รอหลัง Seek Dialogue จบ = เวลาที่ผู้เล่นหลับตานับ (ตั้งให้เท่า Count Duration ของ IngCountSound)")]
    [SerializeField, Min(0f)] float delay = 10f;

    Coroutine routine;

    void Awake()
    {
        if (npcDialogue == null) npcDialogue = GetComponent<NpcDialogue>();
    }

    void OnEnable()
    {
        if (npcDialogue != null) npcDialogue.DialogueCompleted += OnDialogueCompleted;
    }

    void OnDisable()
    {
        if (npcDialogue != null) npcDialogue.DialogueCompleted -= OnDialogueCompleted;
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    void OnDialogueCompleted(NpcDialogue dialogue, NpcConversationStage stage)
    {
        if (dialogue != npcDialogue || stage != NpcConversationStage.PlayerSeeker) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowTask());
    }

    IEnumerator ShowTask()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        routine = null;

        TaskManager tm = TaskManager.Instance;
        if (tm == null) yield break;

        // มี Task นี้ในลิสต์อยู่แล้ว (Start Active ปิดไว้) -> เปิดให้ขึ้น / ไม่มี -> สร้างใหม่
        bool exists = false;
        foreach (TaskData t in tm.Tasks)
            if (t.id == taskId) { exists = true; break; }

        if (exists) tm.ActivateTask(taskId);
        else tm.AddTask(new TaskData { id = taskId, title = taskTitle, targetAmount = 1 });
    }
}
