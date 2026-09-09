using UnityEngine;
using UnityEngine.InputSystem;

public class FishingThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5.5f, -8.5f);
    public float lookHeight = 1.35f;
    public float followSmooth = 10f;
    public float mouseSensitivity = 2.5f;
    public float minPitch = -25f;
    public float maxPitch = 65f;

    private float yaw;
    private float pitch = 18f;
    private bool cursorLocked;

    private void Start()
    {
        var angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        if (!MobileTouchControls.IsMobile)
            SetCursorLock(true);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (MobileTouchControls.IsMobile)
        {
            // Touch drag for camera orbit
            var delta = MobileTouchControls.CameraDelta;
            if (delta.sqrMagnitude > 0.1f)
            {
                yaw += delta.x * 0.15f;
                pitch = Mathf.Clamp(pitch - delta.y * 0.15f, minPitch, maxPitch);
            }
            // Keep cursor unlocked on mobile
            if (Cursor.lockState != CursorLockMode.None)
                SetCursorLock(false);
        }
        else
        {
            // Desktop: mouse delta with cursor lock
            if (Mouse.current != null && cursorLocked)
            {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * mouseSensitivity * 0.08f;
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity * 0.08f, minPitch, maxPitch);
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLock(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursorLock(true);
        }

        var orbit = Quaternion.Euler(pitch, yaw, 0f);
        var desiredPosition = target.position + orbit * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmooth * Time.deltaTime);

        var lookTarget = target.position + Vector3.up * lookHeight;
        var direction = lookTarget - transform.position;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void SetCursorLock(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
