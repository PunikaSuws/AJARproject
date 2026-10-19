using TMPro;
using UnityEngine;

/// <summary>ขว้างของใส่ผีด้วยคลิกซ้าย ใส่ที่ผู้เล่น (Director จะเปิดใช้ตอนผีเริ่มไล่)</summary>
public class ThrowController : MonoBehaviour
{
    public static ThrowController Instance { get; private set; }

    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Camera playerCamera;
    [SerializeField] float throwSpeed = 16f;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] int startAmmo = 0;
    [SerializeField] TMP_Text ammoText;
    [SerializeField] GameObject ammoRoot;
    [SerializeField] AudioClip throwSound;

    int ammo;
    float nextThrow;

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

    void Update()
    {
        if (GameState.Cutscene || GameState.GameOver || PlayerControl.IsLocked) return;
        if (ammo <= 0 || Time.time < nextThrow) return;

        if (Input.GetMouseButtonDown(0))
            Throw();
    }

    void Throw()
    {
        if (projectilePrefab == null || playerCamera == null) return;

        nextThrow = Time.time + cooldown;
        ammo--;
        RefreshUI();

        Transform cam = playerCamera.transform;
        Projectile p = Instantiate(projectilePrefab, cam.position + cam.forward * 0.6f, cam.rotation);
        Rigidbody rb = p.GetComponent<Rigidbody>();
        rb.linearVelocity = cam.forward * throwSpeed;
        rb.AddTorque(Random.onUnitSphere * 5f, ForceMode.VelocityChange);

        // ไม่ให้ชนตัวผู้เล่นเอง
        Collider pc = p.GetComponent<Collider>();
        foreach (Collider c in GetComponentsInChildren<Collider>())
            if (pc != null) Physics.IgnoreCollision(pc, c);

        if (throwSound != null) GameAudio.PlaySfx(throwSound, cam.position);
    }

    void RefreshUI()
    {
        if (ammoRoot != null) ammoRoot.SetActive(enabled);
        if (ammoText != null) ammoText.text = "ขว้าง (คลิกซ้าย) x" + ammo;
    }
}
