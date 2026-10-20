using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ขว้างของใส่ผีด้วยคลิกซ้าย ใส่ที่ผู้เล่น (Director จะเปิดใช้ตอนผีเริ่มไล่)
/// รองรับ 2 แบบ ใช้ปนกันได้
///   1) ของในฉาก (ThrowablePickup, Throw This Object = ติ๊ก): เก็บแล้วของหายจากที่เดิม
///      ตอนขว้างจะเอา "ชิ้นเดิมนั้น" ออกมาโยน ไม่ต้องมี Prefab
///   2) Projectile Prefab: เก็บแล้วได้กระสุน ตอนขว้างสร้างสำเนาจาก Prefab/แม่แบบ
/// </summary>
public class ThrowController : MonoBehaviour
{
    public static ThrowController Instance { get; private set; }

    [Tooltip("ไม่จำเป็น ใช้เฉพาะแบบที่เก็บแล้วได้กระสุน (AddAmmo) จะใช้ Prefab หรือ GameObject ในฉากเป็นแม่แบบก็ได้")]
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Camera playerCamera;
    [SerializeField] float throwSpeed = 16f;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] int startAmmo = 0;
    [SerializeField] TMP_Text ammoText;
    [SerializeField] GameObject ammoRoot;
    [SerializeField] AudioClip throwSound;

    int ammo;                                           // กระสุนแบบ Prefab
    readonly Queue<GameObject> held = new Queue<GameObject>();   // ของในฉากที่เก็บไว้
    float nextThrow;

    int TotalAmmo { get { return ammo + held.Count; } }

    void Awake()
    {
        Instance = this;
        ammo = startAmmo;
        if (playerCamera == null) playerCamera = Camera.main;
        RefreshUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        RefreshUI();
    }

    void OnDisable()
    {
        if (ammoRoot != null) ammoRoot.SetActive(false);
    }

    public void AddAmmo(int amount)
    {
        ammo += amount;
        RefreshUI();
    }

    /// <summary>เก็บของในฉากชิ้นนี้ไว้ขว้าง (ของจะถูกซ่อนจากที่เดิม ไม่ถูกลบ)</summary>
    public void AddHeld(GameObject item)
    {
        if (item == null) return;
        held.Enqueue(item);
        item.SetActive(false);
        RefreshUI();
    }

    void Update()
    {
        if (GameState.Cutscene || GameState.GameOver || PlayerControl.IsLocked) return;
        if (TotalAmmo <= 0 || Time.time < nextThrow) return;

        if (Input.GetMouseButtonDown(0))
            Throw();
    }

    void Throw()
    {
        if (playerCamera == null) return;

        Transform cam = playerCamera.transform;
        Vector3 pos = cam.position + cam.forward * 0.6f;

        GameObject thrown = null;
        while (held.Count > 0 && thrown == null)
            thrown = held.Dequeue();          // ข้ามชิ้นที่ถูกลบไปแล้ว

        if (thrown != null)
        {
            // ชิ้นเดิมที่เก็บไว้ ออกมาจากกระเป๋า
            thrown.transform.SetParent(null);
            thrown.transform.SetPositionAndRotation(pos, cam.rotation);

            // ปิดสคริปต์เก็บของ กันเก็บซ้ำกลางอากาศ
            foreach (ThrowablePickup tp in thrown.GetComponentsInChildren<ThrowablePickup>(true))
                tp.enabled = false;

            thrown.SetActive(true);
        }
        else if (ammo > 0 && projectilePrefab != null)
        {
            ammo--;
            Projectile p = Instantiate(projectilePrefab, pos, cam.rotation);
            p.transform.SetParent(null);          // กรณีใช้ GameObject ในฉากเป็นแม่แบบ
            p.gameObject.SetActive(true);         // แม่แบบในฉากมักปิด Active ไว้
            thrown = p.gameObject;
        }
        else
        {
            return;
        }

        nextThrow = Time.time + cooldown;
        RefreshUI();

        // ต้องมี Rigidbody (ถ้าของในฉากไม่มี ใส่ให้) และต้องไม่ Kinematic
        Rigidbody rb = thrown.GetComponent<Rigidbody>();
        if (rb == null) rb = thrown.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        // ต้องมี Projectile (ถ้าไม่มี ใส่ให้) เพื่อให้โดนผีแล้ว Stun
        if (thrown.GetComponent<Projectile>() == null)
            thrown.AddComponent<Projectile>();

        rb.linearVelocity = cam.forward * throwSpeed;
        rb.AddTorque(Random.onUnitSphere * 5f, ForceMode.VelocityChange);

        // ไม่ให้ชนตัวผู้เล่นเอง
        Collider[] mine = GetComponentsInChildren<Collider>();
        foreach (Collider pc in thrown.GetComponentsInChildren<Collider>())
            foreach (Collider c in mine)
                Physics.IgnoreCollision(pc, c);

        if (throwSound != null) GameAudio.PlaySfx(throwSound, cam.position);
    }

    void RefreshUI()
    {
        if (ammoRoot != null) ammoRoot.SetActive(enabled);
        if (ammoText != null) ammoText.text = "ขว้าง (คลิกซ้าย) x" + TotalAmmo;
    }
}