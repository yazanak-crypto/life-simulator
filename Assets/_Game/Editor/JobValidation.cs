using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LifeSimulator.Editor
{
    // Schedule, assignment and wage checks driven through the same public entry points the player
    // uses (Interact / TryClockIn), so the validated path is the shipped path.
    public static class JobValidation
    {
        private const string ScenePath = "Assets/_Game/Scenes/PrototypeMovement.unity";

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + message);
            Debug.Log("PASS: " + message);
        }

        internal static void Awake(Component component) => component.GetType()
            .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, null);

        // Unity does not raise OnEnable in edit mode, so event subscriptions must be made by hand.
        internal static void Enable(Component component) => component.GetType()
            .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, null);

        internal static void SetPrivate(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [MenuItem("Tools/Life Simulator/Validate warehouse job (reopens scene)")]
        public static void Validate()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            try
            {
                RunChecks();
                Debug.Log("JOB VALIDATION PASSED");
            }
            finally
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        internal static void RunChecks()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
            var wallet = player.GetComponent<PlayerWallet>();
            var carry = player.GetComponent<PlayerCarry>();
            var employment = player.GetComponent<PlayerEmployment>();
            var clock = UnityEngine.Object.FindFirstObjectByType<GameClock>();
            var jobSite = UnityEngine.Object.FindFirstObjectByType<JobSite>();
            var shift = UnityEngine.Object.FindFirstObjectByType<ShiftAssignments>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<TimeClockTerminal>();

            foreach (Component c in new Component[] { wallet, carry, employment, clock, player })
                Awake(c);
            Enable(jobSite);

            // Orders take in-game time; without this the whole shift would occur in one minute.
            const int minutesPerOrder = 40;

            Check(shift.IsReady && jobSite.IsReady, "Warehouse is wired: box prefab, racks, drop-offs, clock");
            Check(UnityEngine.Object.FindObjectsByType<WarehouseRack>(FindObjectsSortMode.None).Length >= 8,
                "Warehouse exposes multiple racks across aisles");
            Check(UnityEngine.Object.FindObjectsByType<WarehouseDropOff>(FindObjectsSortMode.None).Length >= 2,
                "Warehouse exposes multiple destinations");
            Check(wallet.Balance == 0, "Normal starting balance is $0");

            // --- schedule window -------------------------------------------------------------
            Check(clock.Hour == 6, "Day starts at 6:00 AM, before the shift");
            terminal.Interact(player);
            Check(!employment.IsClockedIn, "Clocking in hours before the shift is refused");
            Check(terminal.Prompt.Contains("closed"), "Terminal reports the warehouse as closed");

            clock.SkipToTimeOfDay(7, 50);
            Check(terminal.Prompt == "Clock In", "Terminal offers Clock In inside the early window");
            terminal.Interact(player);
            Check(employment.IsClockedIn && employment.Employer == jobSite, "Owner clocks in during the early window");
            long scheduledStart = employment.ScheduledEndMinute - 8 * GameClock.MinutesPerHour;
            Check(employment.PayableStartMinute == scheduledStart,
                "Arriving early does not start paid time before the scheduled hour");
            Check(shift.Current != null && shift.Current.State == AssignmentState.Issued,
                "Clocking in issues the first order");
            Check(shift.Current.Box != null && shift.Current.Box != (WarehouseBox)null,
                "The order spawns its own box instance");

            // --- one full order --------------------------------------------------------------
            Assignment first = shift.Current;
            WarehouseBox firstBox = first.Box;
            Check(Vector3.Distance(firstBox.transform.position, first.Rack.BoxAnchor.position) < 0.5f,
                "The box spawns at its assigned rack");
            WarehouseDropOff wrong = UnityEngine.Object.FindObjectsByType<WarehouseDropOff>(FindObjectsSortMode.None)
                .First(d => d != first.DropOff);
            wrong.Interact(player);
            Check(!carry.IsCarrying && shift.Completed == 0, "Delivering before pickup does nothing");
            firstBox.Interact(player);
            Check(carry.IsCarrying && carry.IsHolding(firstBox.Item) && shift.Current.State == AssignmentState.Carrying,
                "Picking up the order's box attaches it to the player");
            wrong.Interact(player);
            Check(carry.IsCarrying && shift.Completed == 0, "Delivering to the wrong destination is refused");
            first.DropOff.Interact(player);
            clock.AdvanceMinutes(minutesPerOrder);
            Check(!carry.IsCarrying && shift.Completed == 1, "Delivering to the correct destination completes the order");
            Check(wallet.Balance == 0, "Delivering a box pays no money by itself");
            Check(shift.Current != null && shift.Current.OrderNumber != first.OrderNumber,
                "A new order is issued immediately");

            // --- remaining quota -------------------------------------------------------------
            int guard = 0;
            while (!shift.QuotaComplete && guard++ < 64)
            {
                Assignment current = shift.Current;
                current.Box.Interact(player);
                current.DropOff.Interact(player);
                clock.AdvanceMinutes(minutesPerOrder);
            }
            Check(shift.QuotaComplete && shift.Completed == shift.AssignmentsPerShift,
                "Completing the configured number of orders finishes the shift's work");
            Check(shift.Current == null, "No further orders are issued once the quota is met");
            Check(wallet.Balance == 0, "Still unpaid until clock-out");

            // Idling after the work is done must not earn wages.
            long cappedAt = employment.PayableEndMinute;
            Check(cappedAt < employment.ScheduledEndMinute,
                "Finishing the quota caps paid time before the scheduled end");
            clock.AdvanceMinutes(120);
            Check(employment.PayableEndMinute == cappedAt,
                "Paid time stops when the work is complete, so idling earns nothing");

            // --- wages -----------------------------------------------------------------------
            double hours = (employment.PayableEndMinute - employment.PayableStartMinute) / 60d;
            int expected = Mathf.RoundToInt((float)(hours * jobSite.Schedule.HourlyWage));
            terminal.Interact(player);
            Check(!employment.IsClockedIn, "Clocking out ends the shift");
            Check(wallet.Balance == expected && expected > 0,
                "Wages equal hours worked times the hourly rate ($" + expected + ")");
            Check(employment.ShiftsWorked == 1, "The completed shift is recorded on the player");

            // --- late arrival is paid from arrival -------------------------------------------
            long balanceAfterFirst = wallet.Balance;
            clock.SkipToTimeOfDay(10, 0);
            terminal.Interact(player);
            Check(employment.IsClockedIn, "A late arrival may still clock in during the shift");
            Check(employment.PayableStartMinute == clock.TotalMinutes,
                "A late arrival is paid from arrival, not from the scheduled start");
            clock.SkipToTimeOfDay(16, 0);
            jobSite.SendMessage("Update");
            Check(!employment.IsClockedIn, "The shift ends on schedule without returning to the terminal");
            Check(wallet.Balance == balanceAfterFirst + Mathf.RoundToInt(6f * jobSite.Schedule.HourlyWage),
                "A 10:00 AM to 4:00 PM shift pays exactly six hours");

            // --- closed outside the window ---------------------------------------------------
            clock.SkipToTimeOfDay(18, 0);
            terminal.Interact(player);
            Check(!employment.IsClockedIn, "Clocking in after the shift window is refused");
        }
    }
}
