using System.Collections;
using UnityEngine;

public class TalkTaskTrigger : MonoBehaviour
{
    [SerializeField] NpcDialogue npc;
    [SerializeField] string taskId = "Talk_npcIng";
    [SerializeField] NpcConversationStage requiredStage = NpcConversationStage.First;

    [Header("Task ถัดไปที่จะเปิด (เว้นว่างถ้าไม่มี)")]
    [SerializeField] string nextTaskId = "find_hiding_spot";
    [SerializeField] float nextTaskDelay = 2.2f;  // = Hold Seconds + Fade Seconds (1.5 + 0.5) + เผื่อนิดหน่อย

    void OnEnable()
    {
        if (npc != null)
            npc.DialogueCompleted += OnDone;
    }

    void OnDisable()
    {
        if (npc != null)
            npc.DialogueCompleted -= OnDone;
    }

    void OnDone(NpcDialogue n, NpcConversationStage stage)
    {
        if (stage != requiredStage) return;

        TaskManager.Instance.AddProgress(taskId);

        if (!string.IsNullOrEmpty(nextTaskId))
            StartCoroutine(ActivateNextAfterDelay());
    }

    IEnumerator ActivateNextAfterDelay()
    {
        yield return new WaitForSeconds(nextTaskDelay);
        TaskManager.Instance.ActivateTask(nextTaskId);
    }
}