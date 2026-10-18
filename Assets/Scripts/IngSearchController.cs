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
    [SerializeField] private Animator eyeAnimator;

    [Header("Search Settings")]
    [SerializeField, Min(0f)] private float countDuration = 10f;
    [SerializeField, Min(0.1f)] private float foundDistance = 1.5f;
    [SerializeField, Min(0.05f)] private float destinationRefreshInterval = 0.2f;
    [SerializeField, Range(0f, 180f)] private float playerFacingAngle = 50f;
    [SerializeField, Min(0f)] private float playerSeekerDialogueDelay = 3f;
    [SerializeField, Min(0.1f)] private float playerTrackingTurnSpeed = 540f;
    [SerializeField, Min(0.1f)] private float departureDistance = 15f;

    private static readonly int Dialogue1Finished = Animator.StringToHash("IsDialogue1Finished");
    private static readonly int Dialogue2Finished = Animator.StringToHash("IsDialogue2Finished");
    private static readonly int SeekDialogueFinished = Animator.StringToHash("SeekDialogueFinished");
    private static readonly int Seek = Animator.StringToHash("IsSeek");
    private static readonly int Find = Animator.StringToHash("IsFind");
    private static readonly int CountState = Animator.StringToHash("Base Layer.Count");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
    private static readonly int EyeAnimationShortName = Animator.StringToHash("JanEye");
    private static readonly int IdleShortName = Animator.StringToHash("Idle");

    private Coroutine sequence;
    private HideController hideController;
    private bool hasStartedSearching;
    private bool hasFoundPlayer;
    private bool hasCompletedFoundDialogue;
    private bool hasStartedPlayerSeekerDialogue;
    private bool isLeaving;

    public bool IsSearching { get; internal set; }
    public bool HasFoundPlayer => hasFoundPlayer;
    

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

        if (eyeAnimator != null)
            eyeAnimator.gameObject.SetActive(false);

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
        if (!hasFoundPlayer || isLeaving || player == null)
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

        if (stage == NpcConversationStage.PlayerSeeker)
        {
            if (!hasFoundPlayer || !hasStartedPlayerSeekerDialogue)
            {
                Debug.LogError($"{name}: Seek dialogue completed before Ing found the player.", this);
                return;
            }

            sequence = StartCoroutine(PlayEyeAndLeave());
            return;
        }

        if (stage == NpcConversationStage.Found)
        {
            hasCompletedFoundDialogue = true;
            StartPlayerSeekerDialogue();
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

        if (eyeAnimator == null)
        {
            Debug.LogError($"{name}: Assign the Eye Animator for the Seek dialogue's eye animation.", this);
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

        StartPlayerSeekerDialogue();
    }

    private void StartPlayerSeekerDialogue()
    {
        if (!hasFoundPlayer || hasStartedPlayerSeekerDialogue)
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

    private IEnumerator PlayEyeAndLeave()
    {
        if (eyeAnimator == null)
        {
            Debug.LogError($"{name}: Eye Animator is missing; cannot play the Seek dialogue eye animation.", this);
        }
        else if (eyeAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError($"{name}: Eye Animator has no Runtime Animator Controller.", eyeAnimator);
        }
        else
        {
            eyeAnimator.gameObject.SetActive(true);
            SetAnimatorSignal(eyeAnimator, SeekDialogueFinished, true);
            eyeAnimator.Play("JanEye", 0, 0f);
            yield return null;

            if (eyeAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != EyeAnimationShortName)
            {
                Debug.LogError($"{name}: Eye Animator did not enter the JanEye state.", eyeAnimator);
                eyeAnimator.gameObject.SetActive(false);
            }
        }

        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogError($"{name}: Cannot make Ing leave because the player or NavMeshAgent is unavailable.", this);
            sequence = null;
            yield break;
        }

        FirstPersonLook playerLook = player.GetComponentInChildren<FirstPersonLook>();
        Vector3 sideDirection = playerLook != null
            ? Vector3.ProjectOnPlane(playerLook.transform.right, Vector3.up).normalized
            : Vector3.zero;
        Vector3 directionToPlayer = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
        if (sideDirection.sqrMagnitude < 0.0001f && directionToPlayer.sqrMagnitude > 0.0001f)
            sideDirection = Vector3.Cross(Vector3.up, directionToPlayer.normalized);

        if (sideDirection.sqrMagnitude < 0.0001f ||
            !TryGetDepartureDestination(sideDirection, out Vector3 destination))
        {
            Debug.LogError($"{name}: Could not find a NavMesh path for Ing to leave the player's view.", this);
            sequence = null;
            yield break;
        }

        isLeaving = true;
        agent.updateRotation = true;
        agent.isStopped = false;
        SetAnimatorSignal(Seek, true);
        SetAnimatorSignal(Dialogue2Finished, true);
        animator.Play(WalkState, 0, 0f);

        if (!agent.SetDestination(destination))
        {
            Debug.LogError($"{name}: Ing's NavMeshAgent could not set a path out of the player's view.", this);
            sequence = null;
            yield break;
        }

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError($"{name}: Ing's departure path is incomplete.", this);
            sequence = null;
            yield break;
        }

        while (agent.hasPath &&
               agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.1f))
            yield return null;

        agent.isStopped = true;
        agent.ResetPath();
        SetAnimatorSignal(Seek, false);
        animator.Play(IdleState, 0, 0f);
        sequence = null;
        Destroy(gameObject);
    }

    private bool TryGetDepartureDestination(Vector3 sideDirection, out Vector3 destination)
    {
        destination = default;
        NavMeshPath path = new NavMeshPath();
        float sampleRadius = Mathf.Max(2f, departureDistance * 0.5f);

        for (int side = 0; side < 2; side++)
        {
            Vector3 direction = side == 0 ? sideDirection : -sideDirection;
            Vector3 candidate = transform.position + direction * departureDistance;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, agent.areaMask))
                continue;

            if (agent.CalculatePath(hit.position, path) &&
                path.status == NavMeshPathStatus.PathComplete)
            {
                destination = hit.position;
                return true;
            }
        }

        return false;
    }

    private bool HasReachedPlayer()
    {
        Vector3 p = player.position, i = transform.position;
        p.y = 0f; i.y = 0f;
        return (p - i).sqrMagnitude <= foundDistance * foundDistance;
    }

    private void SetAnimatorSignal(int parameterHash, bool value)
    {
        SetAnimatorSignal(animator, parameterHash, value);
    }

    private static void SetAnimatorSignal(Animator targetAnimator, int parameterHash, bool value)
    {
        if (targetAnimator == null)
            return;

        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.nameHash != parameterHash)
                continue;

            if (parameter.type == AnimatorControllerParameterType.Bool)
                targetAnimator.SetBool(parameterHash, value);
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                if (value)
                    targetAnimator.SetTrigger(parameterHash);
                else
                    targetAnimator.ResetTrigger(parameterHash);
            }

            return;
        }
    }
}
