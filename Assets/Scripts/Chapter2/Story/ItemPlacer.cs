using System.Collections;
using UnityEngine;

/// <summary>
/// กด E ที่จุดวางของ: เผยวัตถุจำลองของไอเท็มทีละชิ้นบนพื้น (ทำวัตถุไว้ในฉากแล้วปิด Active ไว้)
/// เสร็จแล้วนับ Task place_items
/// </summary>
public class ItemPlacer : MonoBehaviour, IInteractable
{
    [SerializeField] string prompt = "[E] วางของทั้งหมดลงบนพื้น";
    [SerializeField] string requiredTaskId = "place_items";
    [SerializeField] string progressTaskId = "place_items";
    [Tooltip("วัตถุที่ปิด Active ไว้ในฉาก จะถูกเปิดทีละชิ้นตามลำดับ (ควรมี 6 ชิ้น: ปิ่น หวี รูป1 รูป2 สมุด หม้อดิน)")]
    [SerializeField] GameObject[] placedObjects;
    [SerializeField] float delayBetween = 0.5f;
    [SerializeField] AudioClip placeSound;
    [SerializeField] bool requireAllInInventory = false;

    bool used;
    bool busy;

    public string Prompt { get { return prompt; } }

    public bool CanInteract
    {
        get
        {
            if (used || busy) return false;
            return TaskManager.Instance != null && TaskManager.Instance.IsActive(requiredTaskId);
        }
    }

    void Awake()
    {
        foreach (GameObject go in placedObjects)
            if (go != null) go.SetActive(false);
    }

    public void Interact()
    {
        if (!CanInteract) return;
        StartCoroutine(PlaceRoutine());
    }

    IEnumerator PlaceRoutine()
    {
        busy = true;
        PlayerControl.Lock(false);

        foreach (GameObject go in placedObjects)
        {
            if (go == null) continue;
            go.SetActive(true);
            if (placeSound != null) GameAudio.PlaySfx(placeSound, go.transform.position);
            yield return new WaitForSeconds(delayBetween);
        }

        yield return new WaitForSeconds(0.5f);

        used = true;
        busy = false;
        PlayerControl.Unlock(false);

        if (TaskManager.Instance != null && !string.IsNullOrEmpty(progressTaskId))
            TaskManager.Instance.AddProgress(progressTaskId);
    }
}
