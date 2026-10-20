using System.Collections;
using UnityEngine;

/// <summary>
/// เสียงประกอบระทึกขวัญ: Begin() เฟดเข้า, End() เฟดออก
/// ระหว่างที่ผีทำให้ Game Over (GameState.GameOver) จะเงียบลงเอง แล้วกลับมาเมื่อผู้เล่นฟื้น
/// ใส่ที่ Empty GameObject (เช่น TensionMusic) พร้อม AudioSource ที่ใส่เสียงระทึก ติ๊ก Loop และ ปิด Play On Awake
/// Chapter2Director เป็นผู้เรียก Begin() ตอนผีโผล่หลังเก็บหม้อดิน และ End() ตอนเริ่มคัทซีนจบ
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TensionMusic : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float fadeInSeconds = 2f;
    [SerializeField, Min(0.1f)] float fadeOutSeconds = 1.5f;

    [Header("ลดเสียงบรรยากาศปกติลงระหว่างที่เสียงระทึกเล่น (ไม่ใส่ก็ได้)")]
    [SerializeField] AudioSource ambienceToDuck;
    [Range(0f, 1f)] [SerializeField] float duckTo = 0.3f;

    AudioSource source;
    float maxVolume;
    float ambienceBase;
    bool wanted;          // Director สั่งให้เล่นอยู่
    bool duckEnabled = true;
    float level;          // 0-1 ระดับปัจจุบัน

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        maxVolume = source.volume > 0f ? source.volume : 1f;
        source.volume = 0f;
        if (ambienceToDuck != null) ambienceBase = ambienceToDuck.volume;
    }

    public void Begin()
    {
        wanted = true;
    }

    public void End()
    {
        wanted = false;
    }

    /// <summary>ปิด/เปิดการลดเสียงบรรยากาศ (Director ปิดตอนจะเฟดเสียงบรรยากาศทิ้งเอง)</summary>
    public void SetDuckEnabled(bool value)
    {
        duckEnabled = value;
    }

    void Update()
    {
        // เล่นเฉพาะตอนถูกสั่ง และไม่อยู่ในช่วง Game Over
        bool shouldPlay = wanted && !GameState.GameOver;
        float target = shouldPlay ? 1f : 0f;
        float seconds = shouldPlay ? fadeInSeconds : fadeOutSeconds;

        level = Mathf.MoveTowards(level, target, Time.unscaledDeltaTime / seconds);
        source.volume = level * maxVolume;

        if (level > 0f && !source.isPlaying) source.Play();
        else if (level <= 0f && source.isPlaying) source.Stop();

        if (ambienceToDuck != null && duckEnabled)
            ambienceToDuck.volume = Mathf.Lerp(ambienceBase, ambienceBase * duckTo, level);
    }
}