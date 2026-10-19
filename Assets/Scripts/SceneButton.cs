using UnityEngine;

public class SceneButton : MonoBehaviour
{
    [SerializeField] string sceneName = "MainMenu";
    [SerializeField] MenuTransition.Style style = MenuTransition.Style.Blink;

    public void Go()
    {
        if (MenuTransition.Instance != null) MenuTransition.Instance.ToScene(sceneName, style);
    }
}