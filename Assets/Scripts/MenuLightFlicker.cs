using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuLightFlicker : MonoBehaviour
{
    [Header("แสงตะเกียง (Image วางที่ตะเกียงในภาพพื้นหลัง)")]
    [SerializeField] Image glow;
    [SerializeField] Color glowColor = new Color(1f, 0.70f, 0.35f, 1f);
    [SerializeField, Range(0f, 1f)] float glowBase = 0.35f;
    [SerializeField, Range(0f, 1f)] float glowFlicker = 0.25f;

    [Header("ความมืดที่วูบ (Image ดำเต็มจอ)")]
    [SerializeField] Image shade;
    [SerializeField, Range(0f, 1f)] float shadeDip = 0.55f;
    [SerializeField] Vector2 gapBetweenEvents = new Vector2(7f, 16f);
    [SerializeField] int minBlinks = 2;
    [SerializeField] int maxBlinks = 3;

    [Header("เสียง (ไม่ใส่ก็ได้)")]
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip flickerClip;

    [Header("ผู้เล่นที่ไวต่อแสง")]
    [SerializeField] bool calmMode;     // true = ไม่มีวูบแรง เหลือแค่แสงสั่นเบา ๆ

    float seed;
    float dip;                           // ระดับความมืดที่เพิ่มจากเหตุการณ์วูบ (0 ถึง 1)

    public void SetCalmMode(bool value) => calmMode = value;

    void Awake()
    {
        seed = Random.value * 100f;

        if (glow != null)
        {
            if (glow.sprite == null) glow.sprite = MakeRadialSprite();
            glow.raycastTarget = false;
        }
        if (shade != null)
        {
            shade.color = new Color(0f, 0f, 0f, 0f);
            shade.raycastTarget = false;
        }
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(2f);          // รอให้เมนูเปิดจากดำก่อน
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(gapBetweenEvents.x, gapBetweenEvents.y));
            if (!calmMode) yield return Blinks();
        }
    }

    void Update()
    {
        if (glow != null)
        {
            float n = Mathf.PerlinNoise(Time.unscaledTime * 9f, seed);
            float a = glowBase * (1f - glowFlicker * n) * (1f - dip);
            Color c = glowColor;
            c.a = a;
            glow.color = c;
        }
        if (shade != null)
            shade.color = new Color(0f, 0f, 0f, dip * shadeDip);
    }

    IEnumerator Blinks()
    {
        if (sfx != null && flickerClip != null) sfx.PlayOneShot(flickerClip);

        int count = Random.Range(minBlinks, maxBlinks + 1);
        for (int i = 0; i < count; i++)
        {
            float depth = Random.Range(0.6f, 1f);
            yield return Ramp(0f, depth, Random.Range(0.03f, 0.06f));
            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.12f));
            yield return Ramp(depth, 0f, Random.Range(0.05f, 0.1f));
            // เว้นอย่างน้อย 0.35 วินาที ไม่ให้วูบถี่เกิน 3 ครั้งต่อวินาที
            yield return new WaitForSecondsRealtime(Random.Range(0.35f, 0.6f));
        }
    }

    IEnumerator Ramp(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            dip = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        dip = to;
    }

    // สร้างวงแสงนุ่มด้วยโค้ด ไม่ต้องมีรูปก็ใช้ได้
    static Sprite MakeRadialSprite(int size = 128)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a *= a;                                  // ขอบนุ่ม
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}