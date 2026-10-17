using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text dialogueText;
    [SerializeField] Button nextButton;
    [SerializeField] Animator panelAnimator;

    [Header("Animation Settings")]
    [SerializeField] string animatorParameter = "IsTalk";
    [SerializeField] string closeStateName = "DialogueClose";

    NpcDialogue currentDialogue;
    List<DialogueLine> currentDialogueLines;
    NpcConversationStage currentStage;
    FirstPersonLook playerLook;
    FirstPersonMovement playerMovement;
    Transform playerRoot;
    Quaternion lockedRotation;
    int lineIndex;
    bool dialogueOpen;
    bool isClosing;
    //bool wasPlayerLookEnabled;
    bool wasPlayerMovementEnabled;
    CursorLockMode previousCursorLockState;
    bool previousCursorVisibility;

    public bool IsDialogueOpen
    {
        get { return dialogueOpen || isClosing; }
    }

    void Awake()
    {
        if (panelAnimator == null && dialoguePanel != null)
            panelAnimator = dialoguePanel.GetComponent<Animator>();

        if (dialoguePanel == null)
            Debug.LogError($"{name}: Assign the DialogueBG panel.", this);

        if (speakerText == null || dialogueText == null || nextButton == null)
            Debug.LogError($"{name}: Assign the speaker text, dialogue text, and Next button.", this);

        if (panelAnimator != null)
        {
            panelAnimator.SetBool(animatorParameter, false);

            int closeState = Animator.StringToHash(closeStateName);
            if (panelAnimator.HasState(0, closeState))
            {
                panelAnimator.Play(closeState, 0, 1f);
                panelAnimator.Update(0f);
            }
        }
        else
        {
            Debug.LogError($"{name}: Assign the DialogueBG Animator.", this);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextLine);
            nextButton.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (dialogueOpen && playerRoot != null)
            playerRoot.rotation = lockedRotation;
    }
    void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextLine);
    }

    public void StartDialogue(NpcDialogue dialogue, FirstPersonLook look)
    {
        StartDialogue(dialogue, look, NpcConversationStage.First);
    }

    public bool StartDialogue(
        NpcDialogue dialogue,
        FirstPersonLook look,
        NpcConversationStage stage)
    {
        if (dialogue == null || IsDialogueOpen)
            return false;

        List<DialogueLine> lines = dialogue.GetDialogueLines(stage);
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning($"{dialogue.name} has no {stage} dialogue lines.", dialogue);
            return false;
        }

        if (dialoguePanel == null ||
            speakerText == null ||
            dialogueText == null ||
            nextButton == null ||
            panelAnimator == null)
            return false;

        currentDialogue = dialogue;
        currentDialogueLines = lines;
        currentStage = stage;
        playerLook = look;
        playerMovement = playerLook != null
            ? playerLook.GetComponentInParent<FirstPersonMovement>()
            : null;
        lineIndex = 0;
        dialogueOpen = true;
        previousCursorLockState = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;

        if (playerLook != null)
        {
            playerLook.LookLocked = true;
        }

        if (playerMovement != null)
        {
            wasPlayerMovementEnabled = playerMovement.enabled;
            playerMovement.enabled = false;

            playerRoot = playerMovement.transform;
            lockedRotation = playerRoot.rotation;

            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        dialoguePanel.SetActive(true);
        nextButton.gameObject.SetActive(true);
        panelAnimator.SetBool(animatorParameter, true);
        ShowCurrentLine();
        return true;
    }

    void ShowNextLine()
    {
        if (!dialogueOpen || isClosing)
            return;

        if (lineIndex + 1 < currentDialogueLines.Count)
        {
            lineIndex++;
            ShowCurrentLine();
            return;
        }

        StartCoroutine(CloseDialogue());
    }

    void ShowCurrentLine()
    {
        DialogueLine line = currentDialogueLines[lineIndex];
        speakerText.text = string.IsNullOrWhiteSpace(line.speakerName)
            ? currentDialogue.name
            : line.speakerName;
        dialogueText.text = line.text;
    }

    IEnumerator CloseDialogue()
    {
        isClosing = true;
        dialogueOpen = false;
        panelAnimator.SetBool(animatorParameter, false);

        int closeState = Animator.StringToHash(closeStateName);
        float timeout = Time.unscaledTime + 3f;

        while (panelAnimator != null &&
               panelAnimator.isActiveAndEnabled &&
               panelAnimator.HasState(0, closeState) &&
               Time.unscaledTime < timeout)
        {
            AnimatorStateInfo state = panelAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(closeStateName) &&
                state.normalizedTime >= 1f &&
                !panelAnimator.IsInTransition(0))
            {
                break;
            }

            yield return null;
        }

        nextButton.gameObject.SetActive(false);
        NpcDialogue completedDialogue = currentDialogue;
        NpcConversationStage completedStage = currentStage;
        currentDialogue = null;
        currentDialogueLines = null;
        isClosing = false;

        if (playerLook != null)
        {
            playerLook.LookLocked = false;
            playerLook = null;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = wasPlayerMovementEnabled;
            playerMovement = null;
        }
        playerRoot = null;

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisibility;

        if (completedDialogue != null)
            completedDialogue.NotifyDialogueCompleted(completedStage);
    }
}
