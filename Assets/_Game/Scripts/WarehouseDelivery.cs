using UnityEngine;

namespace LifeSimulator
{
    public sealed class WarehouseDelivery : MonoBehaviour, IInteractable
    {
        private WarehouseTask assignedTask;
        public string Prompt => "Deliver Box";
        internal bool IsAvailable => assignedTask == null && gameObject.activeInHierarchy;

        public void Interact(PlayerInteraction player)
        {
            if (isActiveAndEnabled && assignedTask != null && assignedTask.Owner == player)
                assignedTask.TryDeliver(this);
        }

        internal void ReserveFor(WarehouseTask task)
        {
            assignedTask = task;
            enabled = false;
        }

        internal void EnableFor(WarehouseTask task)
        {
            if (assignedTask != task)
                return;
            enabled = true;
        }

        internal void Release(WarehouseTask task)
        {
            if (assignedTask != null && assignedTask != task)
                return;
            assignedTask = null;
            enabled = false;
        }
    }
}
