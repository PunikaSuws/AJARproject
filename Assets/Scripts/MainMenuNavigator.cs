using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuNavigator : MonoBehaviour
{
    [SerializeField] CanvasGroup mainPage;
    [SerializeField] CanvasGroup chapterPage;

    void Awake()
    {
        // กลับมาจากหน้าหยุดเกมหรือ Game Over สถานะอาจค้างอยู่
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPage(mainPage, true);
        SetPage(chapterPage, false);
    }

    public void OpenChapterSelect() => GoTo(chapterPage, mainPage);
    public void BackToMain()        => GoTo(mainPage, chapterPage);

    void GoTo(CanvasGroup show, CanvasGroup hide)
    {
        void Swap()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            SetPage(hide, false);
            SetPage(show, true);
        }

        if (MenuTransition.Instance != null) MenuTransition.Instance.Page(Swap);
        else Swap();
    }

    static void SetPage(CanvasGroup g, bool on)
    {
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }
}