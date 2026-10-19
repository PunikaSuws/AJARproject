using UnityEngine;

/// <summary>
/// ใส่ที่ GameObject ที่มี AudioSource เพื่อให้ปรับเสียงตามสไลเดอร์ใน Pause
/// เลือก Kind = Music (เพลงพื้นหลัง/บรรยากาศ) หรือ Sfx (เสียงเอฟเฟกต์)
/// ค่า Volume ใน AudioSource คือ "เสียงสูงสุด" ที่ตั้งไว้ สคริปต์จะคูณด้วยค่าจากสไลเดอร์
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    public enum Kind { Music, Sfx }

    [SerializeField] Kind kind = Kind.Sfx;

    AudioSource source;
    float baseVolume = 1f;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
    }

    void OnEnable()
    {
        GameAudio.Changed += Apply;
        Apply();
    }

    void OnDisable()
    {
        GameAudio.Changed -= Apply;
    }

    void Apply()
    {
        if (source == null) return;
        float mul = kind == Kind.Music ? GameAudio.MusicVolume : GameAudio.SfxVolume;
        source.volume = baseVolume * mul;
    }
}
