using UnityEngine;

namespace LifeSimulator
{
    // A named pickup location. Not interactable itself: the spawned box is what the player aims at.
    // Adding a rack to the warehouse is all that is needed for it to appear in assignments.
    public sealed class WarehouseRack : MonoBehaviour
    {
        [SerializeField] private string aisle = "A";
        [SerializeField, Min(1)] private int rackNumber = 1;
        [SerializeField, Tooltip("Where a box spawns. Falls back to this transform.")]
        private Transform boxAnchor;

        public string DisplayName => "Aisle " + aisle + " — Rack " + rackNumber;
        public Transform BoxAnchor => boxAnchor != null ? boxAnchor : transform;
    }
}
