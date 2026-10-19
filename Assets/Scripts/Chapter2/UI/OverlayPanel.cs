using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หน้าต่าง Overlay แบบเฟดเข้า/ออก (Pause, Game Over, จบเกม)
/// ใส่ที่ Panel ที่มี CanvasGroup ปล่อย Active ไว้ได้ (ซ่อนด้วย Alpha = 0)
/// ใช้เวลาจริง (unscaled) จึงเฟดได้ตอนเกมหยุด (Time.timeScale = 0)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class OverlayPanel : MonoBehaviour
{
    [SerializeField] float fadeSeconds = 0.4f;

    [Header("พื้นหลังเกมมืดลงตอนขึ้น Overlay")]
    [Tooltip("สร้างชั้นสีดำเต็มจอไว้หลังหน้าต่างให้เอง (เฟดพร้อมกับ Overlay)")]
    [SerializeField] bool autoBackdrop = true;
    [Range(0f, 1f)]
    [SerializeField] float backdropAlpha = 0.8f;

    CanvasGroup group;
    Coroutine current;
    RectTransform backdropRt;

    public bool IsVisible { get; private set; }

    void Awake()
    {
        EnsureGroup();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        if (autoBackdrop) CreateBackdrop();
    }

    void CreateBackdrop()
    {
        Transform existing = transform.Find("AutoBackdrop");
        if (existing != null)
        {
            backdropRt = (RectTransform)existing;
        }
        else
        {
            GameObject go = new GameObject("AutoBackdrop", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            backdropRt = (RectTransform)go.transform;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, backdropAlpha);
            img.raycastTarget = true;
        }
        FitBackdrop();
    }

    /// <summary>ขยายชั้นดำให้เต็มทั้ง Canvas (ไม่ว่า Overlay ตัวเองจะเล็กกว่าจอแค่ไหน)</summary>
    void FitBackdrop()
    {
        if (backdropRt == null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRt = (RectTransform)canvas.rootCanvas.transform;

        backdropRt.anchorMin = backdropRt.anchorMax = new Vector2(0.5f, 0.5f);
        backdropRt.pivot = new Vector2(0.5f, 0.5f);

        float scale = canvasRt.lossyScale.x / Mathf.Max(0.0001f, backdropRt.parent.lossyScale.x);
        backdropRt.sizeDelta = canvasRt.rect.size * scale * 1.05f;
        backdropRt.rotation = canvasRt.rotation;
        backdropRt.position = canvasRt.TransformPoint(canvasRt.rect.center);
    }

    void EnsureGroup()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
    }

    public void Show()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        EnsureGroup();
        FitBackdrop();
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeRoutine(1f));
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy) { IsVisible = false; return; }
        EnsureGroup();
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeRoutine(0f));
    }

    /// <summary>ใช้ใน Coroutine ของตัวอื่น: yield return panel.HideRoutine();</summary>
    public IEnumerator HideRoutine()
    {
        if (!gameObject.activeInHierarchy) { IsVisible = false; yield break; }
        EnsureGroup();
        yield return FadeRoutine(0f);
    }

    IEnumerator FadeRoutine(float target)
    {
        IsVisible = target > 0.5f;

        if (target > 0.5f)
            group.blocksRaycasts = true;
        group.interactable = false;

        float start = group.alpha;
        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
            yield return null;
        }
        group.alpha = target;

        group.blocksRaycasts = target > 0.5f;
        group.interactable = target > 0.5f;
        current = null;
    }
}