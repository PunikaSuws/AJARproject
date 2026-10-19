using System.Collections;
using UnityEngine;

public class HidingScareController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] IngSearchController ing;
    [SerializeField] HideController hide;
    [SerializeField] AudioSource audioSource;          // เสียงผี

    [Header("Ghost Sounds (ใส่ได้หลายอัน สุ่มเลือก)")]
    [SerializeField] AudioClip[] sounds;
    [SerializeField] Vector2 intervalRange = new Vector2(4f, 9f);

    [Header("Heartbeat")]
    [SerializeField] AudioSource heartbeatSource;
    [SerializeField] AudioClip heartbeatClip;
    [SerializeField] Transform ingTransform;
    [SerializeField] Transform playerTransform;
    [SerializeField] float farDistance = 20f;
    [SerializeField] float nearDistance = 3f;
    [SerializeField] Vector2 heartbeatVolume = new Vector2(0.2f, 1f);
    [SerializeField] Vector2 heartbeatPitch = new Vector2(0.9f, 1.4f);
    [SerializeField] float firstSoundDelay = 5f;   // รอกี่วินาทีหลังเริ่มซ่อน ก่อนเสียงแรก

    void Start()
    {
        if (heartbeatSource != null)
        {
            heartbeatSource.clip = heartbeatClip;
            heartbeatSource.loop = true;
            heartbeatSource.playOnAwake = false;
        }

        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (ing != null && !ing.HasFoundPlayer)
        {
            // รอจนผู้เล่นซ่อน
            while (hide == null || !hide.IsHiding)
            {
                if (ing == null || ing.HasFoundPlayer) yield break;
                yield return null;
            }

            // ดีเลย์ก่อนเสียงแรก (ถ้าออกจากที่แอบระหว่างรอ ให้เริ่มนับใหม่)
            float timer = 0f;
            while (timer < firstSoundDelay)
            {
                if (ing == null || ing.HasFoundPlayer) yield break;
                if (!hide.IsHiding) break;
                timer += Time.deltaTime;
                yield return null;
            }
            if (!hide.IsHiding) continue;

            // เล่นเสียงเป็นช่วงๆ ตราบที่ยังซ่อนอยู่
            while (hide.IsHiding && ing != null && !ing.HasFoundPlayer)
            {
                PlaySound();
                yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));
            }
        }
    }

    int lastSoundIndex = -1;

    void PlaySound()
    {
        if (audioSource == null || sounds == null || sounds.Length == 0) return;

        int i;
        do { i = Random.Range(0, sounds.Length); }
        while (sounds.Length > 1 && i == lastSoundIndex);

        lastSoundIndex = i;
        audioSource.PlayOneShot(sounds[i]);
        Debug.Log($"play sound {i} ({sounds[i].name}), total={sounds.Length}");
    }

    void Update()
    {
        if (heartbeatSource == null || heartbeatClip == null) return;

        bool shouldPlay = hide != null && hide.IsHiding &&
                          !(ing != null && ing.HasFoundPlayer);

        if (!shouldPlay)
        {
            if (heartbeatSource.isPlaying) heartbeatSource.Stop();
            return;
        }

        if (!heartbeatSource.isPlaying) heartbeatSource.Play();

        float t = 0f;
        if (ingTransform != null && playerTransform != null)
        {
            float d = Vector3.Distance(ingTransform.position, playerTransform.position);
            t = Mathf.InverseLerp(farDistance, nearDistance, d);
        }

        heartbeatSource.volume = Mathf.Lerp(heartbeatVolume.x, heartbeatVolume.y, t);
        heartbeatSource.pitch = Mathf.Lerp(heartbeatPitch.x, heartbeatPitch.y, t);
    }
}