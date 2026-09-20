using UnityEngine;

namespace LifeSimulator
{
    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        public string Prompt => "Test Interaction";

        public void Interact(PlayerInteraction player)
        {
            player.ShowFeedback("Interaction successful");
        }
    }
}
