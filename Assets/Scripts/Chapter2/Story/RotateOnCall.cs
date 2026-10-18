using System.Collections;
using UnityEngine;

/// <summary>หมุนวัตถุ (เช่น พระพุทธรูป) เมื่อถูกเรียก Rotate() จาก UnityEvent ของ TaskInteractable</summary>
public class RotateOnCall : MonoBehaviour
{
    [SerializeField] Vector3 eulerDelta = new Vector3(0f, 180f, 0f);
    [SerializeField] float seconds = 1.5f;
    [SerializeField] AudioSource sound;

    bool rotating;

    public void Rotate()
    {
        if (!rotating) StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        rotating = true;
        if (sound != null) sound.Play();

        Quaternion from = transform.rotation;
        Quaternion to = Quaternion.Euler(eulerDelta) * from;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        transform.rotation = to;
        rotating = false;
    }
}
