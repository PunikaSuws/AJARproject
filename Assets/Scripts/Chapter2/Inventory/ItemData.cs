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
    public Sprite detailCard;            // รูปการ์ดรายละเอียดสำเร็จรูป (มีข้อความในรูปแล้ว) ถ้าใส่ จะไม่ใช้ Title/Description แบบข้อความ
    [Header("ปรับโมเดลในหน้าต่างรายละเอียด (ต่อไอเท็ม)")]
    [Tooltip("คูณขนาดเพิ่มหลังจัดให้พอดีอัตโนมัติ 1 = ตามปกติ, 1.3 = ใหญ่ขึ้น 30%, 0.8 = เล็กลง")]
    public float viewScale = 1f;
    [Tooltip("มุมเริ่มต้นของโมเดล เช่น รูปที่นอนราบให้ลอง X = -90 เพื่อให้หันหน้าเข้าหากล้อง")]
    public Vector3 viewEuler = Vector3.zero;
    public AudioClip pickupSound;        // เสียงตอนเก็บ (ไม่ใส่ก็ได้)
}
