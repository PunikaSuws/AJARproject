using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ChapterSelectItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [SerializeField] int chapterNumber = 1;
    [SerializeField] string sceneName = "Chapter1";
    [SerializeField] MenuTransition.Style style = MenuTransition.Style.LightsOut;

    [Header("ตอนล็อก")]
    [SerializeField] GameObject lockedVisual;       // สายสิญจน์และยันต์ (ซ่อนเมื่อปลดล็อก)
    [SerializeField] CanvasGroup lockedHint;        // ข้อความ "เล่นตอนที่ ๑ ให้จบก่อน"
    [SerializeField] RectTransform shakeTarget;     // ตัวที่สั่น (ลูกของปุ่ม ถ้าว่างจะสั่นปุ่มเอง)
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip lockedClip;

    [Header("ทดสอบ")]
    [SerializeField] bool forceUnlocked;            // เปิดไว้ทดสอบเท่านั้น

    Button button;
    bool shaking;
    Coroutine hintRoutine;

    bool IsUnlocked => forceUnlocked || ChapterProgress.IsUnlocked(chapterNumber);

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Open);
        if (shakeTarget == null) shakeTarget = (RectTransform)transform;
        if (lockedHint != null) lockedHint.alpha = 0f;
        Refresh();
    }

    void Refresh()
    {
        bool open = IsUnlocked;
        button.interactable = open;                    // ล็อก: Button จะใช้ Disabled Sprite
        if (lockedVisual != null) lockedVisual.SetActive(!open);
    }

    void Open()
    {
        if (!IsUnlocked) return;
        if (MenuTransition.Instance != null) MenuTransition.Instance.ToScene(sceneName, style);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!IsUnlocked && !shaking) StartCoroutine(Shake(0.2f, 5f));
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (IsUnlocked) return;
        if (sfx != null && lockedClip != null) sfx.PlayOneShot(lockedClip);
        if (!shaking) StartCoroutine(Shake(0.35f, 12f));
        if (lockedHint != null)
        {
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            hintRoutine = StartCoroutine(ShowHint());
        }
    }

    IEnumerator Shake(float duration, float strength)
    {
        shaking = true;
        Vector2 origin = shakeTarget.anchoredPosition;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / duration;
            shakeTarget.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 70f) * strength * k, 0f);
            yield return null;
        }
        shakeTarget.anchoredPosition = origin;
        shaking = false;
    }

    IEnumerator ShowHint()
    {
        lockedHint.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.6f);
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            lockedHint.alpha = 1f - t / 0.4f;
            yield return null;
        }
        lockedHint.alpha = 0f;
    }
}