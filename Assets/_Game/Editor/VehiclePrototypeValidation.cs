using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LifeSimulator.Editor
{
    // Explicit edit-mode component/physics checks. Never save their runtime mutations.
    public static class VehiclePrototypeValidation
    {
        private const string ScenePath = "Assets/_Game/Scenes/PrototypeMovement.unity";
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + message);
            Debug.Log("PASS: " + message);
        }
        private static object Field(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Call(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        // Set the Transform too: Physics.SyncTransforms pushes Transform -> PhysX and would undo a body-only move.
        private static void Teleport(Rigidbody body, Vector3 position, Quaternion rotation)
        {
            body.transform.SetPositionAndRotation(position, rotation);
            body.position = position;
            body.rotation = rotation;
            Rest(body);
            Physics.SyncTransforms();
        }
        // SetDriven no longer zeroes momentum, so tests that need a stationary car say so.
        private static void Rest(Rigidbody body)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        // Peak single-step velocity change across an impact: the same quantity a future damage
        // component would read from Collision.relativeVelocity.
        private static float ImpactSeverity(ArcadeVehicle motor, Rigidbody body, float approachSpeed)
        {
            Teleport(body, new Vector3(36f, 0.08f, -27f), Quaternion.Euler(0f, 90f, 0f));
            Call(motor, "SetInput", Vector2.zero);
            body.linearVelocity = motor.Forward * approachSpeed;
            Vector3 previous = body.linearVelocity;
            float peak = 0f;
            for (int i = 0; i < 200; i++)
            {
                Call(motor, "FixedUpdate");
                Physics.Simulate(Time.fixedDeltaTime);
                peak = Mathf.Max(peak, (body.linearVelocity - previous).magnitude);
                previous = body.linearVelocity;
            }
            return peak;
        }
        private static void Step(ArcadeVehicle motor, Vector2 input, int frames)
        {
            Call(motor, "SetInput", input);
            for (int i = 0; i < frames; i++)
            {
                Call(motor, "FixedUpdate");
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        [MenuItem("Tools/Life Simulator/Validate vehicle (reopens scene)")]
        public static void Validate()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SimulationMode previousSimulation = Physics.simulationMode;
            try
            {
                // Includes the existing three-delivery economy/ownership checks.
                DealershipPrototype.Validate();
                EditorSceneManager.OpenScene(ScenePath);
                Physics.simulationMode = SimulationMode.Script;
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
                var mode = player.GetComponent<PlayerVehicleMode>();
                var movement = player.GetComponent<ThirdPersonPlayer>();
                var character = player.GetComponent<CharacterController>();
                var camera = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
                var seat = UnityEngine.Object.FindFirstObjectByType<VehicleSeat>();
                var motor = seat.GetComponent<ArcadeVehicle>();
                var body = seat.GetComponent<Rigidbody>();
                var wallet = player.GetComponent<PlayerWallet>();
                var purchase = seat.Ownership;
                foreach (Component component in new Component[] { player, movement, camera, wallet,
                             player.GetComponent<WarehouseTask>(), motor, seat, mode }) Call(component, "Awake");
                player.transform.position = seat.transform.position + Vector3.right * 2.3f;
                seat.Interact(player);
                Check(!mode.IsDriving && seat.Driver == null && !motor.IsDriven, "Unpurchased car rejects entry and is left undriven");
                wallet.TryAddMoney(300);
                purchase.Interact(player);
                Check(wallet.Balance == 0 && purchase.IsOwnedBy(player), "Purchase still costs $300 and retains the same owner");
                var other = UnityEngine.Object.Instantiate(player.gameObject);
                other.name = "Validation Other Player";
                seat.Interact(other.GetComponent<PlayerInteraction>());
                Check(seat.Driver == null && !other.GetComponent<PlayerVehicleMode>().IsDriving, "A different player cannot enter an owned car");
                UnityEngine.Object.DestroyImmediate(other);
                player.transform.position = seat.transform.position + Vector3.right * 2.3f;
                var workplace = UnityEngine.Object.FindFirstObjectByType<Workplace>();
                workplace.Interact(player);
                seat.Interact(player);
                Check(!mode.IsDriving && player.GetComponent<WarehouseTask>().HasActiveShift, "Entry during warehouse work preserves the active shift");
                player.GetComponent<WarehouseTask>().CancelShift();
                float oldYaw = (float)Field(camera, "yaw");
                float oldPitch = (float)Field(camera, "pitch");
                seat.Interact(player);
                Check(mode.CurrentVehicle == seat && seat.Driver == mode && motor.IsDriven && !body.isKinematic, "Owner enters; seat and player agree on the driver");
                Check(!movement.enabled && !player.enabled && !character.enabled
                    && player.GetComponentsInChildren<Renderer>().All(r => !r.enabled)
                    && player.GetComponentsInChildren<Collider>().All(c => !c.enabled), "Walking, generic interactions, renderers and all player collisions are suspended");
                Check((Transform)Field(camera, "followOverride") == seat.transform, "Chase camera follows the car");
                Check(!mode.TryEnter(seat), "Duplicate entry rejected");
                var action = (InputAction)Field(mode, "drive");
                Check(new[] { "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d" }
                    .All(path => action.bindings.Any(b => b.path == path)), "W/S/A/D bindings are configured");
                Check(((InputAction)Field(mode, "exit")).bindings[0].path == "<Keyboard>/e", "E exits vehicle mode");
                // Move the occupied vehicle into the clear test apron for deterministic physics checks.
                Teleport(body, new Vector3(40f, 0.08f, -27f), Quaternion.Euler(0f, 90f, 0f));
                Physics.SyncTransforms();
                Step(motor, Vector2.up, 10);
                Check(motor.Speed > 0f && motor.Speed < 3f, "Forward acceleration is gradual");
                Step(motor, Vector2.up, 160);
                Check(motor.Speed > 10f && motor.Speed <= 14.1f, "Forward speed is capped at 14 m/s");
                float fast = motor.Speed;
                Step(motor, Vector2.zero, 25);
                Check(motor.Speed < fast && motor.Speed > 0f, "Releasing throttle naturally slows the car");
                Step(motor, Vector2.down, 200);
                Check(motor.Speed < -4f && motor.Speed >= -5.1f, "S brakes and then reverses, capped at 5 m/s");
                Rest(body);
                Quaternion stationary = body.rotation;
                Step(motor, Vector2.right, 30);
                Check(Quaternion.Angle(stationary, body.rotation) < 0.1f, "Stationary steering does not spin the car");
                Step(motor, Vector2.up, 30);
                Quaternion straight = body.rotation;
                Step(motor, new Vector2(1f, 1f), 20);
                Check(Quaternion.Angle(straight, body.rotation) > 5f, "Steering turns a moving car");
                Check(Mathf.Abs(body.rotation.eulerAngles.x) < 0.1f && Mathf.Abs(body.rotation.eulerAngles.z) < 0.1f,
                    "Vehicle remains upright");
                // Tyre grip is finite: a hard corner should slip a little (not on rails) while
                // staying far from a spin (not on ice).
                Step(motor, new Vector2(1f, 1f), 40);
                Vector3 travel = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
                float slip = Vector3.Angle(travel, Vector3.ProjectOnPlane(motor.Forward, Vector3.up));
                Debug.Log("Cornering slip angle: " + slip + " degrees at " + travel.magnitude + " m/s");
                Check(travel.magnitude > 2f && slip > 0.5f && slip < 25f,
                    "Hard cornering slips slightly instead of running on rails");
                // Driver state and physical state are separate: losing the driver must not brake.
                Rest(body);
                Teleport(body, new Vector3(40f, 0.08f, -27f), Quaternion.Euler(0f, 90f, 0f));
                Step(motor, Vector2.up, 120);
                float cruising = motor.Speed;
                Check(cruising > 6f, "Reaches cruising speed for the momentum checks");
                Check(!mode.TryExit() && mode.IsDriving, "Exiting above the safe exit speed is temporarily blocked");
                Vector3 momentum = body.linearVelocity;
                Call(motor, "SetDriven", false);
                Check(!motor.IsDriven && !body.isKinematic
                    && (body.linearVelocity - momentum).magnitude < 0.01f,
                    "Removing driver control preserves velocity and leaves the car simulated");
                Step(motor, Vector2.zero, 25);
                Check(motor.Speed > 0.5f && motor.Speed < cruising, "Unoccupied car keeps rolling and slows naturally");
                Step(motor, Vector2.zero, 400);
                Check(motor.Speed < 0.5f, "Unoccupied car coasts to rest without being frozen");
                Call(motor, "SetDriven", true);

                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Validation Collision Wall";
                wall.transform.position = new Vector3(30f, 2f, -27f);
                wall.transform.localScale = new Vector3(1f, 4f, 12f);
                Physics.SyncTransforms();
                Teleport(body, new Vector3(40f, 0.08f, -27f), Quaternion.Euler(0f, 90f, 0f));
                Step(motor, Vector2.up, 200);
                Check(body.position.x > 32f && body.position.x < 35f, "Rigidbody stops against a solid wall without passing through");
                float gentle = ImpactSeverity(motor, body, 4f);
                float heavy = ImpactSeverity(motor, body, 14f);
                Debug.Log("Impact severity: gentle=" + gentle + " m/s, heavy=" + heavy + " m/s");
                Check(heavy > gentle * 2.5f && heavy > 5f,
                    "Impact severity scales with speed, keeping Collision.relativeVelocity usable for damage");
                UnityEngine.Object.DestroyImmediate(wall);
                // Block both sides and rear-side exits with solids; do not teleport through them.
                Teleport(body, new Vector3(20f, 0.08f, -27f), Quaternion.identity);
                var blockers = new GameObject[2];
                for (int i = 0; i < 2; i++)
                {
                    blockers[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blockers[i].transform.position = body.position + new Vector3(i == 0 ? -2.25f : 2.25f, 1f, 0.75f);
                    blockers[i].transform.localScale = new Vector3(1.2f, 3f, 5f);
                }
                Physics.SyncTransforms();
                Check(!mode.TryExit() && mode.IsDriving, "Blocked exits keep the driver in vehicle mode");
                foreach (var blocker in blockers) UnityEngine.Object.DestroyImmediate(blocker);
                Physics.SyncTransforms();
                Check(mode.TryExit(), "Clear side allows exit at low speed");
                Check(!mode.IsDriving && seat.Driver == null && !motor.IsDriven && !body.isKinematic
                    && movement.enabled && player.enabled && character.enabled
                    && player.GetComponentsInChildren<Renderer>().All(r => r.enabled), "Exit restores walking, visibility, collision and unoccupied state");
                Check((Transform)Field(camera, "followOverride") == null && (float)Field(camera, "yaw") == oldYaw
                    && (float)Field(camera, "pitch") == oldPitch, "On-foot camera settings and orbit are restored");
                Check(Mathf.Abs(player.transform.position.x - body.position.x) > 2f, "Player exits beside the vehicle");
                Check(!mode.TryExit() && !mode.TryEnter(seat), "Repeated exit and immediate re-entry are rejected");
                workplace.Interact(player);
                UnityEngine.Object.FindFirstObjectByType<WarehouseBox>(FindObjectsInactive.Include).Interact(player);
                UnityEngine.Object.FindFirstObjectByType<WarehouseDelivery>().Interact(player);
                Check(wallet.Balance == 100, "Warehouse pays $100 after driving and exiting");
                Debug.Log("VEHICLE VALIDATION PASSED: component and physics simulation. Manual Game-view input/camera checks still required.");
            }
            finally
            {
                Physics.simulationMode = previousSimulation;
                EditorSceneManager.OpenScene(ScenePath);
            }
        }
    }
}
