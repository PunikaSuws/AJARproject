using UnityEngine;

/// <summary>ข้อมูลไอเท็ม 1 ชิ้น สร้างจาก Assets > Create > Game > Item Data</summary>
[CreateAssetMenu(fileName = "Item_", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string id;                    // เช่น "hairpin", "comb", "photo1", "photo2", "book", "pot"
    public string displayName;           // เช่น "ปิ่นปักผม"
    public Sprite icon;                  // รูปในแถบ Inventory
    public GameObject modelPrefab;       // โมเดล 3D ที่หมุนดูได้ในหน้าต่างรายละเอียด
    [TextArea(3, 10)] public string description;   // ข้อความรายละเอียด (สมุด = ข้อความที่บอกให้ไปเอาหม้อดิน)
    public AudioClip pickupSound;        // เสียงตอนเก็บ (ไม่ใส่ก็ได้)
}
