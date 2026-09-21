using UnityEngine;

namespace LifeSimulator
{
    // One box per assignment, spawned from a prefab by ShiftAssignments and destroyed on delivery.
    // Never a shared scene object: two players must be able to carry two different boxes.
    [RequireComponent(typeof(CarryableItem))]
    public sealed class WarehouseBox : MonoBehaviour, IInteractable
    {
        private CarryableItem item;
        // Resolved lazily: Awake does not run on instances spawned in edit mode by the validation
        // suite, and a box with no item would silently refuse every pickup.
        public CarryableItem Item => item != null ? item : (item = GetComponent<CarryableItem>());
        // Set by the spawner. A box with no source cannot be picked up.
        internal ShiftAssignments Source { get; set; }

        public string Prompt => "Pick Up Box";

        public void Interact(PlayerInteraction player)
        {
            if (isActiveAndEnabled && Source != null)
                Source.TryPickUp(this, player);
        }
    }
}
