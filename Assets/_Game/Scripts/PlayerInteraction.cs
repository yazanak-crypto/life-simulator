using UnityEngine;
using UnityEngine.InputSystem;

namespace LifeSimulator
{
    // Resolve the target after the existing camera has finished its LateUpdate.
    [DefaultExecutionOrder(100)]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private InteractionUI interactionUI;
        [SerializeField, Min(0.1f)] private float reach = 2.5f;
        [SerializeField] private Vector3 reachOriginOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private LayerMask detectionMask = Physics.DefaultRaycastLayers;

        private InputAction interact;
        public IInteractable CurrentTarget { get; private set; }

        private void Awake()
        {
            interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            if (viewCamera == null || interactionUI == null)
            {
                Debug.LogError("Assign a camera and interaction UI to PlayerInteraction.", this);
                enabled = false;
            }
        }

        private void OnEnable() => interact?.Enable();

        private void OnDisable()
        {
            interact?.Disable();
            CurrentTarget = null;
            if (interactionUI != null)
                interactionUI.Clear();
        }

        private void OnDestroy() => interact?.Dispose();

        private void LateUpdate()
        {
            if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked)
            {
                CurrentTarget = null;
                interactionUI.Clear();
                return;
            }

            // Re-evaluate on the press frame, so a stale prompt cannot activate a distant target.
            CurrentTarget = FindTarget();
            interactionUI.SetTarget(CurrentTarget);
            if (CurrentTarget != null && interact.WasPressedThisFrame())
                CurrentTarget.Interact(this);
        }

        private IInteractable FindTarget()
        {
            Vector3 origin = transform.position + reachOriginOffset;
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // The third-person camera is behind the player; its offset must not consume player reach.
            float rayLength = Vector3.Distance(ray.origin, origin) + reach;
            if (!Physics.Raycast(ray, out RaycastHit hit, rayLength, detectionMask,
                    QueryTriggerInteraction.Ignore))
                return null;

            IInteractable candidate = hit.collider.GetComponentInParent<IInteractable>();
            if (!(candidate is Behaviour behaviour) || !behaviour.isActiveAndEnabled
                || Vector3.Distance(origin, hit.point) > reach)
                return null;

            // Seeing an object around a corner with the camera does not grant reach through a wall.
            if (Physics.Linecast(origin, hit.point, out RaycastHit obstruction, detectionMask,
                    QueryTriggerInteraction.Ignore)
                && obstruction.collider.GetComponentInParent<IInteractable>() != candidate)
                return null;

            return candidate;
        }

        public void ShowFeedback(string message) => interactionUI.ShowFeedback(message);
    }
}
