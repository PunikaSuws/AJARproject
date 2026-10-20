using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(Collider))]
public class ChapterEndTrigger : MonoBehaviour
{
    [Header("Fade (Panel ดำเต็มจอ + CanvasGroup, alpha เริ่มที่ 0)")]
    [SerializeField] CanvasGroup fadeGroup;
    [SerializeField] float fadeDuration = 1.5f;

    [Header("วิดีโอคัทซีน")]
    [SerializeField] VideoPlayer endVideo;
    [SerializeField] RawImage endVideoImage;

    [Header("หลังวิดีโอจบ")]
    [SerializeField] string nextSceneName = "Chapter2";

    [Header("ปิดการควบคุมผู้เล่นตอนเข้าคัทซีน")]
    [SerializeField] Behaviour[] disableOnTrigger;   // FirstPersonLook, FirstPersonMovement, HideController, PlayerNpcInteractor

    bool armed;
    bool triggered;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
        if (endVideoImage != null)
            endVideoImage.enabled = false;
    }

    // เรียกตอนผู้เล่นเริ่มหาอิง
    public void Arm() { armed = true; }

    void OnTriggerEnter(Collider other)
    {
        if (!armed || triggered || !other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(Cutscene());
    }

    IEnumerator Cutscene()
    {
        foreach (var b in disableOnTrigger)
            if (b != null) b.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        // 1) เฟดเป็นดำ
        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = true;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                fadeGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        // 2) เล่นวิดีโอคัทซีน
        if (endVideo == null || endVideoImage == null)
        {
            Debug.LogError("[ChapterEnd] ยังไม่ได้ลาก End Video / End Video Image");
            LoadNext();
            yield break;
        }

        endVideo.playOnAwake = false;
        endVideo.isLooping = false;
        endVideo.renderMode = VideoRenderMode.RenderTexture;
        endVideo.errorReceived += (vp, msg) => Debug.LogError("[ChapterEnd] Video error: " + msg);

        endVideo.Prepare();
        float timeout = Time.unscaledTime + 10f;
        while (!endVideo.isPrepared && Time.unscaledTime < timeout)
            yield return null;

        if (!endVideo.isPrepared)
        {
            Debug.LogError("[ChapterEnd] Prepare วิดีโอไม่สำเร็จ ข้ามไปฉากถัดไป");
            LoadNext();
            yield break;
        }

        var rt = new RenderTexture((int)endVideo.width, (int)endVideo.height, 0);
        endVideo.targetTexture = rt;
        endVideoImage.texture = rt;

        // ให้วิดีโอกับ RawImage อยู่เหนือพื้นดำ (ดูโครงสร้างด้านล่าง)
        endVideo.Play();
        yield return null;
        endVideoImage.enabled = true;

        // 3) รอวิดีโอจบ
        bool done = false;
        endVideo.loopPointReached += _ => done = true;
        while (!done)
            yield return null;

        // 4) ไป Chapter 2 อัตโนมัติ
        LoadNext();
    }

    void LoadNext()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}