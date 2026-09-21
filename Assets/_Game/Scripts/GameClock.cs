using System;
using UnityEngine;

namespace LifeSimulator
{
    // The single source of in-game time. Deliberately a plain scene component rather than a
    // singleton or static: multiplayer will make time server-authoritative, and a static would
    // have to be torn out. Consumers either hold a serialized reference or subscribe to MinuteTick.
    public sealed class GameClock : MonoBehaviour
    {
        public const int MinutesPerHour = 60;
        public const int HoursPerDay = 24;
        public const int MinutesPerDay = MinutesPerHour * HoursPerDay;

        [SerializeField, Min(0f), Tooltip("In-game seconds per real second. 60 compresses one in-game day into 24 real minutes. Nothing should assume this value.")]
        private float compression = 60f;
        [SerializeField, Range(0, 6), Tooltip("0 = Sunday.")] private int startDayOfWeek = 1;
        [SerializeField, Range(0, 23)] private int startHour = 6;
        [SerializeField, Range(0, 59)] private int startMinute;

        private static readonly string[] DayNames =
            { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        private double elapsedSeconds;
        private long lastMinute = -1;

        public bool IsPaused { get; private set; }
        public float Compression => compression;
        public double TotalSeconds => elapsedSeconds;
        public long TotalMinutes => (long)(elapsedSeconds / 60d);
        // Day 1 is the first in-game day.
        public int Day => (int)(TotalMinutes / MinutesPerDay) + 1;
        public int MinutesOfDay => (int)(TotalMinutes % MinutesPerDay);
        public int Hour => MinutesOfDay / MinutesPerHour;
        public int Minute => MinutesOfDay % MinutesPerHour;
        public string DayName => DayNames[(startDayOfWeek + Day - 1) % 7];
        // For future day/night lighting; no lighting system reads it yet.
        public float NormalizedDay => MinutesOfDay / (float)MinutesPerDay;

        // Raised at most once per frame, after the clock has advanced past a whole minute. A large
        // jump raises it once, so consumers must compare absolute times rather than count ticks.
        public event Action<GameClock> MinuteTick;

        private void Awake()
        {
            elapsedSeconds = ((startHour * MinutesPerHour) + startMinute) * 60d;
            lastMinute = TotalMinutes;
        }

        private void Update()
        {
            if (IsPaused || compression <= 0f) return;
            Advance(Time.deltaTime * compression);
        }

        public void SetPaused(bool paused) => IsPaused = paused;

        // Shared by Update and the development overrides so both raise identical ticks.
        public void Advance(double seconds)
        {
            if (seconds <= 0d) return;
            elapsedSeconds += seconds;
            long now = TotalMinutes;
            if (now == lastMinute) return;
            lastMinute = now;
            MinuteTick?.Invoke(this);
        }

        public void AdvanceMinutes(double minutes) => Advance(minutes * 60d);

        // Always moves forward to the next occurrence: time never rewinds under live systems.
        public void SkipToTimeOfDay(int hour, int minute)
        {
            int target = Mathf.Clamp(hour, 0, 23) * MinutesPerHour + Mathf.Clamp(minute, 0, 59);
            int delta = target - MinutesOfDay;
            if (delta <= 0) delta += MinutesPerDay;
            AdvanceMinutes(delta);
        }

        public string FormatTimeOfDay() => FormatTimeOfDay(MinutesOfDay);

        public static string FormatTimeOfDay(int minutesOfDay)
        {
            int normalized = ((minutesOfDay % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
            int hour24 = normalized / MinutesPerHour;
            int minute = normalized % MinutesPerHour;
            int hour12 = hour24 % 12 == 0 ? 12 : hour24 % 12;
            return hour12 + ":" + minute.ToString("00") + (hour24 < 12 ? " AM" : " PM");
        }
    }
}
