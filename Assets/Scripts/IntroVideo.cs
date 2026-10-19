using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideo : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] RawImage videoImage;          // อยู่ใต้ BlackBG ใน Hierarchy (ลำดับหลัง)
    [SerializeField] GameObject videoRoot;         // IntroCanvas
    [SerializeField] KeyCode skipKey = KeyCode.Space;
    [SerializeField] bool allowSkip = true;

    [Header("Fade")]
    [SerializeField] CanvasGroup fadeGroup;               // CanvasGroup บน IntroCanvas
    [SerializeField] float blackHoldSeconds = 0.5f;       // ค้างจอดำก่อนเริ่มเฟดอิน
    [SerializeField] float videoFadeInDuration = 1.5f;    // ดำ -> วิดีโอ
    [SerializeField] float videoFadeOutDuration = 1.5f;   // วิดีโอ -> ดำ
    [SerializeField] float gameFadeDuration = 1.5f;       // ดำ -> เข้าเกม

    [Header("สิ่งที่ปิดไว้ระหว่างวิดีโอ")]
    [SerializeField] Behaviour[] disableWhilePlaying;

    bool finished;
    Coroutine fadeInRoutine;

    void Awake()
    {
        foreach (var b in disableWhilePlaying)
            if (b != null) b.enabled = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        if (videoRoot != null) videoRoot.SetActive(true);

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = true;
        }

        if (videoImage != null)
        {
            videoImage.color = new Color(1f, 1f, 1f, 0f);   // โปร่งใส เห็นแต่พื้นดำ
            videoImage.enabled = false;
        }
    }

    IEnumerator Start()
    {
        if (videoPlayer == null || videoImage == null)
        {
            Debug.LogError("[Intro] ยังไม่ได้ลาก Video Player หรือ Video Image");
            Finish();
            yield break;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.errorReceived += (vp, msg) => Debug.LogError("[Intro] Video error: " + msg);

        videoPlayer.Prepare();
        float timeout = Time.unscaledTime + 40f;
        while (!videoPlayer.isPrepared && Time.unscaledTime < timeout)
            yield return null;

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("[Intro] Prepare ไม่สำเร็จ (ไฟล์/codec มีปัญหา) ข้ามไปเข้าเกม");
            Finish();
            yield break;
        }

        var rt = new RenderTexture((int)videoPlayer.width, (int)videoPlayer.height, 0);
        videoPlayer.targetTexture = rt;
        videoImage.texture = rt;

        // ค้างจอดำสั้นๆ ก่อนเริ่มเล่น
        if (blackHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(blackHoldSeconds);

        if (finished) yield break;   // ผู้เล่นกดข้ามระหว่างรอ

        videoPlayer.Play();
        yield return null;           // รอ 1 เฟรมให้เฟรมแรกถูกวาดลง texture
        videoImage.enabled = true;

        // เฟดอิน: ดำ -> วิดีโอ
        fadeInRoutine = StartCoroutine(FadeVideo(0f, 1f, videoFadeInDuration));
    }

    void Update()
    {
        if (finished) return;

        if (allowSkip && Input.GetKeyDown(skipKey))
        {
            Finish();
            return;
        }

        // เริ่มเฟดออกก่อนวิดีโอจบ ให้ภาพจางพร้อมช่วงท้ายคลิป
        if (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.length > 0 &&
            videoPlayer.length - videoPlayer.time <= videoFadeOutDuration)
            Finish();
    }

    void Finish()
    {
        if (finished) return;
        finished = true;

        // หยุดเฟดอินที่ค้างอยู่ ไม่ให้ชนกับเฟดออก
        if (fadeInRoutine != null)
        {
            StopCoroutine(fadeInRoutine);
            fadeInRoutine = null;
        }

        StartCoroutine(FinishRoutine());
    }

    IEnumerator FinishRoutine()
    {
        // 1) เฟดภาพวิดีโอเป็นดำ (เริ่มจาก alpha ปัจจุบัน เผื่อกดข้ามกลางเฟดอิน)
        if (videoImage != null && videoImage.enabled)
            yield return FadeVideo(videoImage.color.a, 0f, videoFadeOutDuration);

        if (videoPlayer != null) videoPlayer.Stop();

        // 2) เฟดจากดำเข้าเกม
        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            for (float t = 0f; t < gameFadeDuration; t += Time.unscaledDeltaTime)
            {
                fadeGroup.alpha = Mathf.Lerp(1f, 0f, t / gameFadeDuration);
                yield return null;
            }
            fadeGroup.alpha = 0f;
        }

        // 3) ปลดล็อกผู้เล่นหลังเฟดจบ
        foreach (var b in disableWhilePlaying)
            if (b != null) b.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (videoRoot != null) videoRoot.SetActive(false);
    }

    IEnumerator FadeVideo(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            videoImage.color = new Color(1f, 1f, 1f, to);
            yield break;
        }

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float a = Mathf.Lerp(from, to, t / duration);
            videoImage.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        videoImage.color = new Color(1f, 1f, 1f, to);
    }
}