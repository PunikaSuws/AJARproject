using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ผีตัวเดียวตลอด Chapter 2 (ต้องมี NavMeshAgent + Collider; Animator ไม่บังคับ)
/// Wander   : เดินเตร่ในบ้าน ถ้าผู้เล่นจ้องเกิน gazeSeconds -> หันมาแล้วพุ่งใส่ -> จั๊มป์สแคร์
/// Appear   : โผล่ตรงหน้าผู้เล่น ยืนจ้อง (หัวหันตามผู้เล่น) พอผู้เล่นเดินถอย -> Chase
/// Chase    : ไล่ล่า ถูกขว้างโดน = Stun แล้วไล่ต่อ จับได้ = Game Over
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GhostController : MonoBehaviour
{
    public enum Mode { Inactive, Wander, Appear, Chase, Stunned, Lunging }

    [Header("อ้างอิง (ว่างไว้ = หาเอง)")]
    [SerializeField] Transform eyePoint;            // จุดที่ใช้เช็คว่าถูกจ้อง (หัวผี)
    [SerializeField] Animator animator;

    [Header("Wander")]
    [SerializeField] float wanderRadius = 12f;
    [SerializeField] float wanderSpeed = 1.3f;
    [SerializeField] Vector2 wanderPause = new Vector2(1.5f, 4f);

    [Header("Gaze (จ้องผี)")]
    [SerializeField] float gazeSeconds = 3f;
    [SerializeField] float gazeAngle = 30f;          // ครึ่งมุมของกรวยสายตา
    [SerializeField] float gazeMaxDistance = 30f;
    [SerializeField] float gazeDecay = 1.5f;         // ไม่จ้อง = ตัวนับลดเร็วเท่าไร
    [SerializeField] LayerMask gazeBlockMask = ~0;
    [SerializeField] AudioSource gazeWarningLoop;    // เสียงหัวใจ/ความตึงเครียด ค่อย ๆ ดังขึ้นตอนจ้อง (ไม่ใส่ก็ได้)

    [Header("Lunge (พุ่งใส่ตอนถูกจ้อง)")]
    [SerializeField] float lungeTurnSeconds = 0.6f;
    [SerializeField] float lungeSpeed = 10f;
    [SerializeField] float catchDistance = 1.3f;

    [Header("Appear (โผล่ตรงหน้า)")]
    [SerializeField] float appearDistance = 5f;
    [SerializeField] float appearHeadTurnSpeed = 6f;
    [SerializeField] float walkAwayDistance = 1.0f;  // ผู้เล่นเดินห่างเพิ่มจากตอนโผล่เท่านี้ = เริ่มไล่
    [SerializeField] float appearMinHold = 1.2f;
    [SerializeField] float appearMaxHold = 20f;      // กันค้าง: ถ้าผู้เล่นยืนนิ่งนานเกินนี้ก็ไล่เลย

    [Header("Chase")]
    [SerializeField] float chaseSpeed = 4.2f;
    [SerializeField] float stunSeconds = 2f;

    [Header("Animator parameters (ชื่อ bool; ไม่มีใน Animator ก็ข้ามเอง)")]
    [SerializeField] string walkParam = "Walk";
    [SerializeField] string runParam = "Run";
    [SerializeField] string stunParam = "Stunned";

    [Header("Sounds (ไม่ใส่ก็ได้)")]
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip appearSound;
    [SerializeField] AudioClip chaseSound;
    [SerializeField] AudioClip lungeSound;

    public Mode CurrentMode { get; private set; } = Mode.Inactive;

    /// <summary>ยิงตอนผีเริ่มไล่ (ใช้เปิด Task หนี/ขว้าง)</summary>
    public event Action ChaseStarted;

    NavMeshAgent agent;
    Transform player;
    readonly HashSet<string> animParams = new HashSet<string>();

    Vector3 home;
    float gazeTimer;
    float wanderWait;
    float appearTimer;
    float distanceAtAppear;
    float chaseRepathTimer;
    bool lookAtPlayerIK;
    Coroutine stunRoutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
            foreach (AnimatorControllerParameter p in animator.parameters) animParams.Add(p.name);

        agent.updateRotation = true;
        home = transform.position;
    }

    Transform Player
    {
        get
        {
            if (player == null) player = PlayerControl.PlayerTransform;
            return player;
        }
    }

    Vector3 EyePos
    {
        get { return eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f; }
    }

    // ---------- Public API ----------

    /// <summary>เปิดผีและเริ่มเดินเตร่ที่จุด position</summary>
    public void StartWander(Vector3 position)
    {
        gameObject.SetActive(true);
        if (!Place(position, 4f)) Debug.LogWarning("GhostController: จุดเกิดอยู่นอก NavMesh", this);

        home = transform.position;
        gazeTimer = 0f;
        wanderWait = 0f;
        SetMode(Mode.Wander);
        agent.speed = wanderSpeed;
        PickWanderTarget();
    }

    /// <summary>โผล่ตรงหน้าผู้เล่นในทิศที่กล้องมอง</summary>
    public void AppearInFrontOfPlayer()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        stunRoutine = null;
        SetAnim(stunParam, false);

        Transform pl = Player;
        Transform cam = PlayerControl.CameraTransform != null ? PlayerControl.CameraTransform : Camera.main.transform;

        Vector3 forward = cam.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = pl.forward;
        forward.Normalize();

        bool placed = false;
        for (float d = appearDistance; d >= 2f && !placed; d -= 0.75f)
            placed = Place(pl.position + forward * d, 1.5f);
        if (!placed) placed = Place(pl.position + forward * appearDistance, 6f);
        if (!placed) Debug.LogWarning("GhostController: หาจุดโผล่บน NavMesh ไม่ได้", this);

        // หันหน้าหาผู้เล่นทันที
        Vector3 look = pl.position - transform.position; look.y = 0f;
        if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);

        agent.isStopped = true;
        agent.ResetPath();
        distanceAtAppear = FlatDistance(pl.position, transform.position);
        appearTimer = 0f;
        gazeTimer = 0f;
        if (gazeWarningLoop != null) gazeWarningLoop.Stop();

        SetMode(Mode.Appear);
        lookAtPlayerIK = true;
        if (sfx != null && appearSound != null) sfx.PlayOneShot(appearSound);
    }

    public void StartChase()
    {
        if (CurrentMode == Mode.Chase || CurrentMode == Mode.Lunging) return;
        lookAtPlayerIK = false;
        SetMode(Mode.Chase);
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        chaseRepathTimer = 0f;
        if (sfx != null && chaseSound != null) sfx.PlayOneShot(chaseSound);
        ChaseStarted?.Invoke();
    }

    /// <summary>ถูกขว้างโดน: หยุด seconds วินาทีแล้วไล่ต่อ คืน true ถ้ามีผล</summary>
    public bool Stun(float seconds = -1f)
    {
        if (CurrentMode != Mode.Chase && CurrentMode != Mode.Stunned) return false;
        if (seconds < 0f) seconds = stunSeconds;

        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine(seconds));
        return true;
    }

    /// <summary>ผีหายไป (ผู้เล่นเข้าห้องพระ)</summary>
    public void Banish()
    {
        StopAllCoroutines();
        stunRoutine = null;
        if (gazeWarningLoop != null) gazeWarningLoop.Stop();
        lookAtPlayerIK = false;
        CurrentMode = Mode.Inactive;
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        gameObject.SetActive(false);
    }

    /// <summary>หยุดทุกอย่าง (ตอน Game Over)</summary>
    public void Freeze()
    {
        StopAllCoroutines();
        stunRoutine = null;
        if (gazeWarningLoop != null) gazeWarningLoop.Stop();
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        SetAnim(walkParam, false);
        SetAnim(runParam, false);
        CurrentMode = Mode.Inactive;
    }

    // ---------- Update ----------

    void Update()
    {
        if (GameState.GameOver || Player == null) return;

        switch (CurrentMode)
        {
            case Mode.Wander: UpdateWander(); break;
            case Mode.Appear: UpdateAppear(); break;
            case Mode.Chase: UpdateChase(); break;
        }
    }

    void UpdateWander()
    {
        // เดินเตร่
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            SetAnim(walkParam, false);
            wanderWait -= Time.deltaTime;
            if (wanderWait <= 0f) PickWanderTarget();
        }
        else
        {
            SetAnim(walkParam, true);
        }

        // เช็คสายตา
        if (IsBeingStaredAt())
        {
            gazeTimer += Time.deltaTime;
            if (gazeTimer >= gazeSeconds)
            {
                StartCoroutine(LungeRoutine());
                return;
            }
        }
        else
        {
            gazeTimer = Mathf.Max(0f, gazeTimer - gazeDecay * Time.deltaTime);
        }

        UpdateGazeAudio();
    }

    void UpdateGazeAudio()
    {
        if (gazeWarningLoop == null) return;
        float k = Mathf.Clamp01(gazeTimer / gazeSeconds);
        if (k > 0.05f)
        {
            if (!gazeWarningLoop.isPlaying) gazeWarningLoop.Play();
            gazeWarningLoop.volume = k;
        }
        else if (gazeWarningLoop.isPlaying)
        {
            gazeWarningLoop.Stop();
        }
    }

    void PickWanderTarget()
    {
        wanderWait = UnityEngine.Random.Range(wanderPause.x, wanderPause.y);
        for (int i = 0; i < 12; i++)
        {
            Vector3 random = home + UnityEngine.Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    bool IsBeingStaredAt()
    {
        Transform cam = PlayerControl.CameraTransform;
        if (cam == null) return false;

        Vector3 eye = EyePos;
        Vector3 toGhost = eye - cam.position;
        float dist = toGhost.magnitude;
        if (dist > gazeMaxDistance || dist < 0.01f) return false;

        if (Vector3.Angle(cam.forward, toGhost) > gazeAngle) return false;

        // มีสิ่งกีดขวาง (ผนัง ประตู) บังอยู่ไหม
        if (Physics.Linecast(cam.position, eye, out RaycastHit hit, gazeBlockMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(Player))
                return false;
        }
        return true;
    }

    IEnumerator LungeRoutine()
    {
        SetMode(Mode.Lunging);
        if (gazeWarningLoop != null) gazeWarningLoop.Stop();

        agent.isStopped = true;
        agent.ResetPath();
        SetAnim(walkParam, false);
        lookAtPlayerIK = true;
        PlayerControl.Lock(false);      // ผู้เล่นขยับไม่ได้ตอนผีพุ่ง

        // หันมาจ้องผู้เล่น
        Quaternion start = transform.rotation;
        for (float t = 0f; t < lungeTurnSeconds; t += Time.deltaTime)
        {
            Vector3 look = Player.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(start, Quaternion.LookRotation(look), t / lungeTurnSeconds);
            yield return null;
        }

        if (sfx != null && lungeSound != null) sfx.PlayOneShot(lungeSound);

        // พุ่งใส่
        agent.isStopped = false;
        agent.speed = lungeSpeed;
        agent.acceleration = 60f;
        SetAnim(runParam, true);

        float timeout = 6f;
        while (timeout > 0f)
        {
            agent.SetDestination(Player.position);
            if (FlatDistance(Player.position, transform.position) <= catchDistance) break;
            timeout -= Time.deltaTime;
            yield return null;
        }

        Caught();
    }

    void UpdateAppear()
    {
        appearTimer += Time.deltaTime;

        // หัน/ตัวหันตามผู้เล่น
        Vector3 look = Player.position - transform.position; look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(look);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, appearHeadTurnSpeed * Time.deltaTime);
        }

        if (appearTimer < appearMinHold) return;

        float now = FlatDistance(Player.position, transform.position);
        bool walkedAway = now >= distanceAtAppear + walkAwayDistance;
        if (walkedAway || appearTimer >= appearMaxHold)
            StartChase();
    }

    void UpdateChase()
    {
        SetAnim(runParam, true);

        chaseRepathTimer -= Time.deltaTime;
        if (chaseRepathTimer <= 0f)
        {
            chaseRepathTimer = 0.2f;
            agent.SetDestination(Player.position);
        }

        if (FlatDistance(Player.position, transform.position) <= catchDistance)
            Caught();
    }

    IEnumerator StunRoutine(float seconds)
    {
        SetMode(Mode.Stunned);
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        SetAnim(runParam, false);
        SetAnim(stunParam, true);

        yield return new WaitForSeconds(seconds);

        SetAnim(stunParam, false);
        stunRoutine = null;
        SetMode(Mode.Chase);
        agent.isStopped = false;
        agent.speed = chaseSpeed;
    }

    void Caught()
    {
        if (GameState.GameOver) return;
        Freeze();
        GameOverManager.Trigger();
    }

    // ---------- Helpers ----------

    bool Place(Vector3 position, float searchRadius)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            if (!agent.enabled) agent.enabled = true;
            agent.Warp(hit.position);
            return true;
        }
        return false;
    }

    void SetMode(Mode m)
    {
        CurrentMode = m;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void SetAnim(string param, bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(param) && animParams.Contains(param))
            animator.SetBool(param, value);
    }

    // เปิด "IK Pass" ที่ Layer ใน Animator Controller ถ้าอยากให้หัวหันตามด้วย IK (ไม่เปิดก็ไม่เป็นไร)
    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || Player == null) return;
        animator.SetLookAtWeight(lookAtPlayerIK ? 1f : 0f, 0.3f, 1f, 1f, 0.6f);
        if (lookAtPlayerIK)
        {
            Transform cam = PlayerControl.CameraTransform;
            animator.SetLookAtPosition(cam != null ? cam.position : Player.position + Vector3.up * 1.6f);
        }
    }
}
