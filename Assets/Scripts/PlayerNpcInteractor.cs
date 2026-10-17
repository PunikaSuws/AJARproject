using UnityEngine;

public class PlayerNpcInteractor : MonoBehaviour
{
    [SerializeField] NpcDialogue npc;
    [SerializeField] DialogueUI dialogueUI;
    [SerializeField] FirstPersonLook playerLook;
    [SerializeField, Min(0.1f)] float interactionDistance = 2.5f;
    [SerializeField] KeyCode interactionKey = KeyCode.E;

    void Awake()
    {
        if (playerLook == null)
            playerLook = GetComponentInChildren<FirstPersonLook>();

        if (playerLook == null)
            playerLook = GetComponentInParent<FirstPersonLook>();

        if (playerLook == null)
            playerLook = FindFirstObjectByType<FirstPersonLook>();

        if (playerLook == null)
            Debug.LogError($"{name}: FirstPersonLook not found!", this);

        if (npc == null)
            Debug.LogError($"{name}: Assign an NPC to PlayerNpcInteractor.", this);

        if (dialogueUI == null)
            Debug.LogError($"{name}: Assign a DialogueUI to PlayerNpcInteractor.", this);
    }

    void Update()
    {
        if (npc == null || dialogueUI == null)
        {
            return;
        }

        if (!npc.IsInteractionEnabled || dialogueUI.IsDialogueOpen)
        {
            npc.SetPromptVisible(false);
            return;
        }

        bool inRange = IsWithinInteractionDistance();
        npc.SetPromptVisible(inRange);

        if (inRange && Input.GetKeyDown(interactionKey))
        {
            npc.SetPromptVisible(false);
            dialogueUI.StartDialogue(npc, playerLook);
        }
    }

    bool IsWithinInteractionDistance()
    {
        Vector3 playerPosition = transform.position;
        Vector3 npcPosition = npc.InteractionPoint.position;
        playerPosition.y = 0f;
        npcPosition.y = 0f;

        return (playerPosition - npcPosition).sqrMagnitude <= interactionDistance * interactionDistance;
    }

    void OnDisable()
    {
        if (npc != null)
        {
            npc.SetPromptVisible(false);
        }
    }
}
