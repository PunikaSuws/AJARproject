using System;
using UnityEngine;

/// <summary>ระดับเสียงดนตรีพื้นหลัง / เสียงเอฟเฟกต์ (บันทึกลง PlayerPrefs) ใช้ร่วมกับ AudioChannel</summary>
public static class GameAudio
{
    const string MusicKey = "MusicVolume";
    const string SfxKey = "SfxVolume";
    const string MasterKey = "MasterVolume";

    static float music = -1f;
    static float sfx = -1f;
    static float master = -1f;

    public static event Action Changed;

    public static float MusicVolume
    {
        get { if (music < 0f) music = PlayerPrefs.GetFloat(MusicKey, 1f); return music; }
        set
        {
            music = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, music);
            if (Changed != null) Changed();
        }
    }

    public static float SfxVolume
    {
        get { if (sfx < 0f) sfx = PlayerPrefs.GetFloat(SfxKey, 1f); return sfx; }
        set
        {
            sfx = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, sfx);
            if (Changed != null) Changed();
        }
    }

    /// <summary>ระดับเสียงรวมทั้งเกม 0-1 (บันทึกลง PlayerPrefs) คุมผ่าน AudioListener.volume</summary>
    public static float MasterVolume
    {
        get { if (master < 0f) master = PlayerPrefs.GetFloat(MasterKey, 1f); return master; }
        set
        {
            master = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, master);
            AudioListener.volume = master;
            if (Changed != null) Changed();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        music = -1f;
        sfx = -1f;
        master = -1f;
        Changed = null;
    }

    // ใช้ค่าที่บันทึกไว้ทุกครั้งที่โหลดซีน
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyMaster()
    {
        AudioListener.volume = MasterVolume;
    }

    /// <summary>เล่นเสียงเอฟเฟกต์ ณ ตำแหน่ง โดยคูณระดับเสียง SFX ให้</summary>
    public static void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume * SfxVolume));
    }
}