using System.Collections;
using TMPro;
using UnityEngine;

public class TaskItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text label;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color completedColor = Color.green;
    [SerializeField] float holdSeconds = 1.5f;   // ค้างไว้หลังเสร็จก่อนจาง
    [SerializeField] float fadeSeconds = 0.5f;

    public TaskData Data { get; private set; }

    public void Setup(TaskData data)
    {
        Data = data;
        label.color = normalColor;
        canvasGroup.alpha = 1f;
        Refresh();
    }

    public void Refresh()
    {
        if (Data.targetAmount > 1)
            label.text = $"{Data.title} ({Data.currentAmount}/{Data.targetAmount})";
        else
            label.text = Data.title;
    }

    public void PlayCompleted()
    {
        Refresh();
        label.color = completedColor;
        StartCoroutine(FadeAndRemove());
    }

    IEnumerator FadeAndRemove()
    {
        yield return new WaitForSeconds(holdSeconds);

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeSeconds);
            yield return null;
        }

        Destroy(gameObject);
    }
}