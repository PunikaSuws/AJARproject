using UnityEngine;

public class TalkTaskTrigger : MonoBehaviour
{
    [SerializeField] NpcDialogue npc;
    [SerializeField] string taskId = "Talk_npcIng";
    [SerializeField] NpcConversationStage requiredStage = NpcConversationStage.First;

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

        Debug.Log($"TalkTaskTrigger: AddProgress {taskId}");
        TaskManager.Instance.AddProgress(taskId);
    }
}