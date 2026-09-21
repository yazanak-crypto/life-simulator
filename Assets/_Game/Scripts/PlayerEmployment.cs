using UnityEngine;

namespace LifeSimulator
{
    // Per-player employment state. Holds no wage rules of its own: the JobSite owns those, so a
    // second employer never needs this component changed. Nothing here is static or global.
    [RequireComponent(typeof(PlayerInteraction), typeof(PlayerWallet), typeof(PlayerCarry))]
    public sealed class PlayerEmployment : MonoBehaviour
    {
        public PlayerInteraction Worker { get; private set; }
        public PlayerWallet Wallet { get; private set; }
        public PlayerCarry Carry { get; private set; }

        public JobSite Employer { get; private set; }
        public bool IsClockedIn => Employer != null;
        // Absolute game minutes, so a shift may legitimately cross midnight later.
        public long PayableStartMinute { get; private set; }
        public long ScheduledEndMinute { get; private set; }
        // Lowered when the shift's orders are finished, so idling earns nothing.
        public long PayableEndMinute { get; private set; }
        public int ShiftsWorked { get; private set; }

        private void Awake()
        {
            Worker = GetComponent<PlayerInteraction>();
            Wallet = GetComponent<PlayerWallet>();
            Carry = GetComponent<PlayerCarry>();
        }

        internal void ClockIn(JobSite employer, long payableStart, long scheduledEnd)
        {
            Employer = employer;
            PayableStartMinute = payableStart;
            ScheduledEndMinute = scheduledEnd;
            PayableEndMinute = scheduledEnd;
        }

        internal void CapPayableAt(long minute)
        {
            if (IsClockedIn && minute < PayableEndMinute) PayableEndMinute = minute;
        }

        internal void ClockOut()
        {
            if (!IsClockedIn) return;
            Employer = null;
            ShiftsWorked++;
        }

        // Losing the player mid-shift must not leave the employer holding a dangling worker.
        private void OnDisable()
        {
            if (Employer != null) Employer.AbandonShift(this);
        }
    }
}
