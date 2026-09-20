using UnityEngine;

namespace LifeSimulator
{
    public sealed class Workplace : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(1)] private int wage = 100;
        [SerializeField] private WarehouseBox jobBox;
        [SerializeField] private WarehouseDelivery deliveryPoint;

        private WarehouseTask activeTask;
        public string Prompt => activeTask != null ? "Shift active" : "Start Shift";

        public void Interact(PlayerInteraction player)
        {
            if (!isActiveAndEnabled || activeTask != null || player == null || !player.isActiveAndEnabled)
                return;

            if (!player.TryGetComponent(out WarehouseTask task))
            {
                player.ShowFeedback("A warehouse task component is required to start a shift.");
                return;
            }

            if (task.TryStartShift(this, jobBox, deliveryPoint, wage))
                activeTask = task;
        }

        internal void Release(WarehouseTask task)
        {
            if (activeTask == task)
                activeTask = null;
        }

        private void OnDisable()
        {
            if (activeTask != null)
                activeTask.CancelShift();
        }
    }
}
