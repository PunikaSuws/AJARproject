using UnityEngine;

/// <summary>ของที่ถูกขว้าง (Prefab: Rigidbody + Collider + สคริปต์นี้) โดนผี = Stun + นับ Task</summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] string progressTaskId = "throw_ghost";
    [SerializeField] float lifetime = 6f;
    [SerializeField] AudioClip hitGhostSound;
    [SerializeField] AudioClip hitOtherSound;

    bool used;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (used) return;

        GhostController ghost = collision.collider.GetComponentInParent<GhostController>();
        if (ghost != null)
        {
            used = true;
            if (ghost.Stun())
            {
                if (TaskManager.Instance != null && !string.IsNullOrEmpty(progressTaskId))
                    TaskManager.Instance.AddProgress(progressTaskId);
            }
            if (hitGhostSound != null) AudioSource.PlayClipAtPoint(hitGhostSound, transform.position);
            Destroy(gameObject);
        }
        else if (hitOtherSound != null && collision.relativeVelocity.magnitude > 2f)
        {
            AudioSource.PlayClipAtPoint(hitOtherSound, transform.position, 0.6f);
        }
    }
}
