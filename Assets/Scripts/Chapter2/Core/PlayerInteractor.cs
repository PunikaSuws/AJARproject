using TMPro;
using UnityEngine;

/// <summary>ยิง Ray จากกลางกล้อง หา IInteractable แล้วกด E เพื่อใช้งาน ใส่ไว้ที่ผู้เล่น</summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] float range = 3f;
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] LayerMask hitMask = ~0;

    [Header("UI")]
    [SerializeField] GameObject promptRoot;
    [SerializeField] TMP_Text promptText;

    IInteractable current;

    void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        SetPrompt(null);
    }

    void Update()
    {
        if (GameState.Cutscene || GameState.GameOver || PlayerControl.IsLocked || playerCamera == null)
        {
            current = null;
            SetPrompt(null);
            return;
        }

        current = null;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
            if (target != null && target.CanInteract)
                current = target;
        }

        SetPrompt(current != null ? current.Prompt : null);

        if (current != null && Input.GetKeyDown(interactKey))
            current.Interact();
    }

    void SetPrompt(string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        if (promptRoot != null) promptRoot.SetActive(show);
        if (promptText != null && show) promptText.text = text;
    }
}
