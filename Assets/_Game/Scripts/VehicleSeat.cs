using UnityEngine;

namespace LifeSimulator
{
    [RequireComponent(typeof(ArcadeVehicle))]
    public sealed class VehicleSeat : MonoBehaviour, IInteractable
    {
        [SerializeField] private CarPurchase ownership;
        // TEMPORARY: bailing out of a moving car should hurt, but there is no player injury or
        // ragdoll system yet. Delete this field and IsSafeToExit once one exists.
        [SerializeField, Min(0f)] private float safeExitSpeed = 2f;
        public PlayerVehicleMode Driver { get; private set; }
        public CarPurchase Ownership => ownership;
        public ArcadeVehicle Motor { get; private set; }
        public string Prompt => ownership == null || ownership.Owner == null ? "Vehicle not purchased"
            : Driver != null ? "Vehicle occupied" : "Enter Vehicle";

        internal bool IsSafeToExit => Motor == null || Motor.GroundSpeed <= safeExitSpeed;

        private void Awake() => Motor = GetComponent<ArcadeVehicle>();

        public void Interact(PlayerInteraction player)
        {
            if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled) return;
            if (ownership == null || !ownership.IsOwnedBy(player))
            {
                player.ShowFeedback("Only the owner can drive this car");
                return;
            }
            if (Driver == null && player.TryGetComponent(out PlayerVehicleMode mode))
                mode.TryEnter(this);
        }

        internal bool Claim(PlayerVehicleMode driver)
        {
            if (!isActiveAndEnabled || Driver != null || driver == null || ownership == null
                || !ownership.IsOwnedBy(driver.GetComponent<PlayerInteraction>())
                || Motor == null || !Motor.isActiveAndEnabled) return false;
            Driver = driver;
            Motor.SetDriven(true);
            return true;
        }

        internal void Release(PlayerVehicleMode driver)
        {
            if (Driver != driver) return;
            Motor.SetDriven(false);
            Driver = null;
        }

        // Try both sides, including a rear-side alternative, never directly ahead.
        internal bool FindExit(CharacterController character, out Vector3 position)
        {
            Physics.SyncTransforms();
            foreach (float z in new[] { 0f, 1.5f })
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 candidate = transform.TransformPoint(new Vector3(side * 2.25f, 0f, z));
                if (!Physics.Raycast(candidate + Vector3.up * 1.5f, Vector3.down, out RaycastHit ground,
                        3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    || ground.normal.y < Mathf.Cos(character.slopeLimit * Mathf.Deg2Rad)
                    || ground.collider.GetComponentInParent<VehicleSeat>() == this) continue;
                candidate.y = ground.point.y + 0.1f;
                float radius = character.radius + 0.05f;
                Vector3 center = candidate + character.center;
                float half = Mathf.Max(0f, character.height * 0.5f - character.radius);
                if (Physics.CheckCapsule(center + Vector3.up * half, center - Vector3.up * half,
                        radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                // Don't teleport through a thin wall that lies between the door and candidate.
                Vector3 door = transform.TransformPoint(new Vector3(side * 1.2f, 0.8f, z));
                if (Physics.Linecast(door, candidate + Vector3.up * 0.8f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                position = candidate;
                return true;
            }
            position = default;
            return false;
        }

        private void OnDisable()
        {
            if (Driver != null) Driver.RestoreOnFoot();
        }
    }
}
