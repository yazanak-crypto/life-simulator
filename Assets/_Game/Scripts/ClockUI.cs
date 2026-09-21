using UnityEngine;
using UnityEngine.UI;

namespace LifeSimulator
{
    // Mirrors BalanceUI: a serialized source plus a Text, refreshed from the source's event.
    public sealed class ClockUI : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private Text clockText;

        private void OnEnable()
        {
            if (clock == null || clockText == null)
            {
                Debug.LogError("Assign a game clock and Text to ClockUI.", this);
                enabled = false;
                return;
            }

            clock.MinuteTick += Refresh;
            Refresh(clock);
        }

        private void OnDisable()
        {
            if (clock != null)
                clock.MinuteTick -= Refresh;
        }

        private void Refresh(GameClock source) =>
            clockText.text = source.DayName + "\n" + source.FormatTimeOfDay();
    }
}
