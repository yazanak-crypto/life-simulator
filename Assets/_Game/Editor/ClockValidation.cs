using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LifeSimulator.Editor
{
    // Pure clock arithmetic: no scene, no Play mode, no physics.
    public static class ClockValidation
    {
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + message);
            Debug.Log("PASS: " + message);
        }

        private static GameClock NewClock(int hour, int minute, int dayOfWeek)
        {
            var host = new GameObject("Validation Clock");
            var clock = host.AddComponent<GameClock>();
            Set(clock, "startHour", hour);
            Set(clock, "startMinute", minute);
            Set(clock, "startDayOfWeek", dayOfWeek);
            typeof(GameClock).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(clock, null);
            return clock;
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [MenuItem("Tools/Life Simulator/Validate clock")]
        public static void Validate()
        {
            GameClock clock = null;
            try
            {
                clock = NewClock(6, 0, 1);
                Check(clock.Day == 1 && clock.Hour == 6 && clock.Minute == 0 && clock.DayName == "Monday",
                    "Clock starts at the configured day and time");
                Check(clock.MinutesOfDay == 360, "MinutesOfDay reports 6:00 AM as 360");

                int ticks = 0;
                clock.MinuteTick += _ => ticks++;
                clock.AdvanceMinutes(74);
                Check(clock.Hour == 7 && clock.Minute == 14 && ticks == 1,
                    "Advancing 74 minutes reaches 7:14 AM and raises one tick");
                Check(clock.FormatTimeOfDay() == "7:14 AM", "Formats 7:14 AM");

                clock.Advance(30);
                Check(clock.Minute == 14 && ticks == 1, "A part-minute advance raises no extra tick");
                clock.Advance(30);
                Check(clock.Minute == 15 && ticks == 2, "Crossing the minute boundary raises one tick");

                Check(GameClock.FormatTimeOfDay(0) == "12:00 AM", "Midnight formats as 12:00 AM");
                Check(GameClock.FormatTimeOfDay(720) == "12:00 PM", "Noon formats as 12:00 PM");
                Check(GameClock.FormatTimeOfDay(480) == "8:00 AM", "Shift start formats as 8:00 AM");
                Check(GameClock.FormatTimeOfDay(960) == "4:00 PM", "Shift end formats as 4:00 PM");
                Check(GameClock.FormatTimeOfDay(1439) == "11:59 PM", "Last minute of the day formats correctly");

                int beforeDay = clock.Day;
                int beforeMinutes = clock.MinutesOfDay;
                clock.AdvanceMinutes(GameClock.MinutesPerDay);
                Check(clock.Day == beforeDay + 1 && clock.MinutesOfDay == beforeMinutes,
                    "A full day advances the day counter and preserves the time of day");
                Check(clock.DayName == "Tuesday", "Day name rolls over to Tuesday");

                UnityEngine.Object.DestroyImmediate(clock.gameObject);
                clock = NewClock(23, 30, 6);
                clock.AdvanceMinutes(45);
                Check(clock.Day == 2 && clock.Hour == 0 && clock.Minute == 15 && clock.DayName == "Sunday",
                    "Crossing midnight rolls the day and wraps the hour");

                clock.SkipToTimeOfDay(7, 50);
                Check(clock.Day == 2 && clock.Hour == 7 && clock.Minute == 50,
                    "SkipToTimeOfDay reaches a later time on the same day");
                clock.SkipToTimeOfDay(6, 0);
                Check(clock.Day == 3 && clock.Hour == 6 && clock.Minute == 0,
                    "SkipToTimeOfDay never rewinds; an earlier time lands on the next day");

                clock.SetPaused(true);
                Check(clock.IsPaused, "Clock reports the paused state");
                long paused = clock.TotalMinutes;
                clock.AdvanceMinutes(10);
                Check(clock.TotalMinutes == paused + 10,
                    "Explicit advances still work while paused, so development overrides stay usable");

                Debug.Log("CLOCK VALIDATION PASSED");
            }
            finally
            {
                if (clock != null) UnityEngine.Object.DestroyImmediate(clock.gameObject);
            }
        }
    }
}
