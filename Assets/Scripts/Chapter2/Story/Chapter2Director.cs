using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// ตัวควบคุมเนื้อเรื่อง Chapter 2 ทั้งหมด (วางไว้ที่ GameObject "Chapter2Director" ตัวเดียวใน Scene)
/// ลำดับ: คัตซีนเปิด -> collect_items -> (30 วิ ผีเดินเตร่) -> get_book -> get_pot -> ผีโผล่ตรงหน้า ->
///        ไล่ (escape + throw_ghost) -> ห้องพระ -> turn_buddha -> place_items -> light_incense -> วิดีโอจบ -> EndScene
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
    [SerializeField] string endSceneName = "EndScene";

    [Header("ผี")]
    [SerializeField] GhostController ghost;
    [SerializeField] Transform ghostSpawnPoint;
    [SerializeField] float ghostDelaySeconds = 30f;
    [TextArea] [SerializeField] string ghostWarningText = "อย่าจ้องมองผีเกิน 3 วินาที";
    [SerializeField] float warningSeconds = 5f;

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

    TaskManager tasks;
    bool potGhostTriggered;
    bool endingStarted;
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
        if (ghost != null) ghost.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        ItemDetailView.Closed += OnItemDetailClosed;
    }

    void OnDisable()
    {
        ItemDetailView.Closed -= OnItemDetailClosed;
        if (tasks != null) tasks.TaskCompleted -= OnTaskCompleted;
        if (ghost != null) ghost.ChaseStarted -= OnGhostChaseStarted;
    }

    void Start()
    {
        tasks = TaskManager.Instance;
        if (tasks == null) { Debug.LogError("Chapter2Director: ไม่พบ TaskManager ใน Scene"); return; }

        tasks.TaskCompleted += OnTaskCompleted;
        if (ghost != null) ghost.ChaseStarted += OnGhostChaseStarted;

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
                // ถึงห้องพระ: ผีหายไป ปิด Task ขว้างที่ค้างอยู่ แล้วไปพลิกพระพุทธรูป
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

        if (ghost != null) ghost.AppearInFrontOfPlayer();
    }

    void OnGhostChaseStarted()
    {
        if (throwablesRoot != null) throwablesRoot.SetActive(true);
        if (throwController != null) throwController.enabled = true;

        tasks.ActivateTask(Escape);
        tasks.ActivateTask(ThrowGhost);
    }

    // ---------- 4) ฉากจบ ----------

    void StartEnding()
    {
        if (endingStarted) return;
        endingStarted = true;
        StartCoroutine(EndingRoutine());
    }

    IEnumerator EndingRoutine()
    {
        PlayerControl.Lock(false);
        yield return new WaitForSeconds(1.5f);

        if (cutscene != null && endingClip != null)
            cutscene.Play(endingClip, LoadEndScene, false, true, true);
        else
            LoadEndScene();
    }

    void LoadEndScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(endSceneName);
    }
}
