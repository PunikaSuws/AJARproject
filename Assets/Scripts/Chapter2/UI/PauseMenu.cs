using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// กด ESC = เปิด/ปิดหน้า Pause (Overlay เฟด): เล่นต่อ / เริ่มใหม่ / เมนูหลัก / เสียงพื้นหลัง / เสียงเอฟเฟกต์
/// ไม่เปิดตอน: คัตซีน, Game Over, หน้าต่างรายละเอียดไอเท็ม, หน้าจบเกม
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] OverlayPanel panel;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button menuButton;
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField] KeyCode pauseKey = KeyCode.Escape;

    bool paused;
    bool leaving;

    void Awake()
    {
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

        if (paused) Resume();
        else Pause();
    }

    void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        PlayerControl.Lock(true);
        if (panel != null) panel.Show();
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerControl.Unlock(true);
        if (panel != null) panel.Hide();
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
