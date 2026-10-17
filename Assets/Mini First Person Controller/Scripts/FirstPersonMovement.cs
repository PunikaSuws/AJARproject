using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    Rigidbody rigidbody;
    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();



    void Awake()
    {
        // Get the rigidbody on this.
        rigidbody = GetComponent<Rigidbody>();
    }

    [Header("Step Up")]
    public float stepHeight = 0.4f;     // ความสูงสูงสุดที่ก้าวได้
    public float stepSmooth = 0.1f;     // ระยะที่ยกต่อเฟรมฟิสิกส์
    public float lowerRayDistance = 0.6f; // ระยะตรวจด้านหน้า (ปรับตามรัศมีแคปซูล)
    void FixedUpdate()
    {
        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey);

        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Get targetVelocity from input.
        Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Apply movement.
        rigidbody.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.y);
        StepClimb();
    }
    void StepClimb()
    {
        Vector3 moveDir = new Vector3(rigidbody.linearVelocity.x, 0f, rigidbody.linearVelocity.z);
        if (moveDir.sqrMagnitude < 0.01f) return;
        moveDir.Normalize();

        Vector3 footPos = transform.position + Vector3.up * 0.05f;
        Vector3 stepPos = transform.position + Vector3.up * stepHeight;

        // เส้นล่างชนอะไรสักอย่างที่เท้า
        if (Physics.Raycast(footPos, moveDir, out RaycastHit lowerHit, lowerRayDistance))
        {
            // กันไม่ให้ปีนผนังชัน: ต้องเป็นพื้นผิวแนวตั้งจริงๆ
            if (Mathf.Abs(lowerHit.normal.y) > 0.3f) return;

            // เส้นบนต้องโล่ง แปลว่าเป็นขั้น ไม่ใช่กำแพง
            if (!Physics.Raycast(stepPos, moveDir, lowerRayDistance + 0.1f))
            {
                rigidbody.position += Vector3.up * stepSmooth;
            }
        }
    }
}