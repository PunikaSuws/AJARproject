using System.Collections;
using UnityEngine;

/// <summary>
/// เสียงตอน "ผู้เล่นหลับตานับ 1-10" ก่อนลืมตาออกหา
/// ค่าเริ่มต้น: เริ่มเล่นเมื่อ Seek Dialogue (PlayerSeeker) จบ ซึ่งเป็นจังหวะที่ผู้เล่นเป็นฝ่ายนับ
/// (เลือกให้เล่นตอน First จบ ได้ที่ช่อง Trigger ถ้าอยากให้อิงเป็นฝ่ายนับ)
/// *** ห้ามใส่ที่ตัวอิง *** เพราะอิงถูก Destroy ตอนเดินออก เสียงจะขาดกลางคัน
/// ให้สร้าง Empty GameObject ใหม่ (เช่น CountSound) ใส่สคริปต์นี้ แล้วลากอิงมาใส่ช่อง Npc Dialogue
///   แบบ A: ใส่ Count Clip = ไฟล์เสียงนับ 1-10 ไฟล์เดียว
///   แบบ B: เว้น Count Clip ว่าง ใส่ Tick Clip = เสียงติ๊กสั้น ๆ จะเล่นทุกวินาทีรวม 10 ครั้ง
/// </summary>
public class CountSound : MonoBehaviour
{
    public enum CountTrigger { AfterSeekDialogue, AfterFirstDialogue }

    [SerializeField] NpcDialogue npcDialogue;

    [Header("เริ่มนับเมื่อไหร่")]
    [SerializeField] CountTrigger trigger = CountTrigger.AfterSeekDialogue;
    [SerializeField, Min(0f)] float startDelay = 0f;   // หน่วงก่อนเริ่มเสียง (วินาที)

    [Header("เสียง")]
    [SerializeField] AudioClip countClip;
    [SerializeField] AudioClip tickClip;
    [Range(0f, 1f)] [SerializeField] float volume = 1f;

    [Header("ตั้งให้ตรงกับ IngSearchController")]
    [SerializeField, Min(0.1f)] float countDuration = 10f;
    [SerializeField, Min(1)] int tickCount = 10;

    [Header("เสียงตอนนับจบ / ตอนลืมตา (ไม่ใส่ก็ได้)")]
    [SerializeField] AudioClip finishClip;

    AudioSource source;
    Coroutine routine;

    void Awake()
    {
        if (npcDialogue == null) npcDialogue = GetComponent<NpcDialogue>();
        if (npcDialogue != null && npcDialogue.gameObject == gameObject)
            Debug.LogWarning("[IngCountSound] ใส่บนตัวอิง เสียงจะขาดตอนอิงถูก Destroy ให้ย้ายไปไว้บน Empty GameObject แยกต่างหาก", this);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 2D เพราะเป็นเสียงในหัวผู้เล่น
    }

    void OnEnable()
    {
        if (npcDialogue != null) npcDialogue.DialogueCompleted += OnDialogueCompleted;
    }

    void OnDisable()
    {
        if (npcDialogue != null) npcDialogue.DialogueCompleted -= OnDialogueCompleted;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (source != null) source.Stop();
    }

    void OnDialogueCompleted(NpcDialogue dialogue, NpcConversationStage stage)
    {
        NpcConversationStage wanted = trigger == CountTrigger.AfterSeekDialogue
            ? NpcConversationStage.PlayerSeeker
            : NpcConversationStage.First;
        if (dialogue != npcDialogue || stage != wanted) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CountRoutine());
    }

    IEnumerator CountRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        float v = volume * GameAudio.SfxVolume;

        if (countClip != null)
        {
            source.clip = countClip;
            source.volume = v;
            source.Play();
            yield return new WaitForSeconds(countDuration);
        }
        else if (tickClip != null)
        {
            float interval = countDuration / tickCount;
            for (int i = 0; i < tickCount; i++)
            {
                source.PlayOneShot(tickClip, v);
                yield return new WaitForSeconds(interval);
            }
        }
        else
        {
            yield return new WaitForSeconds(countDuration);
        }

        if (finishClip != null) source.PlayOneShot(finishClip, v);
        routine = null;
    }
}