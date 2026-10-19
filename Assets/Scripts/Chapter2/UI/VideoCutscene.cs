using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// เล่นวิดีโอเต็มจอ (คัตซีนเปิดบท / จั๊มป์สแคร์ / ฉากจบ)
/// โครง: Canvas > VideoRoot (Image ดำ + CanvasGroup + สคริปต์นี้) > RawImage ; VideoPlayer วางที่ไหนก็ได้
/// </summary>
public class VideoCutscene : MonoBehaviour
{
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] RawImage videoImage;
    [SerializeField] CanvasGroup rootGroup;
    [SerializeField] float fadeSeconds = 0.8f;
    [SerializeField] KeyCode skipKey = KeyCode.Space;

    public bool IsPlaying { get; private set; }

    Coroutine routine;

    void Awake()
    {
        if (rootGroup == null) rootGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    void Hide()
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }
        if (videoImage != null) videoImage.enabled = false;
    }

    /// <param name="clip">วิดีโอ</param>
    /// <param name="onFinished">เรียกเมื่อจบ</param>
    /// <param name="skippable">กด Space ข้ามได้</param>
    /// <param name="fadeInAtStart">เฟดจากดำเข้าหาวิดีโอ</param>
    /// <param name="keepBlackAtEnd">true = จบแล้วค้างจอดำ (ใช้กับจั๊มป์สแคร์/ฉากจบ) false = เฟดออกคืนเกม</param>
    public void Play(VideoClip clip, Action onFinished, bool skippable = true,
                     bool fadeInAtStart = true, bool keepBlackAtEnd = false)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine(clip, onFinished, skippable, fadeInAtStart, keepBlackAtEnd));
    }

    IEnumerator PlayRoutine(VideoClip clip, Action onFinished, bool skippable, bool fadeIn, bool keepBlack)
    {
        IsPlaying = true;
        GameState.Cutscene = true;
        PlayerControl.Lock(false);

        // จอดำทันที
        rootGroup.alpha = 1f;
        rootGroup.blocksRaycasts = true;
        videoImage.enabled = false;

        if (clip == null)
        {
            Debug.LogWarning("VideoCutscene: ไม่ได้ใส่ VideoClip ข้ามไปเลย");
            yield return Finish(onFinished, keepBlack);
            yield break;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.Prepare();

        float timeout = Time.unscaledTime + 10f;
        while (!videoPlayer.isPrepared && Time.unscaledTime < timeout)
            yield return null;

        RenderTexture rt = new RenderTexture((int)clip.width, (int)clip.height, 0);
        videoPlayer.targetTexture = rt;
        videoImage.texture = rt;

        bool ended = false;
        VideoPlayer.EventHandler handler = _ => ended = true;
        videoPlayer.loopPointReached += handler;

        for (ushort track = 0; track < videoPlayer.audioTrackCount; track++)
            videoPlayer.SetDirectAudioVolume(track, GameAudio.MusicVolume);

        videoPlayer.Play();

        // รอเฟรมแรกออกก่อนค่อยโชว์ (กันภาพขาว/กระพริบ)
        float frameWait = Time.unscaledTime + 1f;
        while (videoPlayer.frame < 1 && Time.unscaledTime < frameWait)
            yield return null;

        videoImage.enabled = true;

        if (fadeIn)
        {
            Color c = videoImage.color;
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                c.a = t / fadeSeconds;
                videoImage.color = c;
                yield return null;
            }
            c.a = 1f;
            videoImage.color = c;
        }
        else
        {
            Color c = videoImage.color; c.a = 1f; videoImage.color = c;
        }

        while (!ended)
        {
            if (skippable && Input.GetKeyDown(skipKey)) break;
            yield return null;
        }

        videoPlayer.loopPointReached -= handler;
        videoPlayer.Stop();
        videoImage.enabled = false;
        videoPlayer.targetTexture = null;
        rt.Release();
        Destroy(rt);

        yield return Finish(onFinished, keepBlack);
    }

    IEnumerator Finish(Action onFinished, bool keepBlack)
    {
        if (!keepBlack)
        {
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                rootGroup.alpha = 1f - t / fadeSeconds;
                yield return null;
            }
            Hide();
        }

        IsPlaying = false;
        GameState.Cutscene = false;
        PlayerControl.Unlock(false);
        routine = null;

        if (onFinished != null) onFinished();
    }

    /// <summary>ซ่อนจอดำที่ค้างไว้ (keepBlackAtEnd) ด้วยตัวเอง</summary>
    public void ClearBlack()
    {
        Hide();
    }
}
