using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// ตัวควบคุมเนื้อเรื่อง Chapter 2 ทั้งหมด (วางไว้ที่ GameObject "Chapter2Director" ตัวเดียวใน Scene)
/// ลำดับ: คัตซีนเปิด -> collect_items -> (30 วิ ผีเดินเตร่) -> get_book -> get_pot -> ผีโผล่ตรงหน้า ->
///        ไล่ (escape + throw_ghost พร้อมกัน) -> ห้องพระ -> turn_buddha -> place_items -> light_incense -> วิดีโอจบ -> EndScene
/// </summary>
public class Chapter2Director : MonoBehaviour
{
    // ===== Task IDs (ต้องตรงกับที่ตั้งใน TaskManager) =====
    public const string CollectItems = "collect_items";
    public const string GetBook = "get_book";
    public const string GetPot = "get_pot";
    public const string Escape = "escape";
    public const string ThrowGhost = "throw_ghost";
    public const string TurnBuddha = "turn_buddha";
    public const string PlaceItems = "place_items";
    public const string LightIncense = "light_incense";

    [Header("คัตซีน")]
    [SerializeField] VideoCutscene cutscene;
    [SerializeField] VideoClip openingClip;
    [SerializeField] VideoClip endingClip;
    [Tooltip("หน้าจบเกม (Overlay) ถ้าเว้นว่างจะโหลด End Scene Name แทน")]
    [SerializeField] EndScreenUI endScreen;
    [SerializeField] string endSceneName = "EndScene";
    [Tooltip("หลังจุดธูปเสร็จ รอกี่วินาทีก่อนขึ้นวิดีโอฉากจบ (ให้ผู้เล่นดูควันไฟธูป)")]
    [SerializeField] float endingDelaySeconds = 5f;
    [Tooltip("ติ๊ก = ตอนขึ้นหน้าจบเกม ค้างเฟรมสุดท้ายของวิดีโอไว้เป็นพื้นหลัง (ไม่ติ๊ก = จอดำ)")]
    [SerializeField] bool holdEndingLastFrame = true;

    [Header("เสียงระทึก (ไม่ใส่ก็ได้)")]
    [Tooltip("เริ่มตอนผีโผล่หลังเก็บหม้อดิน ปิดตอนเริ่มฉากจบ")]
    [SerializeField] TensionMusic tension;

    [Header("ผี")]
    [SerializeField] GhostController ghost;
    [SerializeField] Transform ghostSpawnPoint;
    [SerializeField] float ghostDelaySeconds = 30f;
    [TextArea] [SerializeField] string ghostWarningText = "อย่าจ้องมองผีเกิน 3 วินาที";
    [SerializeField] float warningSeconds = 5f;
    [SerializeField] float gazeWarningSeconds = 2.5f;   // ข้อความเตือนทุกครั้งที่เริ่มจ้องผี

    [Header("ผีเกิดใหม่หลังกด ลองอีกครั้ง ช่วงก่อนหม้อดิน (สุ่มห่างจากผู้เล่น)")]
    [SerializeField] float reviveSpawnMin = 14f;
    [SerializeField] float reviveSpawnMax = 24f;

    [Header("วัตถุในฉาก")]
    [Tooltip("สมุด (ปิด Active ไว้ได้ จะเปิดเมื่อเก็บครบ 4 ชิ้น) เว้นว่างถ้าวางไว้เปิดตลอด")]
    [SerializeField] GameObject bookObject;
    [Tooltip("หม้อดิน ต้องปิด Active ไว้ จะเปิดหลังเก็บสมุด")]
    [SerializeField] GameObject potObject;
    [Tooltip("กลุ่มของที่ไว้ขว้าง ปิด Active ไว้ จะเปิดตอนผีเริ่มไล่")]
    [SerializeField] GameObject throwablesRoot;
    [SerializeField] ThrowController throwController;

    [Header("ไอเท็มหม้อดิน (ใช้ตรวจว่าปิดหน้าต่างรายละเอียดของหม้อ)")]
    [SerializeField] ItemData potItem;

