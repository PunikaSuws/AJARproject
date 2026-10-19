using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>หน้าจบเกม (Overlay เฟดเข้า) ขึ้นหลังวิดีโอฉากจบเล่นเสร็จ มีปุ่มเมนูหลัก</summary>
public class EndScreenUI : MonoBehaviour
{
    public static bool IsShown { get; private set; }

    [SerializeField] OverlayPanel panel;
    [SerializeField] Button menuButton;
    [SerializeField] string mainMenuScene = "MainMenu";

    bool leaving;

    void Awake()
    {
        IsShown = false;
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
    }

    void OnDestroy()
    {
        IsShown = false;
    }

    public void Show()
    {
        if (IsShown) return;
        IsShown = true;
        Time.timeScale = 1f;
        PlayerControl.Lock(true);
        if (panel != null) panel.Show();
    }

    void ToMenu()
    {
        if (leaving) return;
        leaving = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuScene);
    }
}
