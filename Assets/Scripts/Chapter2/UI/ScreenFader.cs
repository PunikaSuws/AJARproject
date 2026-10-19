using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// จอดำเต็มจอ (ใช้ตอนสติหมด / ลืมตาตื่น / เปลี่ยนซีน)
/// แบบ A (แนะนำถ้ามีอนิเมเตอร์): ใส่ Animator ที่มี Bool "Black" (true = ค่อย ๆ มืด, false = ค่อย ๆ สว่าง)
/// แบบ B: ไม่ใส่ Animator สคริปต์เฟด CanvasGroup เอง
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] Animator animator;
    [SerializeField] string blackBool = "Black";
    [SerializeField] string blackStateName = "Black";    // ชื่อ State จอดำสนิทใน Animator
    [SerializeField] string clearStateName = "Idle";     // ชื่อ State โปร่งใสใน Animator
    [SerializeField] float fadeSeconds = 1.2f;

    CanvasGroup group;

    void Awake()
    {
        Instance = this;
        group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        if (animator == null) group.alpha = 0f;
        else SetBlack(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetBlack(bool black)
    {
        group.blocksRaycasts = black;
        if (animator != null)
        {
            animator.SetBool(blackBool, black);
            string state = black ? blackStateName : clearStateName;
            if (animator.HasState(0, Animator.StringToHash(state)))
            {
                animator.Play(state, 0, 1f);
                animator.Update(0f);
            }
        }
        else
        {
            group.alpha = black ? 1f : 0f;
        }
    }

    public IEnumerator FadeToBlack()
    {
        group.blocksRaycasts = true;
        if (animator != null)
        {
            animator.SetBool(blackBool, true);
            yield return new WaitForSecondsRealtime(fadeSeconds);
        }
        else
        {
            float start = group.alpha;
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, 1f, t / fadeSeconds);
                yield return null;
            }
            group.alpha = 1f;
        }
    }

    public IEnumerator FadeFromBlack()
    {
        if (animator != null)
        {
            animator.SetBool(blackBool, false);
            yield return new WaitForSecondsRealtime(fadeSeconds);
        }
        else
        {
            float start = group.alpha;
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, 0f, t / fadeSeconds);
                yield return null;
            }
            group.alpha = 0f;
        }
        group.blocksRaycasts = false;
    }

    /// <summary>เฟดเป็นสีดำ แล้วเรียก action (เช่น โหลดซีน)</summary>
    public void FadeToBlackThen(Action action)
    {
        StartCoroutine(FadeThenRoutine(action));
    }

    IEnumerator FadeThenRoutine(Action action)
    {
        yield return FadeToBlack();
        if (action != null) action();
    }
}
