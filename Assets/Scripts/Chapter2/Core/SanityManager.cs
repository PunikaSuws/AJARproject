using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ค่าสติผู้เล่น: ผีไล่หรือเดินเตร็ดเตร่อยู่ใกล้ = สติลดตามระยะ (ตอนไล่เร็วกว่า) ถึง 0 = Game Over (จอดำ -> หน้า Game Over)
/// ไม่มีผีอยู่ใกล้ = ฟื้นช้า ๆ ใส่ที่ Managers 1 ตัว
/// </summary>
public class SanityManager : MonoBehaviour
{
    public static SanityManager Instance { get; private set; }

    [SerializeField] GhostController ghost;
    [SerializeField] float maxSanity = 100f;

    [Header("อัตราการลด")]
    [SerializeField] float drainRange = 7f;            // ผีอยู่ใกล้กว่านี้ถึงเริ่มลด
    [SerializeField] float maxDrainPerSecond = 35f;    // ลด/วินาที ตอนผีชิดตัว (ก่อนคูณ)
    [SerializeField] float catchMultiplier = 2.5f;     // ผีประชิดตัวแล้ว คูณเพิ่ม

    [Header("ผีเดินเตร็ดเตร่ (Wander) ก็ทำให้สติลดได้ แต่ช้ากว่าตอนไล่")]
    [SerializeField] bool drainWhileWander = true;
    [SerializeField] float wanderDrainRange = 10f;         // ผีเดินเตร่อยู่ใกล้กว่านี้ถึงเริ่มลด
    [SerializeField] float wanderMaxDrainPerSecond = 6f;   // ลด/วินาที ตอนผีเดินเตร่ใกล้สุด
    [Tooltip("ติ๊ก = ลดเฉพาะตอนผู้เล่นมองเห็นผี (ไม่มีกำแพงบัง)")]
    [SerializeField] bool wanderNeedsLineOfSight = false;

    [Header("การฟื้น")]
    [SerializeField] float recoverDelay = 2.5f;
    [SerializeField] float recoverPerSecond = 6f;

    [Header("UI / เสียง (ไม่ใส่ก็ได้)")]
    [SerializeField] CanvasGroup vignette;             // Image สีแดงเข้มขอบจอ ยิ่งสติต่ำยิ่งชัด
    [SerializeField] Image barFill;                    // Image Type = Filled
    [Tooltip("แสดงค่าสติเป็นตัวเลข (TextMeshPro) ไม่ใส่ก็ได้")]
    [SerializeField] TMP_Text valueText;
    [Tooltip("{0} = ค่าสติ 0-100  เช่น  สติ {0}  หรือ  {0}%")]
    [SerializeField] string valueFormat = "สติ {0}";
    [SerializeField] AudioSource heartbeat;            // เสียงหัวใจเต้น (Loop)

    public float Sanity { get; private set; }

    float sinceThreat;
    bool triggered;

    void Awake()
    {
        Instance = this;
        Sanity = maxSanity;
        if (ghost == null)
            ghost = FindFirstObjectByType<GhostController>(FindObjectsInactive.Include);
        UpdateUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (GameState.GameOver || GameState.Cutscene)
        {
            if (heartbeat != null && heartbeat.isPlaying) heartbeat.Stop();
            return;
        }

        float rate = 0f;
        if (ghost != null && ghost.gameObject.activeInHierarchy)
        {
            float d = ghost.DistanceToPlayer;

            if (ghost.CurrentMode == GhostController.Mode.Chase)
            {
                if (d < drainRange)
                {
                    rate = maxDrainPerSecond * (1f - d / drainRange);
                    if (d <= ghost.CatchDistance + 0.2f) rate *= catchMultiplier;
                }
            }
            else if (drainWhileWander && ghost.CurrentMode == GhostController.Mode.Wander)
            {
                if (d < wanderDrainRange && (!wanderNeedsLineOfSight || CanSeeGhost()))
                    rate = wanderMaxDrainPerSecond * (1f - d / wanderDrainRange);
            }
        }

        if (rate > 0f)
        {
            Sanity -= rate * Time.deltaTime;
            sinceThreat = 0f;
        }
        else
        {
            sinceThreat += Time.deltaTime;
            if (sinceThreat >= recoverDelay)
                Sanity += recoverPerSecond * Time.deltaTime;
        }

        Sanity = Mathf.Clamp(Sanity, 0f, maxSanity);
        UpdateUI();

        if (Sanity <= 0f && !triggered)
        {
            triggered = true;
            GameOverManager.TriggerSanity();
        }
    }

    bool CanSeeGhost()
    {
        Transform cam = PlayerControl.CameraTransform != null
            ? PlayerControl.CameraTransform
            : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return false;

        Vector3 target = ghost.EyePosition;
        if (Physics.Linecast(cam.position, target, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            return hit.transform.IsChildOf(ghost.transform);
        return true;
    }

    public void ResetSanity()
    {
        Sanity = maxSanity;
        triggered = false;
        sinceThreat = 0f;
        UpdateUI();
    }

    void UpdateUI()
    {
        float lost = 1f - Sanity / maxSanity;

        if (vignette != null) vignette.alpha = Mathf.Clamp01(lost * 1.3f);
        if (barFill != null) barFill.fillAmount = Sanity / maxSanity;
        if (valueText != null)
            valueText.text = string.Format(valueFormat, Mathf.CeilToInt(Sanity / maxSanity * 100f));

        if (heartbeat != null)
        {
            if (lost > 0.05f)
            {
                if (!heartbeat.isPlaying) heartbeat.Play();
                heartbeat.volume = Mathf.Clamp01(lost) * GameAudio.SfxVolume;
                heartbeat.pitch = 1f + lost * 0.5f;
            }
            else if (heartbeat.isPlaying)
            {
                heartbeat.Stop();
            }
        }
    }
}