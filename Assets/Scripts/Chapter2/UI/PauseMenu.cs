using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>กด ESC ระหว่างเล่น = เปิด/ปิดหน้าตั้งค่า (หยุดเวลา, ปรับเสียง, เต็มจอ, กลับเมนู, ออกเกม)</summary>
public class PauseMenu : MonoBehaviour
{
    const string VolumeKey = "MasterVolume";

    [SerializeField] GameObject panel;
    [SerializeField] Slider volumeSlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Button resumeButton;
    [SerializeField] Button menuButton;
    [SerializeField] Button quitButton;
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField] KeyCode pauseKey = KeyCode.Escape;

    bool paused;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);

        float vol = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = vol;

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(vol);
            volumeSlider.onValueChanged.AddListener(OnVolume);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
        }
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    void OnVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
    }

    void Update()
    {
        if (!Input.GetKeyDown(pauseKey)) return;
        if (GameState.Cutscene || GameState.GameOver || ItemDetailView.IsOpen) return;

        if (paused) Resume();
        else Pause();
    }

    void Pause()
    {
        // ถ้ามีอย่างอื่นล็อกผู้เล่นอยู่ (เช่นโหมดเมาส์ของ Inventory) ก็ยังเปิดได้ แต่ซ้อนล็อกเพิ่ม
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        PlayerControl.Lock(true);
        if (panel != null) panel.SetActive(true);
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerControl.Unlock(true);
        if (panel != null) panel.SetActive(false);
    }

    void ToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuScene);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
