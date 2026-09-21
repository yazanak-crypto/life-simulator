using System;
using UnityEngine;

namespace LifeSimulator
{
    // Data only: no behaviour, no scene presence. A second job is a second JobSite with its own
    // schedule, which is why this is a serializable class rather than something warehouse-specific.
    [Serializable]
    public sealed class WorkSchedule
    {
        [SerializeField] private string jobTitle = "Warehouse Worker";
        [SerializeField, Range(0, 23)] private int startHour = 8;
        [SerializeField, Range(0, 23)] private int endHour = 16;
        [SerializeField, Min(0), Tooltip("How long before the shift the player may clock in. Paid time still starts at the scheduled hour.")]
        private int earlyClockInMinutes = 15;
        [SerializeField, Min(0), Tooltip("TEMPORARY prototype balancing value.")]
        private float hourlyWage = 14f;

        public string JobTitle => jobTitle;
        public float HourlyWage => hourlyWage;
        public int EarlyClockInMinutes => earlyClockInMinutes;
        public int StartMinuteOfDay => startHour * GameClock.MinutesPerHour;
        public int EndMinuteOfDay => endHour * GameClock.MinutesPerHour;

        public bool IsWithinClockInWindow(int minutesOfDay) =>
            minutesOfDay >= StartMinuteOfDay - earlyClockInMinutes && minutesOfDay < EndMinuteOfDay;

        public string DescribeHours() =>
            GameClock.FormatTimeOfDay(StartMinuteOfDay) + " – " + GameClock.FormatTimeOfDay(EndMinuteOfDay);
    }
}
