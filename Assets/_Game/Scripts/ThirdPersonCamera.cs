using UnityEngine;
using UnityEngine.InputSystem;

namespace LifeSimulator
{
    [DefaultExecutionOrder(-100)]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField, Min(0.5f)] private float distance = 5f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minimumPitch = -25f;
        [SerializeField] private float maximumPitch = 70f;
        [SerializeField] private LayerMask obstructionMask = 1; // Default layer; player uses Ignore Raycast.

        private float yaw;
        private float pitch = 20f;
        private InputAction look;

        private void Awake()
        {
            look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            if (target == null)
            {
                Debug.LogError("Assign the player target to the third-person camera.", this);
                enabled = false;
                return;
            }
            yaw = target.eulerAngles.y;
        }

        private void OnEnable()
        {
            look?.Enable();
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            look?.Disable();
            SetCursorLocked(false);
        }

        private void OnDestroy() => look?.Dispose();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                SetCursorLocked(false);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Update()
        {
            bool captureChanged = false;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
                captureChanged = true;
            }
            else if (Application.isFocused && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
                captureChanged = true;
            }

            if (!captureChanged && Application.isFocused && Cursor.lockState == CursorLockMode.Locked)
            {
                // Mouse delta is already a per-frame displacement: do not multiply by deltaTime.
                Vector2 delta = look.ReadValue<Vector2>() * mouseSensitivity;
                yaw = Mathf.Repeat(yaw + delta.x, 360f);
                pitch = Mathf.Clamp(pitch - delta.y, minimumPitch, maximumPitch);
            }

            // Apply yaw before the player's Update so camera-relative movement samples
            // this frame's orientation while WASD is held.
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void LateUpdate()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 pivot = target.position + pivotOffset;
            Vector3 backwards = rotation * Vector3.back;
            float cameraDistance = distance;
            if (Physics.SphereCast(pivot, 0.2f, backwards, out RaycastHit hit, distance,
                    obstructionMask, QueryTriggerInteraction.Ignore))
                cameraDistance = Mathf.Max(0f, hit.distance - 0.05f);

            transform.SetPositionAndRotation(pivot + backwards * cameraDistance, rotation);
        }
    }
}
