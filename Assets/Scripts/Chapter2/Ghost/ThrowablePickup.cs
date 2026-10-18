using UnityEngine;

/// <summary>ของที่หยิบไว้ขว้างผี (E) ใส่ที่วัตถุในฉาก (Collider ไม่ใช่ Trigger)</summary>
public class ThrowablePickup : MonoBehaviour, IInteractable
{
    [SerializeField] int ammoAmount = 1;
    [SerializeField] string prompt = "[E] หยิบไว้ขว้างผี";
    [SerializeField] AudioClip pickupSound;

    public string Prompt { get { return prompt; } }

    public bool CanInteract
    {
        get { return ThrowController.Instance != null && ThrowController.Instance.enabled; }
    }

    public void Interact()
    {
        if (!CanInteract) return;
        ThrowController.Instance.AddAmmo(ammoAmount);
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        Destroy(gameObject);
    }
}
