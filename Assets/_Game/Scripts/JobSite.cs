using System;
using UnityEngine;

namespace LifeSimulator
{
    // Owns the schedule and the wage rules, and is the only thing that pays money. Boxes never do.
    public sealed class JobSite : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private ShiftAssignments assignments;
        [SerializeField] private WorkSchedule schedule = new WorkSchedule();

        // One employee for v0.1; a collection replaces this for multiplayer without touching
        // PlayerEmployment, because per-player state already lives on the player.
        private PlayerEmployment activeWorker;

        public WorkSchedule Schedule => schedule;
        public bool IsReady => clock != null && assignments != null;
        public PlayerEmployment ActiveWorker => activeWorker;

        private void OnEnable()
        {
            if (assignments != null) assignments.QuotaCompleted += OnQuotaCompleted;
        }

        private void OnDisable()
        {
            if (assignments != null) assignments.QuotaCompleted -= OnQuotaCompleted;
            if (activeWorker != null) FinishShift(activeWorker, false);
        }

        // Work is done: stop the clock on wages rather than inventing busywork to fill the shift.
        private void OnQuotaCompleted()
        {
            if (activeWorker != null && clock != null) activeWorker.CapPayableAt(clock.TotalMinutes);
        }

        public string TerminalPrompt()
        {
            if (!IsReady) return "Time Clock";
            if (activeWorker != null)
                return assignments.QuotaComplete ? "Clock Out — work complete" : "Clock Out";
            return schedule.IsWithinClockInWindow(clock.MinutesOfDay) ? "Clock In" : "Time Clock — closed";
        }

        public void ToggleClock(PlayerInteraction player)
        {
            if (player == null || !player.isActiveAndEnabled || !IsReady) return;
            if (!player.TryGetComponent(out PlayerEmployment employment))
            {
                player.ShowFeedback("You do not work here");
                return;
            }
            if (employment.IsClockedIn) TryClockOut(employment);
            else TryClockIn(employment);
        }

        public bool TryClockIn(PlayerEmployment employment)
        {
            if (employment == null || !employment.isActiveAndEnabled || !IsReady
                || employment.IsClockedIn || activeWorker != null || !assignments.IsReady)
                return false;

            int minutesOfDay = clock.MinutesOfDay;
            if (!schedule.IsWithinClockInWindow(minutesOfDay))
            {
                employment.Worker.ShowFeedback("The warehouse is closed. Shift is " + schedule.DescribeHours());
                return false;
            }

            long now = clock.TotalMinutes;
            long dayStart = now - minutesOfDay;
            long scheduledStart = dayStart + schedule.StartMinuteOfDay;
            long scheduledEnd = dayStart + schedule.EndMinuteOfDay;
            // Arriving early does not earn early wages; arriving late costs the missed minutes.
            long payableStart = Math.Max(now, scheduledStart);

            employment.ClockIn(this, payableStart, scheduledEnd);
            activeWorker = employment;
            assignments.BeginShift(employment);
            employment.Worker.ShowFeedback(payableStart > now
                ? "Clocked in early. Paid time starts at " + GameClock.FormatTimeOfDay(schedule.StartMinuteOfDay)
                : "Clocked in at " + clock.FormatTimeOfDay());
            return true;
        }

        public bool TryClockOut(PlayerEmployment employment)
        {
            if (employment == null || !employment.IsClockedIn || employment.Employer != this) return false;
            FinishShift(employment, true);
            return true;
        }

        // Called when the worker vanishes mid-shift; no wages, no dangling assignment.
        internal void AbandonShift(PlayerEmployment employment)
        {
            if (employment == activeWorker) FinishShift(employment, false);
        }

        private void FinishShift(PlayerEmployment employment, bool pay)
        {
            int completed = assignments != null ? assignments.Completed : 0;
            int quota = assignments != null ? assignments.AssignmentsPerShift : 0;
            long end = Math.Min(clock != null ? clock.TotalMinutes : 0L, employment.PayableEndMinute);
            double hours = Math.Max(0L, end - employment.PayableStartMinute) / (double)GameClock.MinutesPerHour;
            int wages = Mathf.RoundToInt((float)(hours * schedule.HourlyWage));

            if (assignments != null) assignments.EndShift();
            activeWorker = null;
            employment.ClockOut();

            if (!pay || employment.Wallet == null || !employment.Wallet.isActiveAndEnabled) return;
            employment.Wallet.TryAddMoney(wages);
            employment.Worker.ShowFeedback("Shift complete — Orders: " + completed + "/" + quota
                + "   Hours: " + hours.ToString("0.0") + "   Pay: $" + wages.ToString("N0"));
        }

        // The shift ends on schedule whether or not the player walks back to the terminal.
        private void Update()
        {
            if (activeWorker != null && clock != null && clock.TotalMinutes >= activeWorker.ScheduledEndMinute)
                FinishShift(activeWorker, true);
        }
    }
}
