using UnityEngine;

public static class ChapterProgress
{
    const string Prefix = "chapter_unlocked_";

    public static bool IsUnlocked(int chapter)
    {
        if (chapter <= 1) return true;
        return PlayerPrefs.GetInt(Prefix + chapter, 0) == 1;
    }

    public static void Unlock(int chapter)
    {
        if (chapter <= 1) return;
        PlayerPrefs.SetInt(Prefix + chapter, 1);
        PlayerPrefs.Save();
    }
}