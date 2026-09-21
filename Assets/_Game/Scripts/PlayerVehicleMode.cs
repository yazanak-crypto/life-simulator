using UnityEngine;
using UnityEngine.InputSystem;

namespace LifeSimulator
{
    // Exit after PlayerInteraction's LateUpdate so the exit press cannot re-enter.
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(PlayerInteraction), typeof(ThirdPersonPlayer), typeof(CharacterController))]
    public sealed class PlayerVehicleMode : MonoBehaviour
    {
        [SerializeField] private ThirdPersonCamera viewCamera;
        [SerializeField] private InteractionUI interactionUI;
        private PlayerInteraction interaction;
        private ThirdPersonPlayer movement;
        private CharacterController character;
        private Renderer[] renderers;
        private Collider[] colliders;
        private bool[] rendererStates;
        private bool[] colliderStates;
        private InputAction drive;
        private InputAction exit;
        private readonly ExitPrompt exitPrompt = new ExitPrompt();
        private Vector3 entryPosition;
        private Quaternion entryRotation;
        private bool restoring;
        private int enterFrame;
        private float nextEntryTime;
        public VehicleSeat CurrentVehicle { get; private set; }
        public bool IsDriving => CurrentVehicle != null;

        private void Awake()
        {
            interaction = GetComponent<PlayerInteraction>();
            movement = GetComponent<ThirdPersonPlayer>();
            character = GetComponent<CharacterController>();
            drive = new InputAction("Drive", InputActionType.Value);
            drive.AddCompositeBinding("2DVector(mode=1)").With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            exit = new InputAction("Exit Vehicle", InputActionType.Button, "<Keyboard>/e");
        }

        public bool TryEnter(VehicleSeat seat)
        {
            if (!isActiveAndEnabled || IsDriving || restoring || Time.unscaledTime < nextEntryTime
                || seat == null || !interaction.isActiveAndEnabled || !movement.isActiveAndEnabled
                || !character.enabled || viewCamera == null || !viewCamera.isActiveAndEnabled
                || interactionUI == null || (transform.position - seat.transform.position).sqrMagnitude > 25f)
                return false;
            // A carried box must never become an invisible passenger. Being clocked in does not
            // block driving: leaving work early is the player's decision, and it costs them wages.
            if (TryGetComponent(out PlayerCarry carry) && carry.IsCarrying)
            {
                interaction.ShowFeedback("Put the box down before driving");
                return false;
            }
            if (!seat.Claim(this)) return false;
            CurrentVehicle = seat;
            entryPosition = transform.position;
            entryRotation = transform.rotation;
            enterFrame = Time.frameCount;
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            rendererStates = new bool[renderers.Length];
            colliderStates = new bool[colliders.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                rendererStates[i] = renderers[i].enabled;
                renderers[i].enabled = false;
            }
            for (int i = 0; i < colliders.Length; i++)
            {
                colliderStates[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }
            movement.enabled = false;
            interaction.enabled = false;
            viewCamera.BeginFollowOverride(seat.transform, new Vector3(0f, 2.2f, 0f), 7f, 180f);
            drive.Enable();
            exit.Enable();
            interaction.ShowFeedback("W/S — Accelerate / Brake / Reverse   A/D — Steer   E — Exit Vehicle");
            return true;
        }

        private void Update()
        {
            if (!IsDriving) return;
            if (!CurrentVehicle.isActiveAndEnabled || !CurrentVehicle.Motor.isActiveAndEnabled
                || CurrentVehicle.Ownership == null || !CurrentVehicle.Ownership.IsOwnedBy(interaction))
            {
                RestoreOnFoot();
                return;
            }
            bool focused = Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
            CurrentVehicle.Motor.SetInput(focused ? drive.ReadValue<Vector2>() : Vector2.zero);
        }

        private void LateUpdate()
        {
            if (!IsDriving) return;
            transform.SetPositionAndRotation(CurrentVehicle.transform.position, CurrentVehicle.transform.rotation);
            if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked)
            {
                // Existing generic UI can display any IInteractable, including this exit action.
                interactionUI.SetTarget(exitPrompt);
                if (Time.frameCount > enterFrame && exit.WasPressedThisFrame()) TryExit();
            }
            else interactionUI.Clear();
        }

        private sealed class ExitPrompt : IInteractable
        {
            public string Prompt => "Exit Vehicle";
            public void Interact(PlayerInteraction player) { }
        }

        public bool TryExit()
        {
            if (!IsDriving || restoring) return false;
            // TEMPORARY until player injury exists; RestoreOnFoot deliberately skips this gate
            // because cleanup must never strand the player inside a disabled vehicle.
            if (!CurrentVehicle.IsSafeToExit)
            {
                interaction.ShowFeedback("Too fast to exit");
                return false;
            }
            if (!CurrentVehicle.FindExit(character, out Vector3 position))
            {
                interaction.ShowFeedback("Exit blocked — move the car to a clear area");
                return false;
            }
            Restore(position, Quaternion.Euler(0f, CurrentVehicle.transform.eulerAngles.y, 0f));
            return true;
        }

        // Disabling/destroying either participant must not leave the player hidden or input stranded.
        internal void RestoreOnFoot()
        {
            if (!IsDriving || restoring) return;
            Vector3 position = CurrentVehicle.FindExit(character, out Vector3 nearby) ? nearby : entryPosition;
            Restore(position, entryRotation);
        }

        private void Restore(Vector3 position, Quaternion rotation)
        {
            restoring = true;
            VehicleSeat seat = CurrentVehicle;
            CurrentVehicle = null;
            seat.Release(this);
            drive.Disable();
            exit.Disable();
            transform.SetPositionAndRotation(position, rotation);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = rendererStates[i];
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
            if (viewCamera != null) viewCamera.EndFollowOverride();
            movement.enabled = true;
            interaction.enabled = true;
            interactionUI.Clear();
            nextEntryTime = Time.unscaledTime + 0.25f;
            restoring = false;
        }

        private void OnDisable() => RestoreOnFoot();
        private void OnDestroy()
        {
            drive?.Dispose();
            exit?.Dispose();
        }
    }
}
