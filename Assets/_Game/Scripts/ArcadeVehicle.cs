using UnityEngine;

namespace LifeSimulator
{
    // Single-Rigidbody "arcade-sim" car. The engine, brakes, tyre grip and rolling resistance are
    // applied as accelerations, so PhysX keeps ownership of momentum, collision response and slopes.
    // Deliberately not a WheelCollider rig: no suspension, drivetrain or tyre-slip model.
    // Driver state is control only; it never alters the vehicle's physical state.
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class ArcadeVehicle : MonoBehaviour
    {
        [Header("Engine")]
        [SerializeField, Min(1f)] private float forwardSpeed = 14f;
        [SerializeField, Min(1f)] private float reverseSpeed = 5f;
        [SerializeField, Min(0.1f)] private float enginePower = 7f;
        [SerializeField, Min(0.1f)] private float brakingPower = 12f;

        [Header("Resistance")]
        [SerializeField, Min(0f)] private float rollingResistance = 1.5f;
        [SerializeField, Min(0f)] private float aerodynamicDrag = 0.012f;

        [Header("Tyres")]
        [SerializeField, Min(0f)] private float gripResponse = 12f;
        [SerializeField, Min(0f)] private float maxGripAcceleration = 14f;

        [Header("Steering")]
        [SerializeField, Min(0f)] private float maxYawRate = 75f;
        [SerializeField, Min(0f)] private float steeringResponse = 6f;
        [SerializeField, Min(0.01f)] private float steeringFadeSpeed = 3f;

        // Reverse shares the engine but pulls less hard, on top of the lower reverse speed cap.
        private const float ReverseEngineScale = 0.6f;
        private static readonly RaycastHit[] GroundHits = new RaycastHit[8];

        private Rigidbody body;
        private Vector2 input;

        public bool IsDriven { get; private set; }
        // The existing primitive's headlights face local -Z.
        public Vector3 Forward => -transform.forward;
        public float Speed => body == null ? 0f : Vector3.Dot(body.linearVelocity, Forward);
        public float GroundSpeed => body == null ? 0f
            : new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z).magnitude;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 900f;
            body.useGravity = true;
            // Upright by constraint: cheap stability in place of suspension. Yaw stays free so
            // collisions can still spin the car.
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            // Sweep tests, not speculative contacts: a speculative solver slows the body before it
            // touches, which understates Collision.relativeVelocity for a future damage system.
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.isKinematic = false;
            body.linearDamping = 0f;   // longitudinal resistance is modelled below instead
            body.angularDamping = 0.6f;
            // The hull stands in for four tyres, whose grip and rolling resistance are modelled
            // below. Box friction would fight that model and, at 900 kg, statically pin the car.
            GetComponent<BoxCollider>().sharedMaterial = new PhysicsMaterial("Arcade Vehicle")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            SetDriven(false);
        }

        // Control only. Momentum is deliberately untouched so a driver leaving a moving car does
        // not stop it, and an unoccupied car keeps rolling.
        internal void SetDriven(bool driven)
        {
            if (body == null) body = GetComponent<Rigidbody>();
            input = Vector2.zero;
            IsDriven = driven;
            body.WakeUp();
        }

        internal void SetInput(Vector2 value) => input = new Vector2(Mathf.Clamp(value.x, -1f, 1f), Mathf.Clamp(value.y, -1f, 1f));

        // Nearest upward-facing surface under the hull, and its normal so forces follow slopes.
        private bool TryGetGround(out Vector3 normal)
        {
            normal = Vector3.up;
            float nearest = float.MaxValue;
            int count = Physics.RaycastNonAlloc(body.position + Vector3.up * 0.35f, Vector3.down,
                GroundHits, 0.7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = GroundHits[i];
                if (hit.rigidbody == body || hit.normal.y <= 0.45f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                normal = hit.normal;
            }
            return nearest < float.MaxValue;
        }

        private void FixedUpdate()
        {
            // Airborne: no tyre contact, so gravity and existing momentum are the only actors.
            if (body.isKinematic || !TryGetGround(out Vector3 normal)) return;

            float step = Time.fixedDeltaTime;
            Vector3 forward = Vector3.ProjectOnPlane(Forward, normal).normalized;
            Vector3 right = Vector3.Cross(normal, forward);
            Vector3 velocity = body.linearVelocity;
            float along = Vector3.Dot(velocity, forward);
            float lateral = Vector3.Dot(velocity, right);

            // Tyre grip resists side slip, but is capped so hard cornering can break traction
            // rather than running the car on rails.
            float grip = Mathf.Clamp(-lateral * gripResponse, -maxGripAcceleration, maxGripAcceleration);
            body.AddForce(right * grip, ForceMode.Acceleration);

            // Rolling resistance and drag act whether or not anyone is driving, and can only ever
            // slow the car towards rest, never push it backwards.
            float resistance = rollingResistance + aerodynamicDrag * along * along;
            if (Mathf.Abs(along) > 0.001f)
            {
                float shed = Mathf.Min(resistance * step, Mathf.Abs(along));
                body.AddForce(forward * (-Mathf.Sign(along) * shed / step), ForceMode.Acceleration);
            }

            if (!IsDriven) return;

            float throttle = input.y;
            float drive = 0f;
            if (throttle > 0.01f)
                drive = along < forwardSpeed ? throttle * enginePower : 0f;
            else if (throttle < -0.01f)
                // Brake first; only once nearly stopped does S engage reverse.
                drive = along > 0.2f ? throttle * brakingPower
                    : along > -reverseSpeed ? throttle * enginePower * ReverseEngineScale : 0f;
            if (drive != 0f) body.AddForce(forward * drive, ForceMode.Acceleration);

            // Steering asks for a yaw rate and torques towards it, so the car turns in rather than
            // snapping, and a collision can still override it. Authority fades out near standstill.
            float authority = Mathf.Clamp01(Mathf.Abs(along) / steeringFadeSpeed);
            float wanted = input.x * maxYawRate * authority * Mathf.Sign(along);
            float current = body.angularVelocity.y * Mathf.Rad2Deg;
            body.AddTorque(Vector3.up * ((wanted - current) * Mathf.Deg2Rad * steeringResponse),
                ForceMode.Acceleration);
        }

        private void OnDisable() => SetDriven(false);
    }
}
