using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class IngSearchController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController ingAnimatorController;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private NpcDialogue npcDialogue;

    [Header("Search Settings")]
    [SerializeField, Min(0f)] private float countDuration = 10f;
    [SerializeField, Min(0.1f)] private float foundDistance = 1.5f;
    [SerializeField, Min(0.05f)] private float destinationRefreshInterval = 0.2f;
    [SerializeField, Range(0f, 180f)] private float playerFacingAngle = 50f;
    [SerializeField, Min(0f)] private float playerSeekerDialogueDelay = 3f;
    [SerializeField, Min(0.1f)] private float playerTrackingTurnSpeed = 540f;

    private static readonly int Dialogue1Finished = Animator.StringToHash("IsDialogue1Finished");
    private static readonly int Seek = Animator.StringToHash("IsSeek");
    private static readonly int Find = Animator.StringToHash("IsFind");
    private static readonly int CountState = Animator.StringToHash("Base Layer.Count");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
    private static readonly int IdleShortName = Animator.StringToHash("Idle");

    private Coroutine sequence;
    private HideController hideController;
    private bool hasStartedSearching;
    private bool hasFoundPlayer;
    private bool hasCompletedFoundDialogue;
    private bool hasStartedPlayerSeekerDialogue;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            animator = gameObject.AddComponent<Animator>();

        if (animator != null && ingAnimatorController != null)
            animator.runtimeAnimatorController = ingAnimatorController;

        if (npcDialogue == null)
            npcDialogue = GetComponent<NpcDialogue>();

        if (dialogueUI == null)
            dialogueUI = FindAnyObjectByType<DialogueUI>();

        if (player == null)
        {
            PlayerNpcInteractor interactor = FindAnyObjectByType<PlayerNpcInteractor>();
            if (interactor != null)
                player = interactor.transform;
        }

        if (player != null)
            hideController = player.GetComponentInParent<HideController>();
    }

    private void OnEnable()
    {
        if (npcDialogue != null)
            npcDialogue.DialogueCompleted += OnDialogueCompleted;

        if (hideController != null)
            hideController.ExitedHide += OnPlayerExitedHide;
    }

    private void OnDisable()
    {
        if (npcDialogue != null)
            npcDialogue.DialogueCompleted -= OnDialogueCompleted;

        if (hideController != null)
            hideController.ExitedHide -= OnPlayerExitedHide;

        if (sequence != null)
        {
            StopCoroutine(sequence);
            sequence = null;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void LateUpdate()
    {
        if (!hasFoundPlayer || player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            playerTrackingTurnSpeed * Time.deltaTime);
    }

    private void OnDialogueCompleted(NpcDialogue dialogue, NpcConversationStage stage)
    {
        if (dialogue != npcDialogue)
            return;

        if (stage == NpcConversationStage.Found)
        {
            hasCompletedFoundDialogue = true;
            return;
        }

        if (stage != NpcConversationStage.First || hasStartedSearching)
            return;

        if (!HasRequiredReferences())
            return;

        hasStartedSearching = true;
        npcDialogue.SetInteractionEnabled(false);
        SetAnimatorSignal(Seek, false);
        SetAnimatorSignal(Find, false);
        SetAnimatorSignal(Dialogue1Finished, false);
        animator.Play(CountState, 0, 0f);
        sequence = StartCoroutine(CountThenSearch());
    }

    private bool HasRequiredReferences()
    {
        bool valid = true;

        if (player == null)
        {
            Debug.LogError($"{name}: Assign the player Transform to IngSearchController.", this);
            valid = false;
        }

        if (animator == null)
        {
            Debug.LogError($"{name}: Assign Ing's Animator to IngSearchController.", this);
            valid = false;
        }

        if (dialogueUI == null)
        {
            Debug.LogError($"{name}: Assign DialogueCanvas's DialogueUI to IngSearchController.", this);
            valid = false;
        }

        if (npcDialogue == null)
        {
            Debug.LogError($"{name}: Assign Ing's NpcDialogue to IngSearchController.", this);
            valid = false;
        }

        if (agent == null)
        {
            Debug.LogError($"{name}: NavMeshAgent is missing.", this);
            valid = false;
        }

        if (npcDialogue != null &&
            (npcDialogue.foundDialogueLines == null || npcDialogue.foundDialogueLines.Count == 0))
        {
            Debug.LogError($"{name}: Add at least one line to NpcDialogue's Found Dialogue Lines.", npcDialogue);
            valid = false;
        }

        if (npcDialogue != null &&
            (npcDialogue.playerSeekerDialogueLines == null || npcDialogue.playerSeekerDialogueLines.Count == 0))
        {
            Debug.LogError($"{name}: Add at least one line to NpcDialogue's Player Seeker Dialogue Lines.", npcDialogue);
            valid = false;
        }

        if (hideController == null)
        {
            Debug.LogError($"{name}: Assign a player with a HideController to IngSearchController.", this);
            valid = false;
        }

        if (animator != null &&
            (!animator.HasState(0, IdleState) ||
             !animator.HasState(0, CountState) ||
             !animator.HasState(0, WalkState)))
        {
            Debug.LogError(
                $"{name}: Ing's Animator Controller must have Idle, Count, and Walk states in Base Layer.",
                animator);
            valid = false;
        }

        return valid;
    }

    private IEnumerator CountThenSearch()
    {
        yield return new WaitForSeconds(countDuration);
        Debug.Log("[Ing] count finished, start searching");

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogError($"{name}: Ing's NavMeshAgent is not on a baked NavMesh. Bake a NavMesh for the scene.", this);
            sequence = null;
            yield break;
        }

        agent.stoppingDistance = foundDistance * 0.8f;   // 1.2 ม. หยุดก่อนชน
        if (!agent.SetDestination(player.position))
        {
            Debug.LogError($"{name}: Ing's NavMeshAgent could not set a path to the player.", this);
            sequence = null;
            yield break;
        }

        agent.isStopped = false;
        SetAnimatorSignal(Seek, true);
        animator.Play(WalkState, 0, 0f);
        sequence = StartCoroutine(SearchForPlayer());
    }

    private IEnumerator SearchForPlayer()
    {
        while (!hasFoundPlayer)
        {
            if (player == null || !agent.isOnNavMesh)
            {
                Debug.LogError($"{name}: Player reference or NavMeshAgent navigation was lost during search.", this);
                sequence = null;
                yield break;
            }

            if (HasReachedPlayer())
            {
                hasFoundPlayer = true;
                if (hideController != null)
                    hideController.SetHidingEnabled(false);

                agent.isStopped = true;
                agent.ResetPath();
                agent.updateRotation = false;
                SetAnimatorSignal(Seek, false);
                SetAnimatorSignal(Find, false);
                animator.Play(IdleState, 0, 0f);

                if (TaskManager.Instance != null)
                    TaskManager.Instance.AddProgress("find_hiding_spot");

                sequence = StartCoroutine(WaitForIdleAndTalk());
                yield break;
            }

            if (!agent.SetDestination(player.position))
            {
                Debug.LogError($"{name}: Ing's NavMeshAgent could not set a path to the player.", this);
                sequence = null;
                yield break;
            }

            yield return new WaitForSeconds(destinationRefreshInterval);
        }
    }

    private IEnumerator WaitForIdleAndTalk()
    {
        yield return null;

        if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != IdleShortName)
            animator.Play(IdleState, 0, 0f);

        SetAnimatorSignal(Find, false);

        FirstPersonLook playerLook = player != null
            ? player.GetComponentInChildren<FirstPersonLook>()
            : null;

        if (!dialogueUI.StartDialogue(npcDialogue, playerLook, NpcConversationStage.Found))
            Debug.LogError($"{name}: Could not start Ing's found-player dialogue.", this);

        sequence = null;
    }

    private void OnPlayerExitedHide(HideController controller)
    {
        if (controller != hideController || !hasFoundPlayer || hasStartedPlayerSeekerDialogue)
            return;

        hasStartedPlayerSeekerDialogue = true;
        sequence = StartCoroutine(TurnAndStartPlayerSeekerDialogue());
    }

    private IEnumerator TurnAndStartPlayerSeekerDialogue()
    {
        while (!hasCompletedFoundDialogue || (dialogueUI != null && dialogueUI.IsDialogueOpen))
            yield return null;

        if (player == null || dialogueUI == null)
        {
            Debug.LogError($"{name}: Player or DialogueUI was lost before starting the player-seeker dialogue.", this);
            sequence = null;
            yield break;
        }

        yield return new WaitForSeconds(playerSeekerDialogueDelay);

        FirstPersonLook playerLook = player.GetComponentInChildren<FirstPersonLook>();
        if (playerLook == null)
        {
            Debug.LogError($"{name}: Could not find the player's FirstPersonLook component.", this);
            sequence = null;
            yield break;
        }

        while (!IsPlayerFacingIng(playerLook))
            yield return null;

        if (!dialogueUI.StartDialogue(npcDialogue, playerLook, NpcConversationStage.PlayerSeeker))
            Debug.LogError($"{name}: Could not start Ing's player-seeker dialogue.", this);

        sequence = null;
    }

    private bool IsPlayerFacingIng(FirstPersonLook playerLook)
    {
        Vector3 playerForward = Vector3.ProjectOnPlane(playerLook.transform.forward, Vector3.up);
        Vector3 directionToIng = Vector3.ProjectOnPlane(transform.position - playerLook.transform.position, Vector3.up);
        if (playerForward.sqrMagnitude < 0.0001f || directionToIng.sqrMagnitude < 0.0001f)
            return false;

        return Vector3.Angle(playerForward, directionToIng) <= playerFacingAngle;
    }

    private bool HasReachedPlayer()
    {
        Vector3 p = player.position, i = transform.position;
        p.y = 0f; i.y = 0f;
        return (p - i).sqrMagnitude <= foundDistance * foundDistance;
    }

    private void SetAnimatorSignal(int parameterHash, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash != parameterHash)
                continue;

            if (parameter.type == AnimatorControllerParameterType.Bool)
                animator.SetBool(parameterHash, value);
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                if (value)
                    animator.SetTrigger(parameterHash);
                else
                    animator.ResetTrigger(parameterHash);
            }

            return;
        }
    }
    private float nextLog;

    private void Update()
    {
        if (!hasStartedSearching || agent == null || player == null || Time.time < nextLog) return;
        nextLog = Time.time + 0.5f;

        var st = animator.GetCurrentAnimatorStateInfo(0);
        string stateName = st.IsName("Walk") ? "Walk" : st.IsName("Count") ? "Count" : "Idle/other";

        Debug.Log($"[Ing] onNavMesh={agent.isOnNavMesh} stopped={agent.isStopped} speed={agent.speed} " +
                  $"vel={agent.velocity.magnitude:F2} hasPath={agent.hasPath} pending={agent.pathPending} " +
                  $"status={agent.pathStatus} remain={agent.remainingDistance:F2} " +
                  $"distToPlayer={Vector3.Distance(transform.position, player.position):F2} " +
                  $"state={stateName} IsSeek={animator.GetBool(Seek)}");
    }
}
