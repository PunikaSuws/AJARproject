using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum NpcConversationStage
{
    First,
    Found,
    PlayerSeeker
}

[Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(3, 10)]
    public string text;
}

public class NpcDialogue : MonoBehaviour
{
    [SerializeField] TMP_Text interactionPrompt;
    [SerializeField] Transform interactionPoint;

    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
    public List<DialogueLine> foundDialogueLines = new List<DialogueLine>();
    public List<DialogueLine> playerSeekerDialogueLines = new List<DialogueLine>();

    public event Action<NpcDialogue, NpcConversationStage> DialogueCompleted;

    public bool IsInteractionEnabled { get; private set; } = true;

    public Transform InteractionPoint
    {
        get { return interactionPoint != null ? interactionPoint : transform; }
    }

    void Awake()
    {
        SetPromptVisible(false);
    }

    public List<DialogueLine> GetDialogueLines(NpcConversationStage stage)
    {
        switch (stage)
        {
            case NpcConversationStage.Found:
                return foundDialogueLines;
            case NpcConversationStage.PlayerSeeker:
                return playerSeekerDialogueLines;
            default:
                return dialogueLines;
        }
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        IsInteractionEnabled = isEnabled;
        if (!isEnabled)
            SetPromptVisible(false);
    }

    public void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(visible && IsInteractionEnabled);
    }

    public void NotifyDialogueCompleted(NpcConversationStage stage)
    {
        DialogueCompleted?.Invoke(this, stage);
    }
}
