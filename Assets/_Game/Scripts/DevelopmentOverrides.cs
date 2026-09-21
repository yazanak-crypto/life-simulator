using UnityEngine;
using UnityEngine.InputSystem;

namespace LifeSimulator
{
    // Editor and development builds only. Everything here drives the same public entry points the
    // player uses, so a shortcut can never put the game into a state normal play cannot reach.
    // Nothing in normal gameplay UI exposes any of this.
    public sealed class DevelopmentOverrides : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private PlayerInteraction player;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private PlayerEmployment employment;
        [SerializeField] private CarPurchase starterCar;
        [SerializeField] private JobSite jobSite;
        [SerializeField] private ShiftAssignments assignments;

        [Header("Applied once on start")]
        [SerializeField, Min(0)] private int startingMoney;
        [SerializeField, Tooltip("Buys the starter car through the normal purchase path.")]
        private bool grantCarOnStart;
        [SerializeField, Tooltip("Jumps the clock to just before the shift so commuting can be skipped.")]
        private bool skipToShiftStart;
        [SerializeField, Min(0f), Tooltip("Overrides GameClock compression when above zero.")]
        private float compressionOverride;

        [Header("Hotkeys (editor / development builds)")]
        [SerializeField] private bool hotkeysEnabled = true;

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (compressionOverride > 0f && clock != null)
                clock.GetType().GetField("compression",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(clock, compressionOverride);
#else
            enabled = false;
#endif
        }

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (startingMoney > 0 && wallet != null) wallet.TryAddMoney(startingMoney);
            if (grantCarOnStart && starterCar != null && wallet != null && player != null)
            {
                // Use the real purchase so ownership, signage and relocation all follow normal rules.
                wallet.TryAddMoney(starterCar.Price);
                starterCar.Interact(player);
            }
            if (skipToShiftStart && clock != null && jobSite != null)
                clock.SkipToTimeOfDay(jobSite.Schedule.StartMinuteOfDay / GameClock.MinutesPerHour, 0);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (!hotkeysEnabled || Keyboard.current == null) return;

            if (Keyboard.current.f1Key.wasPressedThisFrame && clock != null)
                clock.AdvanceMinutes(GameClock.MinutesPerHour);

            if (Keyboard.current.f2Key.wasPressedThisFrame && clock != null && jobSite != null)
                clock.SkipToTimeOfDay(jobSite.Schedule.StartMinuteOfDay / GameClock.MinutesPerHour, 0);

            if (Keyboard.current.f3Key.wasPressedThisFrame && jobSite != null && employment != null)
                jobSite.TryClockOut(employment);

            // Completes the current order through the normal pickup/deliver interactions.
            if (Keyboard.current.f4Key.wasPressedThisFrame && assignments != null
                && assignments.Current != null && player != null)
            {
                Assignment current = assignments.Current;
                if (current.Box != null) current.Box.Interact(player);
                if (assignments.Current == current && current.DropOff != null)
                    current.DropOff.Interact(player);
            }
        }
#endif
    }
}
