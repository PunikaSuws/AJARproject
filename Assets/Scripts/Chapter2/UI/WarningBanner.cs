using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>ข้อความเตือนกลางจอ เช่น "อย่าจ้องผีเกิน 3 วินาที" เรียก WarningBanner.Show("...", 4f)</summary>
public class WarningBanner : MonoBehaviour
{
    static WarningBanner instance;

    [SerializeField] TMP_Text label;
    [SerializeField] CanvasGroup group;
    [SerializeField] float fadeSeconds = 0.6f;

    Coroutine routine;

    void Awake()
    {
        instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Show(string text, float seconds = 4f)
    {
        if (instance != null) instance.Run(text, seconds);
    }

    void Run(string text, float seconds)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Routine(text, seconds));
    }

    IEnumerator Routine(string text, float seconds)
    {
        label.text = text;

        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / fadeSeconds;
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSecondsRealtime(seconds);

        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / fadeSeconds;
            yield return null;
        }
        group.alpha = 0f;
        routine = null;
    }
}
