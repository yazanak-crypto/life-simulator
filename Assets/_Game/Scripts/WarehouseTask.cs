using UnityEngine;

namespace LifeSimulator
{
    public enum WarehouseTaskState { NoShift, PickUpBox, DeliverBox, Complete }

    [RequireComponent(typeof(PlayerInteraction), typeof(PlayerWallet))]
    public sealed class WarehouseTask : MonoBehaviour
    {
        [SerializeField] private Transform carryPoint;
        [SerializeField] private ObjectiveUI objectiveUI;

        private PlayerWallet wallet;
        private Workplace workplace;
        private WarehouseBox box;
        private WarehouseDelivery deliveryPoint;
        private int reward;
        private bool completing;

        public PlayerInteraction Owner { get; private set; }
        public WarehouseTaskState State { get; private set; }
        public bool HasActiveShift => State == WarehouseTaskState.PickUpBox || State == WarehouseTaskState.DeliverBox;

        private void Awake()
        {
            Owner = GetComponent<PlayerInteraction>();
            wallet = GetComponent<PlayerWallet>();
            SetState(WarehouseTaskState.NoShift);
        }

        internal bool TryStartShift(Workplace source, WarehouseBox assignedBox, WarehouseDelivery destination, int payment)
        {
            if (!isActiveAndEnabled || HasActiveShift || completing || Owner == null || !Owner.isActiveAndEnabled
                || wallet == null || !wallet.isActiveAndEnabled || carryPoint == null || source == null
                || assignedBox == null || !assignedBox.IsAvailable || destination == null || !destination.IsAvailable
                || payment <= 0)
                return false;

            workplace = source;
            box = assignedBox;
            deliveryPoint = destination;
            reward = payment;
            deliveryPoint.ReserveFor(this);
            SetState(WarehouseTaskState.PickUpBox);
            box.MakeAvailable(this);
            Owner.ShowFeedback("Shift started. Pick up the box.");
            return true;
        }

        internal void TryPickUp(WarehouseBox candidate)
        {
            if (!isActiveAndEnabled || State != WarehouseTaskState.PickUpBox || candidate != box
                || Owner == null || !Owner.isActiveAndEnabled)
                return;

            box.CarryAt(carryPoint);
            SetState(WarehouseTaskState.DeliverBox);
            deliveryPoint.EnableFor(this);
        }

        internal void TryDeliver(WarehouseDelivery destination)
        {
            if (!isActiveAndEnabled || completing || State != WarehouseTaskState.DeliverBox
                || destination != deliveryPoint || box == null || !box.IsCarriedAt(carryPoint)
                || Owner == null || !Owner.isActiveAndEnabled || wallet == null || !wallet.isActiveAndEnabled)
                return;

            // Keep delivery locked through BalanceChanged callbacks as well as repeated input.
            completing = true;
            try
            {
                int payment = reward;
                if (!wallet.TryAddMoney(payment))
                {
                    Owner.ShowFeedback("Wallet is full. Delivery has not completed.");
                    return;
                }

                ReleaseObjects();
                SetState(WarehouseTaskState.Complete);
                Owner.ShowFeedback($"Delivery complete! +${payment:N0}");
            }
            finally
            {
                completing = false;
            }
        }

        private void Update()
        {
            if (HasActiveShift && (Owner == null || !Owner.isActiveAndEnabled || wallet == null
                || !wallet.isActiveAndEnabled || workplace == null || !workplace.isActiveAndEnabled
                || box == null || !box.gameObject.activeInHierarchy || deliveryPoint == null
                || !deliveryPoint.gameObject.activeInHierarchy || carryPoint == null))
                CancelShift();
        }

        public void CancelShift()
        {
            if (completing)
                return;
            ReleaseObjects();
            SetState(WarehouseTaskState.NoShift);
        }

        private void ReleaseObjects()
        {
            if (box != null) box.RemoveFromTask();
            if (deliveryPoint != null) deliveryPoint.Release(this);
            if (workplace != null) workplace.Release(this);
            box = null;
            deliveryPoint = null;
            workplace = null;
            reward = 0;
        }

        private void SetState(WarehouseTaskState state)
        {
            State = state;
            if (objectiveUI == null)
                return;
            objectiveUI.SetObjective(state switch
            {
                WarehouseTaskState.PickUpBox => "Objective: Pick up the box",
                WarehouseTaskState.DeliverBox => "Objective: Deliver the box",
                WarehouseTaskState.Complete => "Job complete",
                _ => "No active job"
            });
        }

        private void OnDisable() => CancelShift();
    }
}
