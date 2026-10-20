using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จั๊มป์สแคร์ในเกม (ไม่ใช้วิดีโอ): ผีโผล่ตรงหน้ากล้อง กล้องหันหาหน้าผี ซูมกระชาก จอสั่น มีเสียง
/// ใส่ที่ Empty GameObject ใหม่ (เช่น GhostJumpscare) แล้วลากใส่ช่อง In Game Jumpscare ของ GameOverManager
/// </summary>
public class GhostJumpscare : MonoBehaviour
{
    [Header("ตำแหน่ง/เวลา")]
    [SerializeField] float scareDistance = 1.2f;      // ผีอยู่หน้ากล้องกี่เมตร
    [SerializeField] float scareDuration = 1.8f;      // ค้างฉากกี่วินาทีก่อนจอดำ

    [Header("เอฟเฟกต์กล้อง")]
    [SerializeField] float shakeAmount = 0.06f;       // ความแรงของจอสั่น
    [SerializeField] float fovPunch = 25f;            // ซูมกระชากกี่องศา
    [Tooltip("ไม่ใส่ก็ได้: Image เต็มจอสีแดง/ขาว ไว้แวบตอนผีโผล่ (ปิด Raycast Target)")]
    [SerializeField] Image flashImage;

    [Header("เสียง (ไม่ใส่ก็ได้)")]
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip sting;

    Transform cam;
    Camera camComp;
    Vector3 baseLocalPos;
    Quaternion baseRot;
    float baseFov;
    bool saved;

    /// <summary>ใช้เป็น Coroutine: yield return jumpscare.Play(); (ยังไม่จอดำ ผู้เรียกทำเอง)</summary>
    public IEnumerator Play()
    {
        GhostController ghost = FindFirstObjectByType<GhostController>();
        cam = PlayerControl.CameraTransform != null
            ? PlayerControl.CameraTransform
            : (Camera.main != null ? Camera.main.transform : null);
        camComp = cam != null ? cam.GetComponent<Camera>() : null;

        if (ghost != null && cam != null) ghost.PrepareJumpscare(cam, scareDistance);
        if (sfx != null && sting != null) sfx.PlayOneShot(sting);

        if (cam != null)
        {
            baseLocalPos = cam.localPosition;
            baseRot = cam.rotation;
            baseFov = camComp != null ? camComp.fieldOfView : 60f;
            saved = true;
        }

        for (float t = 0f; t < scareDuration; t += Time.unscaledDeltaTime)
        {
            float k = t / scareDuration;

            if (cam != null)
            {
                if (ghost != null)
                {
                    Vector3 dir = ghost.EyePosition - cam.position;
                    if (dir.sqrMagnitude > 0.001f)
                        cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(dir),
                            1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                }
                cam.localPosition = baseLocalPos + Random.insideUnitSphere * shakeAmount * (1f - k * 0.5f);
            }

            if (camComp != null)
                camComp.fieldOfView = Mathf.Lerp(baseFov, baseFov - fovPunch, Mathf.Clamp01(t / 0.12f));

            if (flashImage != null)
            {
                Color c = flashImage.color;
                c.a = Mathf.Clamp01(1f - t / 0.4f) * 0.7f;
                flashImage.color = c;
            }

            yield return null;
        }
    }

    /// <summary>คืนกล้องกลับค่าเดิม (เรียกหลังจอดำแล้ว)</summary>
    public void Restore()
    {
        if (saved && cam != null)
        {
            cam.localPosition = baseLocalPos;
            cam.rotation = baseRot;
            if (camComp != null) camComp.fieldOfView = baseFov;
        }
        saved = false;

        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
        }
    }
}
