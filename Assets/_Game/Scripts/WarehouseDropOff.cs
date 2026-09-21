using UnityEngine;

namespace LifeSimulator
{
    // A named delivery location. Adding one to the warehouse is all that is needed for it to
    // appear in assignments.
    public sealed class WarehouseDropOff : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Loading Bay 1";
        [SerializeField] private ShiftAssignments assignments;

        public string DisplayName => displayName;
        public string Prompt => "Deliver Box";

        public void Interact(PlayerInteraction player)
        {
            if (isActiveAndEnabled && assignments != null)
                assignments.TryDeliver(this, player);
        }
    }
}