    [Header("Task ตอนเริ่มบท (ข้อความมาจาก TaskManager)")]
    [SerializeField] AudioSource ambience;
    [SerializeField] float ambienceFadeOutSeconds = 1.5f;   // เสียงพื้นหลังเฟดออกตอนเริ่มฉากจบ

    TaskManager tasks;
    bool potGhostTriggered;
    bool endingStarted;
    Vector3 potSpawnPosition;
    Coroutine ghostTimer;

    void Awake()
    {
        GameState.Reset();
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (potObject != null) potObject.SetActive(false);
        if (throwablesRoot != null) throwablesRoot.SetActive(false);
        if (bookObject != null) bookObject.SetActive(false);
        if (throwController != null) throwController.enabled = false;
        if (ghost != null)
        {
            ghost.SilenceGazeAudio();
            ghost.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        ItemDetailView.Closed += OnItemDetailClosed;
        GameOverManager.Revived += OnRevived;
    }

    void OnDisable()
    {
        ItemDetailView.Closed -= OnItemDetailClosed;
        GameOverManager.Revived -= OnRevived;
        if (tasks != null) tasks.TaskCompleted -= OnTaskCompleted;
        if (ghost != null)
        {
            ghost.ChaseStarted -= OnGhostChaseStarted;
            ghost.GazeStarted -= OnGhostGazeStarted;
        }
    }

    void Start()
    {
        tasks = TaskManager.Instance;
        if (tasks == null) { Debug.LogError("Chapter2Director: ไม่พบ TaskManager ใน Scene"); return; }

        tasks.TaskCompleted += OnTaskCompleted;
        if (ghost != null)
        {
            ghost.ChaseStarted += OnGhostChaseStarted;
            ghost.GazeStarted += OnGhostGazeStarted;
        }

        // เสียงพื้นหลังเริ่มพร้อมคัทซีนเปิดเรื่อง (เล่นต่อเนื่องเข้าสู่เกม ปิดตอนฉากจบ)
        if (ambience != null && !ambience.isPlaying) ambience.Play();

        if (cutscene != null && openingClip != null)
            cutscene.Play(openingClip, BeginGameplay, true, true, false);
        else
            BeginGameplay();
    }

    // ---------- 1) เริ่มเกม ----------

    void BeginGameplay()
    {
        tasks.ActivateTask(CollectItems);
        if (ambience != null && !ambience.isPlaying) ambience.Play();
        ghostTimer = StartCoroutine(GhostAfterDelay());
    }

    IEnumerator GhostAfterDelay()
    {
        yield return new WaitForSeconds(ghostDelaySeconds);

        if (ghost == null || GameState.GameOver) yield break;

        Vector3 pos = ghostSpawnPoint != null ? ghostSpawnPoint.position : ghost.transform.position;
        ghost.StartWander(pos);
        WarningBanner.Show(ghostWarningText, warningSeconds);
    }

    // ---------- 2) Task ----------

    void OnTaskCompleted(TaskData t)
    {
        switch (t.id)
        {
            case CollectItems:
                if (bookObject != null) bookObject.SetActive(true);
                tasks.ActivateTask(GetBook);
                break;

            case GetBook:
                if (potObject != null) potObject.SetActive(true);     // หม้อดินเพิ่งโผล่หลังเก็บสมุด
                tasks.ActivateTask(GetPot);
                break;

            case Escape:
                // ถึงห้องพระ: ผีหยุดไล่และหายไป (ขว้างไม่ครบก็ไม่เป็นไร) ปิด Task ขว้างที่ค้าง แล้วไปพลิกพระพุทธรูป
                if (ghost != null) ghost.Banish();
                tasks.ForceComplete(ThrowGhost);
                if (throwController != null) throwController.enabled = false;
                tasks.ActivateTask(TurnBuddha);
                break;

            case TurnBuddha:
                tasks.ActivateTask(PlaceItems);
                break;

            case PlaceItems:
                tasks.ActivateTask(LightIncense);
                break;

            case LightIncense:
                StartEnding();
                break;
        }
    }

    // ---------- 3) หม้อดิน -> ผีโผล่ ----------

    void OnItemDetailClosed(ItemData item)
    {
        if (potGhostTriggered || item == null || potItem == null) return;
        if (item != potItem) return;

        potGhostTriggered = true;
        if (ghostTimer != null) StopCoroutine(ghostTimer);
        if (potObject != null) potSpawnPosition = potObject.transform.position;
        else if (PlayerControl.PlayerTransform != null) potSpawnPosition = PlayerControl.PlayerTransform.position;

        // เก็บหม้อดินครั้งแรก: ผีโผล่แถวจุดเก็บหม้อ หันคอตามผู้เล่น พอเดินห่างถึงเริ่มไล่
        if (ghost != null) ghost.AppearAt(potSpawnPosition);
        if (tension != null) tension.Begin();
    }

    void OnGhostGazeStarted()
    {
        WarningBanner.Show(ghostWarningText, gazeWarningSeconds);
    }

    void OnGhostChaseStarted()
    {
        if (throwablesRoot != null) throwablesRoot.SetActive(true);
        if (throwController != null) throwController.enabled = true;

        // เปิดพร้อมกัน: ขว้างผี + หนีเข้าห้องพระ (ข้อไหนก่อนก็ได้)
        tasks.ActivateTask(Escape);
        tasks.ActivateTask(ThrowGhost);
    }

    // ---------- ผู้เล่นฟื้นหลัง Game Over (ลองอีกครั้ง) ----------

    void OnRevived()
    {
        if (ghost == null || endingStarted) return;

        if (potGhostTriggered)
        {
            // Revive หลังเก็บหม้อดิน: ผีโผล่ในระยะที่ผู้เล่นมองเห็น จ้องผู้เล่น
            // พอผู้เล่นเดินห่างระยะหนึ่งผีจะเริ่มไล่เอง (Task เดิมยังอยู่)
            ghost.AppearInFrontOfPlayer();
        }
        else
        {
            // ก่อนเก็บหม้อดิน: ผีเดินเตร่จากจุดสุ่มห่างผู้เล่น จั๊มป์สแคร์เมื่อถูกจ้องเท่านั้น
            Vector3 pos;
            if (!ghost.TryFindSpawnAwayFromPlayer(reviveSpawnMin, reviveSpawnMax, out pos))
                pos = ghostSpawnPoint != null ? ghostSpawnPoint.position : ghost.transform.position;
            ghost.StartWander(pos);
        }
    }

    // ---------- 4) ฉากจบ ----------

    void StartEnding()
    {
        if (endingStarted) return;
        endingStarted = true;
        if (tension != null)
        {
            tension.End();
            tension.SetDuckEnabled(false);   // กันชนกับการเฟดเสียงพื้นหลังด้านล่าง
        }
        if (ambience != null) StartCoroutine(FadeOutAndStop(ambience, ambienceFadeOutSeconds));
        StartCoroutine(EndingRoutine());
    }

    IEnumerator FadeOutAndStop(AudioSource source, float seconds)
    {
        float start = source.volume;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(start, 0f, t / seconds);
            yield return null;
        }
        source.Stop();
        source.volume = start;   // คืนค่าเดิมเผื่อเล่นใหม่
    }

    IEnumerator EndingRoutine()
    {
        PlayerControl.Lock(false);
        yield return new WaitForSeconds(endingDelaySeconds);
        PlayerControl.Unlock(false);   // ปลดคู่กับ Lock ด้านบน (VideoCutscene จะล็อกเองอีกชั้น)

        if (cutscene != null && endingClip != null)
            cutscene.Play(endingClip, ShowEnd, false, true, true, holdEndingLastFrame);
        else
            ShowEnd();
    }

    void ShowEnd()
    {
        if (endScreen != null)
        {
            endScreen.Show();
            return;
        }
        Debug.LogWarning("Chapter2Director: ช่อง End Screen ว่าง จึงโหลดซีน '" + endSceneName + "' แทน (ลาก EndOverlay ที่มี EndScreenUI ใส่ช่อง End Screen)");
        Time.timeScale = 1f;
        SceneManager.LoadScene(endSceneName);
    }
}