using TMPro;
using UnityEngine;

/// <summary>
/// แถบ Inventory ด้านล่างจอ: เพิ่มช่องเมื่อเก็บของ
/// กด Tab = เปิด/ปิดโหมดเมาส์ (คลิกช่องเพื่อดูรายละเอียด) หรือกดเลข 1-9 เปิดดูรายละเอียดทันที
/// </summary>
public class InventoryBarUI : MonoBehaviour
{
    [Header("แบบ A: มีช่องวางไว้แล้วในฉาก (ลากช่องทั้งหมดใส่ตามลำดับ)")]
    [SerializeField] InventorySlotUI[] fixedSlots;

    [Header("แบบ B: ไม่มีช่อง ให้สร้างช่องใหม่จาก Prefab (เว้นแบบ A ว่าง)")]
    [SerializeField] Transform container;
    [SerializeField] InventorySlotUI slotPrefab;

    [Header("อื่น ๆ")]
    [SerializeField] TMP_Text hintText;
    [SerializeField] KeyCode browseKey = KeyCode.Tab;
    [SerializeField] string hint = "Tab = เปิดเมาส์ / กดเลข 1-9 = ดูไอเท็ม";

    bool browsing;
    InventoryManager inventory;

    void Start()
    {
        inventory = InventoryManager.Instance;
        if (hintText != null) hintText.text = hint;

        if (inventory != null)
        {
            inventory.ItemAdded += OnItemAdded;
            for (int i = 0; i < inventory.Items.Count; i++) OnItemAdded(inventory.Items[i]);
        }
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.ItemAdded -= OnItemAdded;
    }

    void OnItemAdded(ItemData item)
    {
        // แบบ A: เติมลงช่องว่างช่องแรก
        if (fixedSlots != null && fixedSlots.Length > 0)
        {
            for (int i = 0; i < fixedSlots.Length; i++)
            {
                if (fixedSlots[i] != null && fixedSlots[i].IsEmpty)
                {
                    fixedSlots[i].Setup(item, i, OpenDetail);
                    return;
                }
            }
            Debug.LogWarning("InventoryBarUI: ช่องเต็มแล้ว ใส่ช่องเพิ่ม");
            return;
        }

        // แบบ B: สร้างช่องใหม่
        if (slotPrefab == null || container == null) return;
        InventorySlotUI slot = Instantiate(slotPrefab, container);
        slot.Setup(item, container.childCount - 1, OpenDetail);
    }

    void OpenDetail(ItemData item)
    {
        if (ItemDetailView.Instance != null && !GameState.GameOver)
            ItemDetailView.Instance.Show(item);
    }

    void Update()
    {
        if (GameState.Cutscene || GameState.GameOver || ItemDetailView.IsOpen || inventory == null) return;

        if (Input.GetKeyDown(browseKey))
        {
            // ปลดล็อกถ้าล็อกอยู่จาก Pause/อย่างอื่นไม่ได้ จึงเปิดได้เฉพาะตอนไม่ถูกล็อกโดยตัวอื่น
            if (!browsing && !PlayerControl.IsLocked) { browsing = true; PlayerControl.Lock(true); }
            else if (browsing) { browsing = false; PlayerControl.Unlock(true); }
        }

        for (int i = 0; i < 9 && i < inventory.Items.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && (browsing || !PlayerControl.IsLocked))
            {
                OpenDetail(inventory.Items[i]);
                break;
            }
        }
    }
}
