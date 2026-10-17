
using System;
using UnityEngine;

public class HideController : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float hiddenCameraLocalY = 0.4f;

    [Tooltip("ลากสคริปต์เดินและสคริปต์หมุนกล้องมาใส่")]
    [SerializeField] private Behaviour[] controlsToDisable;

    [Header("UI")]
    [SerializeField] private GameObject promptObject;
    [SerializeField] private TMPro.TMP_Text promptText;

    private Renderer[] playerRenderers;
    private bool[] playerRendererStates;
    private Collider[] playerColliders;
    private bool[] playerColliderStates;
    private Rigidbody playerRigidbody;
    private HideSpot currentSpot;
    private HideSpot activeSpot;

    private bool isHiding;
    private bool hidingEnabled = true;
    private bool hasSavedPosition;
    private bool wasRigidbodyKinematic;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalCameraLocalPosition;
    private Vector3 originalLinearVelocity;
    private Vector3 originalAngularVelocity;
    private bool hasSavedCameraPosition;

    public event Action<HideController> ExitedHide;

    private void Awake()
    {
        playerRenderers = GetComponentsInChildren<Renderer>(true);
        playerRendererStates = new bool[playerRenderers.Length];
        playerColliders = GetComponentsInChildren<Collider>(true);
        playerColliderStates = new bool[playerColliders.Length];
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (isHiding)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ExitHide();
            }

            return;
        }

        if (currentSpot != null &&
            Input.GetKeyDown(KeyCode.E))
        {
            EnterHide();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HideSpot spot = other.GetComponentInParent<HideSpot>();

        if (spot == null || isHiding || !hidingEnabled)
            return;

        currentSpot = spot;
        ShowPrompt(spot.HideMessage);
    }

    private void OnTriggerExit(Collider other)
    {
        HideSpot spot = other.GetComponentInParent<HideSpot>();

        if (spot == null || spot != currentSpot || isHiding)
            return;

        currentSpot = null;
        HidePrompt();
    }

    private void EnterHide()
    {
        if (!hidingEnabled ||
            currentSpot == null ||
            currentSpot.HidePosition == null)
        {
            if (!hidingEnabled)
                return;

            Debug.LogWarning(
                "HideSpot or HidePosition is missing."
            );
            return;
        }

        activeSpot = currentSpot;

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        hasSavedPosition = true;

        if (playerCamera != null)
        {
            originalCameraLocalPosition = playerCamera.localPosition;
            hasSavedCameraPosition = true;
        }

        HidePrompt();

        if (playerRigidbody != null)
        {
            wasRigidbodyKinematic = playerRigidbody.isKinematic;
            originalLinearVelocity = playerRigidbody.linearVelocity;
            originalAngularVelocity = playerRigidbody.angularVelocity;
            playerRigidbody.isKinematic = true;
        }

        SetPlayerCollidersEnabled(false);

        transform.SetPositionAndRotation(
            activeSpot.HidePosition.position,
            activeSpot.HidePosition.rotation
        );

        if (playerCamera != null)
        {
            Vector3 hiddenCameraPosition = playerCamera.localPosition;
            hiddenCameraPosition.y = hiddenCameraLocalY;
            playerCamera.localPosition = hiddenCameraPosition;
        }

        SetControlsEnabled(false);
        SetPlayerRenderersEnabled(false);

        isHiding = true;
        ShowPrompt(activeSpot.ExitMessage);
    }

    public void SetHidingEnabled(bool enabled)
    {
        hidingEnabled = enabled;
        if (!hidingEnabled && !isHiding)
        {
            currentSpot = null;
            HidePrompt();
        }
    }

    private void ExitHide()
    {
        if (!hasSavedPosition)
            return;

        transform.SetPositionAndRotation(
            originalPosition,
            originalRotation
        );

        RestoreCameraPosition();

        SetControlsEnabled(true);
        SetPlayerRenderersEnabled(true);
        RestorePlayerPhysics();

        currentSpot = activeSpot;
        isHiding = false;
        hasSavedPosition = false;
        activeSpot = null;

        if (currentSpot != null)
            ShowPrompt(currentSpot.HideMessage);
        else
            HidePrompt();

        ExitedHide?.Invoke(this);
    }

    private void SetControlsEnabled(bool isEnabled)
    {
        if (controlsToDisable == null)
            return;

        foreach (Behaviour control in controlsToDisable)
        {
            if (control != null)
            {
                control.enabled = isEnabled;
            }
        }
    }

    private void ShowPrompt(string message)
    {
        if (promptObject != null)
            promptObject.SetActive(true);

        if (promptText != null)
            promptText.text = message;
    }

    private void HidePrompt()
    {
        if (promptObject != null)
            promptObject.SetActive(false);
    }

    private void SetPlayerRenderersEnabled(bool isEnabled)
    {
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            Renderer playerRenderer = playerRenderers[i];
            if (playerRenderer == null)
                continue;

            if (isEnabled)
                playerRenderer.enabled = playerRendererStates[i];
            else
            {
                playerRendererStates[i] = playerRenderer.enabled;
                playerRenderer.enabled = false;
            }
        }
    }

    private void SetPlayerCollidersEnabled(bool isEnabled)
    {
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];
            if (playerCollider == null)
                continue;

            if (isEnabled)
                playerCollider.enabled = playerColliderStates[i];
            else
            {
                playerColliderStates[i] = playerCollider.enabled;
                playerCollider.enabled = false;
            }
        }
    }

    private void RestorePlayerPhysics()
    {
        SetPlayerCollidersEnabled(true);

        if (playerRigidbody == null)
            return;

        playerRigidbody.isKinematic = wasRigidbodyKinematic;
        if (!wasRigidbodyKinematic)
        {
            playerRigidbody.linearVelocity = originalLinearVelocity;
            playerRigidbody.angularVelocity = originalAngularVelocity;
        }
    }

    private void RestoreCameraPosition()
    {
        if (hasSavedCameraPosition && playerCamera != null)
            playerCamera.localPosition = originalCameraLocalPosition;

        hasSavedCameraPosition = false;
    }

    private void OnDisable()
    {
        if (isHiding && hasSavedPosition)
        {
            transform.SetPositionAndRotation(
                originalPosition,
                originalRotation
            );

            RestoreCameraPosition();

            SetControlsEnabled(true);
            SetPlayerRenderersEnabled(true);
            RestorePlayerPhysics();
        }

        isHiding = false;
        hasSavedPosition = false;
        currentSpot = null;
        activeSpot = null;

        HidePrompt();
    }
}
