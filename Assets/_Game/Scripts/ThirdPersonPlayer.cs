using UnityEngine;
using UnityEngine.InputSystem;

namespace LifeSimulator
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonPlayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField] private float gravity = -25f;

        private CharacterController controller;
        private InputAction move;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            if (cameraTransform == null)
            {
                Debug.LogError("Assign the third-person camera to the player.", this);
                enabled = false;
            }
        }

        private void OnEnable() => move?.Enable();
        private void OnDisable() => move?.Disable();
        private void OnDestroy() => move?.Dispose();

        private void Update()
        {
            // Releasing the cursor also pauses movement input, but not gravity.
            Vector2 input = Cursor.lockState == CursorLockMode.Locked && Application.isFocused
                ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f)
                : Vector2.zero;
            Quaternion cameraYaw = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
            Vector3 direction = cameraYaw * new Vector3(input.x, 0f, input.y);

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
            }

            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;

            verticalSpeed = Mathf.Max(verticalSpeed + gravity * Time.deltaTime, -50f);
            CollisionFlags collisions = controller.Move(
                (direction * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

            if ((collisions & CollisionFlags.Below) != 0 && verticalSpeed < 0f)
                verticalSpeed = -2f;
            if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
                verticalSpeed = 0f;
        }
    }
}
