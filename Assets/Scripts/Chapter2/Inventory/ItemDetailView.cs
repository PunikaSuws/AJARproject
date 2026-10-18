using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หน้าต่างรายละเอียดไอเท็ม: แสดงโมเดล 3D ผ่าน RenderTexture ลากเมาส์ซ้ายหมุนได้ 360 องศา
/// ล้อเมาส์ซูมเข้า-ออก ปิดด้วยปุ่ม Close หรือกด E / Enter
/// ต้องเปิด Canvas เป็น Screen Space - Overlay
/// </summary>
public class ItemDetailView : MonoBehaviour
{
    public static ItemDetailView Instance { get; private set; }
    public static bool IsOpen { get { return Instance != null && Instance.open; } }

    /// <summary>ยิงทุกครั้งที่หน้าต่างปิด (ส่งไอเท็มที่ปิด)</summary>
    public static event Action<ItemData> Closed;

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] RawImage modelImage;
    [SerializeField] Button closeButton;

    [Header("Viewer")]
    [SerializeField] int textureSize = 768;
    [SerializeField] float modelSize = 1f;          // ขนาดโมเดลหลังย่อ/ขยายให้พอดี
    [SerializeField] float cameraDistance = 2.2f;
    [SerializeField] float rotateSpeed = 6f;
    [SerializeField] float idleSpinSpeed = 20f;     // องศา/วินาที ตอนไม่ได้ลาก
    [SerializeField] Color backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
    [SerializeField] KeyCode closeKey = KeyCode.E;

    Camera viewCamera;
    Light viewLight;
    Transform pivot;
    Transform modelRoot;
    RenderTexture rt;

    bool open;
    ItemData currentItem;
    Action onClosed;
    float previousTimeScale = 1f;
    float distance;
    Vector2 spinVelocity;
    bool dragging;

    static readonly Vector3 ViewPosition = new Vector3(0f, -2000f, 0f);

    void Awake()
    {
        Instance = this;

        rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
        rt.Create();
        if (modelImage != null) modelImage.texture = rt;

        pivot = new GameObject("ItemViewPivot").transform;
        pivot.position = ViewPosition;

        GameObject camGo = new GameObject("ItemViewCamera");
        viewCamera = camGo.AddComponent<Camera>();
        viewCamera.clearFlags = CameraClearFlags.SolidColor;
        viewCamera.backgroundColor = backgroundColor;
        viewCamera.fieldOfView = 30f;
        viewCamera.nearClipPlane = 0.05f;
        viewCamera.farClipPlane = 20f;
        viewCamera.depth = -50f;
        viewCamera.targetTexture = rt;
        viewCamera.allowHDR = false;
        viewCamera.allowMSAA = false;
        viewCamera.enabled = false;

        GameObject lightGo = new GameObject("ItemViewLight");
        viewLight = lightGo.AddComponent<Light>();
        viewLight.type = LightType.Point;
        viewLight.range = 12f;
        viewLight.intensity = 1.6f;
        viewLight.enabled = false;

        distance = cameraDistance;
        PlaceCamera();

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panel != null) panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (rt != null) rt.Release();
        if (pivot != null) Destroy(pivot.gameObject);
        if (viewCamera != null) Destroy(viewCamera.gameObject);
        if (viewLight != null) Destroy(viewLight.gameObject);
    }

    void PlaceCamera()
    {
        viewCamera.transform.position = ViewPosition + new Vector3(0f, 0.15f, -distance);
        viewCamera.transform.LookAt(ViewPosition);
        viewLight.transform.position = ViewPosition + new Vector3(1.2f, 1.5f, -distance * 0.8f);
    }

    public void Show(ItemData item, Action onClosedCallback = null)
    {
        if (item == null || open) { if (onClosedCallback != null && !open) onClosedCallback(); return; }

        open = true;
        currentItem = item;
        onClosed = onClosedCallback;

        if (titleText != null) titleText.text = item.displayName;
        if (descriptionText != null) descriptionText.text = item.description;

        BuildModel(item);
        distance = cameraDistance;
        PlaceCamera();
        pivot.rotation = Quaternion.Euler(10f, 25f, 0f);
        spinVelocity = Vector2.zero;

        viewCamera.enabled = true;
        viewLight.enabled = true;
        if (panel != null) panel.SetActive(true);

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        PlayerControl.Lock(true);
    }

    void BuildModel(ItemData item)
    {
        if (modelRoot != null) Destroy(modelRoot.gameObject);
        modelRoot = null;
        if (item.modelPrefab == null) return;

        GameObject go = Instantiate(item.modelPrefab, pivot);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.SetActive(true);
        modelRoot = go.transform;

        // ปิดสิ่งที่อาจรบกวน (สคริปต์ / ฟิสิกส์ / Collider)
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;

        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        float maxExtent = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maxExtent > 0.0001f)
            go.transform.localScale *= modelSize / maxExtent;

        // คำนวณใหม่หลังย่อ แล้วเลื่อนให้จุดกึ่งกลางอยู่ที่ pivot (หมุนรอบศูนย์กลางพอดี)
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        go.transform.position += pivot.position - b.center;
    }

    void Update()
    {
        if (!open) return;

        if (Input.GetKeyDown(closeKey) || Input.GetKeyDown(KeyCode.Return))
        {
            Close();
            return;
        }

        bool overModel = modelImage != null &&
            RectTransformUtility.RectangleContainsScreenPoint(modelImage.rectTransform, Input.mousePosition, null);

        if (Input.GetMouseButtonDown(0) && overModel) dragging = true;
        if (Input.GetMouseButtonUp(0)) dragging = false;

        float dt = Time.unscaledDeltaTime;

        if (dragging)
        {
            float dx = Input.GetAxisRaw("Mouse X");
            float dy = Input.GetAxisRaw("Mouse Y");
            spinVelocity = new Vector2(dx, dy) * rotateSpeed;
            ApplyRotation(spinVelocity);
        }
        else
        {
            // ค่อย ๆ หยุดหมุนตามแรงเฉื่อย แล้วหมุนช้า ๆ เอง
            spinVelocity = Vector2.Lerp(spinVelocity, Vector2.zero, 4f * dt);
            ApplyRotation(spinVelocity);
            pivot.Rotate(Vector3.up, -idleSpinSpeed * dt, Space.World);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f && overModel)
        {
            distance = Mathf.Clamp(distance - scroll * 0.2f, cameraDistance * 0.5f, cameraDistance * 1.8f);
            PlaceCamera();
        }
    }

    void ApplyRotation(Vector2 v)
    {
        // หมุนรอบแกนโลก (ซ้าย-ขวา) และแกนกล้อง (ขึ้น-ลง) ได้ครบ 360 องศา
        pivot.Rotate(Vector3.up, -v.x, Space.World);
        pivot.Rotate(viewCamera.transform.right, v.y, Space.World);
    }

    public void Close()
    {
        if (!open) return;

        open = false;
        dragging = false;

        viewCamera.enabled = false;
        viewLight.enabled = false;
        if (panel != null) panel.SetActive(false);
        if (modelRoot != null) Destroy(modelRoot.gameObject);
        modelRoot = null;

        Time.timeScale = previousTimeScale;
        PlayerControl.Unlock(true);

        ItemData closedItem = currentItem;
        Action cb = onClosed;
        currentItem = null;
        onClosed = null;

        if (cb != null) cb();
        if (Closed != null) Closed(closedItem);
    }
}
