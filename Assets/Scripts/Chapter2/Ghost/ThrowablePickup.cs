using UnityEngine;

/// <summary>
/// ของที่หยิบไว้ขว้างผี (E) ใส่ที่วัตถุในฉาก (Collider ไม่ใช่ Trigger)
/// หยิบได้เฉพาะตอนผีเริ่มไล่ (ThrowController ถูกเปิดแล้ว)
/// Throw This Object = ติ๊ก: ของหายจากที่เดิม แล้วตอนขว้างจะโยน "ชิ้นเดิมนั้น" ออกไป (ไม่ต้องมี Prefab)
/// Throw This Object = ไม่ติ๊ก: ได้กระสุนตามจำนวน แล้วของถูกลบ (ใช้คู่กับ Projectile Prefab)
/// </summary>
public class ThrowablePickup : MonoBehaviour, IInteractable
{
    [SerializeField] bool throwThisObject = true;
    [SerializeField] int ammoAmount = 1;
    [SerializeField] string prompt = "[E] หยิบ";
    [SerializeField] AudioClip pickupSound;

    public string Prompt { get { return prompt; } }

    public bool CanInteract
    {
        get { return ThrowController.Instance != null && ThrowController.Instance.enabled; }
    }

    public void Interact()
    {
        if (!CanInteract) return;
        if (pickupSound != null) GameAudio.PlaySfx(pickupSound, transform.position);

        if (throwThisObject)
        {
            ThrowController.Instance.AddHeld(gameObject);   // ซ่อนไว้ ไม่ลบ
        }
        else
        {
            ThrowController.Instance.AddAmmo(ammoAmount);
            Destroy(gameObject);
        }
    }
}