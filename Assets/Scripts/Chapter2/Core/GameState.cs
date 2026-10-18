using UnityEngine;

/// <summary>สถานะกลางของเกม (static) ใช้เช็คว่ากำลังฉายคัตซีน / จบเกม อยู่หรือไม่</summary>
public static class GameState
{
    public static bool Cutscene;
    public static bool GameOver;

    public static void Reset()
    {
        Cutscene = false;
        GameOver = false;
    }
}
