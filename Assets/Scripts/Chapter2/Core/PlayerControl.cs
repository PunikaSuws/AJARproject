using UnityEngine;

/// <summary>
/// ตัวกลางล็อก/ปลดล็อกผู้เล่น (เดิน + หมุนกล้อง + เคอร์เซอร์) แบบนับจำนวน
/// ใครจะล็อกก็เรียก PlayerControl.Lock() แล้วต้องเรียก Unlock() ให้ครบคู่กัน
/// ใส่ไว้ที่ GameObject ผู้เล่น (ตัวเดียวกับ FirstPersonMovement)
/// </summary>
public class PlayerControl : MonoBehaviour
{
    public static PlayerControl Instance { get; private set; }

    [SerializeField] FirstPersonLook look;
    [SerializeField] FirstPersonMovement movement;
    [SerializeField] Transform cameraTransform;

    int locks;
    int cursorLocks;

    public static bool IsLocked { get { return Instance != null && Instance.locks > 0; } }

    public static Transform PlayerTransform
    {
        get { return Instance != null && Instance.movement != null ? Instance.movement.transform : null; }
    }

    public static Transform CameraTransform
    {
        get { return Instance != null ? Instance.cameraTransform : null; }
    }

    void Awake()
    {
        Instance = this;

        if (movement == null) movement = GetComponentInChildren<FirstPersonMovement>();
        if (movement == null) movement = FindFirstObjectByType<FirstPersonMovement>();
        if (look == null) look = GetComponentInChildren<FirstPersonLook>();
        if (look == null) look = FindFirstObjectByType<FirstPersonLook>();

        if (cameraTransform == null)
        {
            Camera cam = look != null ? look.GetComponentInChildren<Camera>() : null;
            if (cam == null) cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    void Start()
    {
        Apply();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <param name="showCursor">true = แสดงเมาส์ให้คลิก UI ได้ (ต้อง Unlock(true) คู่กัน)</param>
    public static void Lock(bool showCursor = false)
    {
        if (Instance == null) return;
        Instance.locks++;
        if (showCursor) Instance.cursorLocks++;
        Instance.Apply();
    }

    public static void Unlock(bool showCursor = false)
    {
        if (Instance == null) return;
        Instance.locks = Mathf.Max(0, Instance.locks - 1);
        if (showCursor) Instance.cursorLocks = Mathf.Max(0, Instance.cursorLocks - 1);
        Instance.Apply();
    }

    /// <summary>ล้างการล็อกทั้งหมด (ใช้ตอนผู้เล่นฟื้นหลัง Game Over กันล็อกค้างจากผีพุ่ง ฯลฯ)</summary>
    public static void ResetLocks()
    {
        if (Instance == null) return;
        Instance.locks = 0;
        Instance.cursorLocks = 0;
        Instance.Apply();
    }

    void Apply()
    {
        bool locked = locks > 0;

        if (movement != null)
        {
            movement.enabled = !locked;
            if (locked)
            {
                Rigidbody rb = movement.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 v = rb.linearVelocity;
                    rb.linearVelocity = new Vector3(0f, v.y, 0f);
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        if (look != null) look.LookLocked = locked;

        if (cursorLocks > 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}