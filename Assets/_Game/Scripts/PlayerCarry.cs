using UnityEngine;

namespace LifeSimulator
{
    // Per-player "what am I holding". Knows nothing about jobs, orders or money.
    public sealed class PlayerCarry : MonoBehaviour
    {
        [SerializeField] private Transform carryPoint;

        public Transform CarryPoint => carryPoint;
        public CarryableItem Carried { get; private set; }
        public bool IsCarrying => Carried != null;

        public bool TryPickUp(CarryableItem item)
        {
            if (!isActiveAndEnabled || carryPoint == null || item == null || IsCarrying
                || item.IsCarried || !item.gameObject.activeInHierarchy)
                return false;

            item.AttachTo(this);
            Carried = item;
            return true;
        }

        // Releases whatever is held and hands it back so the caller can decide what happens next.
        public CarryableItem Release()
        {
            CarryableItem item = Carried;
            Carried = null;
            if (item != null) item.Detach();
            return item;
        }

        public bool IsHolding(CarryableItem item) => item != null && Carried == item;

        // A disabled or destroyed carrier must never leave an item parented to a hidden player.
        private void OnDisable() => Release();
    }
}
