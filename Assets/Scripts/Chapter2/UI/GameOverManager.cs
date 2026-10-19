using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// จบเกม 2 แบบ
///  1) จ้องผีเกินกำหนด -> ผีพุ่งใส่ -> วิดีโอจั๊มป์สแคร์ (Trigger)
///  2) สติเหลือ 0 (ผีไล่ทัน) -> จอดำ (TriggerSanity)
/// แล้วขึ้นหน้า Game Over (Overlay เฟดเข้า): "ลองอีกครั้ง" = ผู้เล่นลืมตาตื่นที่เดิม ผีโผล่ในสายตาแต่ยังไม่ไล่
/// (ไม่โหลดซีนใหม่) / "เมนูหลัก"
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    /// <summary>ยิงตอนผู้เล่นฟื้น (จอยังดำอยู่) ให้ Director จัดผีใหม่</summary>
    public static event Action Revived;

    [SerializeField] ScreenFader fader;
    [SerializeField] VideoCutscene cutscene;
    [SerializeField] VideoClip jumpscareClip;
    [SerializeField] OverlayPanel gameOverPanel;
    [SerializeField] Button retryButton;
    [SerializeField] Button menuButton;
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField] float wakeDelay = 1.0f;          // หน่วงตอนจอดำก่อนลืมตา
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip jumpscareSting;

    bool busy;
    bool usedJumpscare;

    void Awake()
    {
        Instance = this;
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Revived = null;
    }

    /// <summary>จั๊มป์สแคร์ (ผีพุ่งใส่หลังถูกจ้อง)</summary>
    public static void Trigger()
    {
        if (Instance != null) Instance.Run(true);
    }

    /// <summary>สติหมด</summary>
    public static void TriggerSanity()
    {
        if (Instance != null) Instance.Run(false);
    }

    void Run(bool jumpscare)
    {
        if (GameState.GameOver) return;
        GameState.GameOver = true;
        usedJumpscare = jumpscare;

        foreach (GhostController g in FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            g.Freeze();

        PlayerControl.Lock(false);

        if (jumpscare && cutscene != null)
        {
            if (sfx != null && jumpscareSting != null) sfx.PlayOneShot(jumpscareSting);
            cutscene.Play(jumpscareClip, ShowGameOver, false, false, true);
        }
        else
        {
            StartCoroutine(SanityRoutine());
        }
    }

    IEnumerator SanityRoutine()
    {
        if (fader != null) yield return fader.FadeToBlack();
        ShowGameOver();
    }

    void ShowGameOver()
    {
        Time.timeScale = 1f;
        PlayerControl.Lock(true);

        // หลังจั๊มป์สแคร์ให้จอดำอยู่ที่ fader แทนวิดีโอ
        if (usedJumpscare)
        {
            if (fader != null) fader.SetBlack(true);
            if (cutscene != null) cutscene.ClearBlack();
        }

        if (gameOverPanel != null) gameOverPanel.Show();
    }

    // ---------- ปุ่ม ----------

    void Retry()
    {
        if (busy || !GameState.GameOver) return;
        StartCoroutine(RetryRoutine());
    }

    IEnumerator RetryRoutine()
    {
        busy = true;

        // Overlay เฟดออก (จอยังดำอยู่)
        if (gameOverPanel != null) yield return gameOverPanel.HideRoutine();

        // รีเซ็ตสถานะ ขณะจอดำ
        GameState.GameOver = false;
        if (SanityManager.Instance != null) SanityManager.Instance.ResetSanity();
        if (Revived != null) Revived();

        yield return new WaitForSecondsRealtime(wakeDelay);

        // ลืมตา
        if (fader != null) yield return fader.FadeFromBlack();

        PlayerControl.Unlock(true);
        PlayerControl.Unlock(false);
        busy = false;
    }

    void ToMenu()
    {
        if (busy) return;
        busy = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuScene);
    }
}
