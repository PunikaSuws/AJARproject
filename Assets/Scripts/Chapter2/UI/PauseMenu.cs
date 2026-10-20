using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// กด ESC = เปิด/ปิดหน้า Pause (Overlay เฟด): เล่นต่อ / เริ่มใหม่ / เมนูหลัก / เสียงพื้นหลัง / เสียงเอฟเฟกต์
/// ใช้ได้ทั้ง Chapter 1 (ไม่มี PlayerControl) และ Chapter 2 (มี PlayerControl)
/// ไม่เปิดตอน: คัตซีน, Game Over, หน้าต่างรายละเอียดไอเท็ม, หน้าจบเกม, หรือ Object ใน Block Pause While Active ยังเปิดอยู่
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] OverlayPanel panel;
    [Tooltip("เสียงรวมทั้งเกม (ไม่ใส่ก็ได้)")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button menuButton;
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField] KeyCode pauseKey = KeyCode.Escape;

    [Header("สำหรับ Chapter 1 (ไม่มี PlayerControl)")]
    [Tooltip("ใส่ FirstPersonLook ของผู้เล่น จะล็อกกล้องตอน Pause (ไม่ต้องใส่ถ้า Scene มี PlayerControl)")]
    [SerializeField] FirstPersonLook playerLook;
    [Tooltip("ไดอาล็อกกำลังเปิดอยู่ = ไม่ให้ Pause (ไม่ใส่ก็ได้)")]
    [SerializeField] DialogueUI dialogueUI;
    [Tooltip("วัตถุเหล่านี้เปิดอยู่ = ไม่ให้ Pause เช่น Canvas วิดีโอเปิดเรื่อง")]
    [SerializeField] GameObject[] blockWhileActive;

    [Header("Chapter 1 (ไม่มี PlayerControl)")]
    [Tooltip("สคริปต์ที่ต้องหยุดตอน Pause เช่น FirstPersonLook, FirstPersonMovement (ว่างไว้ = หาเอง)")]
    [SerializeField] Behaviour[] freezeWhilePaused;
    [Tooltip("ถ้า Object ใดในนี้ยัง Active อยู่ จะ Pause ไม่ได้ เช่น Canvas วิดีโอเปิดเรื่อง")]
    [SerializeField] GameObject[] blockPauseWhileActive;

    bool paused;
    bool leaving;
    bool manualLock;
    bool prevLookLocked;
    CursorLockMode prevCursorLock;
    bool prevCursorVisible;

    // สถานะก่อน Pause (ใช้เฉพาะโหมดไม่มี PlayerControl)
    readonly List<Behaviour> frozen = new List<Behaviour>();
    CursorLockMode prevLock;
    bool prevVisible;
    bool usedPlayerControl;

    void Awake()
    {
        if (masterSlider != null)
        {
            masterSlider.minValue = 0f; masterSlider.maxValue = 1f;
            masterSlider.SetValueWithoutNotify(GameAudio.MasterVolume);
            masterSlider.onValueChanged.AddListener(v => GameAudio.MasterVolume = v);

            // ใช้สไลเดอร์ตัวเดียว: ล้างค่าเสียงดนตรี/เอฟเฟกต์เก่าที่เคยตั้งไว้ให้เป็นเต็ม ไม่งั้นเสียงจะเบากว่าที่สไลเดอร์บอก
            if (musicSlider == null && sfxSlider == null)
            {
                GameAudio.MusicVolume = 1f;
                GameAudio.SfxVolume = 1f;
            }
        }
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f; musicSlider.maxValue = 1f;
            musicSlider.SetValueWithoutNotify(GameAudio.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => GameAudio.MusicVolume = v);
        }
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(GameAudio.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => GameAudio.SfxVolume = v);
        }
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
    }

    void Update()
    {
        if (leaving || !Input.GetKeyDown(pauseKey)) return;
        if (GameState.Cutscene || GameState.GameOver || ItemDetailView.IsOpen || EndScreenUI.IsShown) return;
        if (dialogueUI != null && dialogueUI.IsDialogueOpen) return;
        if (blockWhileActive != null)
            foreach (GameObject go in blockWhileActive)
                if (go != null && go.activeInHierarchy) return;

        if (!paused && blockPauseWhileActive != null)
            foreach (GameObject go in blockPauseWhileActive)
                if (go != null && go.activeInHierarchy) return;

        if (paused) Resume();
        else Pause();
    }

    void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        usedPlayerControl = PlayerControl.Instance != null;
        if (usedPlayerControl)
        {
            PlayerControl.Lock(true);
        }
        else
        {
            FreezePlayerManually();
        }

        if (panel != null) panel.Show();
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (usedPlayerControl) PlayerControl.Unlock(true);
        else UnfreezePlayerManually();

        if (panel != null) panel.Hide();
    }

    void FreezePlayerManually()
    {
        frozen.Clear();

        if (freezeWhilePaused == null || freezeWhilePaused.Length == 0)
        {
            FirstPersonLook look = FindFirstObjectByType<FirstPersonLook>();
            FirstPersonMovement move = FindFirstObjectByType<FirstPersonMovement>();
            freezeWhilePaused = new Behaviour[] { look, move };
        }

        foreach (Behaviour b in freezeWhilePaused)
        {
            if (b != null && b.enabled)
            {
                b.enabled = false;      // เก็บไว้เฉพาะตัวที่เปิดอยู่เดิม จะได้คืนค่าถูกต้อง
                frozen.Add(b);
            }
        }

        prevLock = Cursor.lockState;
        prevVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void UnfreezePlayerManually()
    {
        foreach (Behaviour b in frozen)
            if (b != null) b.enabled = true;
        frozen.Clear();

        Cursor.lockState = prevLock;
        Cursor.visible = prevVisible;
    }

    void LockPlayer()
    {
        if (PlayerControl.Instance != null)
        {
            PlayerControl.Lock(true);
            manualLock = false;
            return;
        }

        manualLock = true;
        prevCursorLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        if (playerLook != null)
        {
            prevLookLocked = playerLook.LookLocked;
            playerLook.LookLocked = true;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void UnlockPlayer()
    {
        if (!manualLock)
        {
            PlayerControl.Unlock(true);
            return;
        }

        manualLock = false;
        if (playerLook != null) playerLook.LookLocked = prevLookLocked;
        Cursor.lockState = prevCursorLock;
        Cursor.visible = prevCursorVisible;
    }

    void Restart()
    {
        if (leaving) return;
        leaving = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        int index = SceneManager.GetActiveScene().buildIndex;
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeToBlackThen(() => SceneManager.LoadScene(index));
        else
            SceneManager.LoadScene(index);
    }

    void ToMenu()
    {
        if (leaving) return;
        leaving = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        string scene = mainMenuScene;
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeToBlackThen(() => SceneManager.LoadScene(scene));
        else
            SceneManager.LoadScene(scene);
    }
}