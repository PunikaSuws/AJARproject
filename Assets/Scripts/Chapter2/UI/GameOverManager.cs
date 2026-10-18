using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>ผีจับได้ -> ฉายวิดีโอจั๊มป์สแคร์ -> แสดงหน้า Game Over (เล่นใหม่ / เมนูหลัก)</summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [SerializeField] VideoCutscene cutscene;
    [SerializeField] VideoClip jumpscareClip;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] Button retryButton;
    [SerializeField] Button menuButton;
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip jumpscareSting;

    void Awake()
    {
        Instance = this;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Trigger()
    {
        if (Instance != null) Instance.Run();
    }

    void Run()
    {
        if (GameState.GameOver) return;
        GameState.GameOver = true;

        // ผีหยุดทุกตัว
        foreach (GhostController g in FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            g.Freeze();

        if (sfx != null && jumpscareSting != null) sfx.PlayOneShot(jumpscareSting);

        if (cutscene != null)
            cutscene.Play(jumpscareClip, ShowGameOver, false, false, true);
        else
            ShowGameOver();
    }

    void ShowGameOver()
    {
        Time.timeScale = 1f;
        PlayerControl.Lock(true);
        if (cutscene != null) cutscene.ClearBlack();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    void Retry()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuScene);
    }
}
