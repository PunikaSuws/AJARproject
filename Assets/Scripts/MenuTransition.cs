using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class MenuTransition : MonoBehaviour
{
    public static MenuTransition Instance { get; private set; }

    public enum Style { Blink, LightsOut }

    [SerializeField] bool fadeInOnStart = true;   // เปิดฉากด้วยการสว่างขึ้นจากดำ
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip blinkClip;         // เสียงวูบ (ไม่ใส่ก็ได้)
    [SerializeField] AudioClip lightsOutClip;     // เสียงไฟดับ (ไม่ใส่ก็ได้)

    CanvasGroup group;
    bool busy;

    // ค่าความมืดของจังหวะไฟริบหรี่ ก่อนดับสนิท
    static readonly float[] LightsOutAlpha = { 0f, 0.55f, 0.1f, 0.75f, 0.25f, 0.9f, 0.5f, 1f };

    void Awake()
    {
        Instance = this;
        group = GetComponent<CanvasGroup>();
        group.alpha = fadeInOnStart ? 1f : 0f;
        group.blocksRaycasts = fadeInOnStart;
        group.interactable = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    IEnumerator Start()
    {
        if (!fadeInOnStart) yield break;
        yield return null;                          // ให้เฟรมแรกวาดจอดำก่อน
        yield return Flicker(1f, 0f, 0.7f);
        group.blocksRaycasts = false;
    }

    /// เปลี่ยนหน้าในฉากเดียวกัน: ดำวูบ, สลับหน้า, สว่างขึ้น
    public void Page(Action swap)
    {
        if (busy) return;
        StartCoroutine(PageRoutine(swap));
    }

    /// ไปฉากอื่น: ดำ แล้วโหลดฉาก (ฉากใหม่ต้องเปิดจอเอง)
    public void ToScene(string scene, Style style = Style.Blink)
    {
        if (busy) return;
        StartCoroutine(SceneRoutine(scene, style));
    }

    IEnumerator PageRoutine(Action swap)
    {
        busy = true;
        group.blocksRaycasts = true;
        PlaySfx(blinkClip);
        yield return Flicker(0f, 1f, 0.25f);
        if (swap != null) swap();
        yield return new WaitForSecondsRealtime(0.08f);
        yield return Flicker(1f, 0f, 0.35f);
        group.blocksRaycasts = false;
        busy = false;
    }

    IEnumerator SceneRoutine(string scene, Style style)
    {
        busy = true;
        group.blocksRaycasts = true;

        if (style == Style.LightsOut)
        {
            PlaySfx(lightsOutClip);
            yield return LightsOut(1.4f);
        }
        else
        {
            PlaySfx(blinkClip);
            yield return Flicker(0f, 1f, 0.45f);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    IEnumerator Flicker(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float k = t / time;
            float a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
            float n = Mathf.PerlinNoise(Time.unscaledTime * 22f, 0.37f) - 0.5f;
            a = Mathf.Clamp01(a + n * 0.45f * Mathf.Sin(k * Mathf.PI));   // สั่นช่วงกลาง ต้นกับปลายนิ่ง
            group.alpha = a;
            yield return null;
        }
        group.alpha = to;
    }

    IEnumerator LightsOut(float total)
    {
        int steps = LightsOutAlpha.Length - 1;
        float stepTime = total / steps;
        for (int i = 0; i < steps; i++)
        {
            float from = group.alpha;
            float to = LightsOutAlpha[i + 1];
            float dur = stepTime * UnityEngine.Random.Range(0.6f, 1.4f);
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
        }
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(0.35f);   // มืดสนิทและเงียบ
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip);
    }
}