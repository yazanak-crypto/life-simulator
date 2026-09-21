using UnityEngine;

namespace LifeSimulator
{
    // The physical punch clock. Thin on purpose: all schedule and wage rules live in JobSite.
    public sealed class TimeClockTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private JobSite jobSite;

        public string Prompt => jobSite != null ? jobSite.TerminalPrompt() : "Time Clock";

        public void Interact(PlayerInteraction player)
        {
            if (isActiveAndEnabled && jobSite != null)
                jobSite.ToggleClock(player);
        }
    }
}
