using System;
using UnityEngine;

namespace LifeSimulator
{
    // Issues warehouse orders by composing scene-tagged racks and drop-offs at runtime, so new
    // orders need no hand-authoring: add a rack or a drop-off and it joins the pool.
    public sealed class ShiftAssignments : MonoBehaviour
    {
        [SerializeField] private WarehouseBox boxPrefab;
        [SerializeField] private WarehouseRack[] racks;
        [SerializeField] private WarehouseDropOff[] dropOffs;
        [SerializeField] private ObjectiveUI objectiveUI;
        [SerializeField, Min(1), Tooltip("Orders required before the shift's work is complete. Tune by playing.")]
        private int assignmentsPerShift = 6;

        // One active worker for v0.1. A dictionary keyed by worker replaces this for multiplayer;
        // nothing here is static, so that change stays local to this component.
        private PlayerEmployment worker;
        private int nextOrderNumber;
        private WarehouseRack lastRack;

        public int AssignmentsPerShift => assignmentsPerShift;
        public Assignment Current { get; private set; }
        public int Completed { get; private set; }
        public bool QuotaComplete => Completed >= assignmentsPerShift;

        // Raised the moment the last order is delivered, so JobSite can stop wages accruing.
        public event Action QuotaCompleted;

        public bool IsReady => boxPrefab != null && racks != null && racks.Length > 0
            && dropOffs != null && dropOffs.Length > 0;

        internal void BeginShift(PlayerEmployment employment)
        {
            if (employment == null || !IsReady) return;
            EndShift();
            worker = employment;
            Completed = 0;
            lastRack = null;
            nextOrderNumber = UnityEngine.Random.Range(100, 900);
            IssueNext();
        }

        internal void EndShift()
        {
            DiscardCurrent();
            worker = null;
            Completed = 0;
            if (objectiveUI != null) objectiveUI.SetObjective(string.Empty);
        }

        private void DiscardCurrent()
        {
            if (Current == null) return;
            if (Current.Box != null)
            {
                // Whoever is holding it must let go before the instance disappears.
                if (Current.Box.Item != null && Current.Box.Item.Holder != null)
                    Current.Box.Item.Holder.Release();
                Discard(Current.Box.gameObject);
            }
            Current = null;
        }

        private void IssueNext()
        {
            if (worker == null || !IsReady || QuotaComplete) return;

            WarehouseRack rack = PickRack();
            WarehouseDropOff dropOff = dropOffs[UnityEngine.Random.Range(0, dropOffs.Length)];
            lastRack = rack;

            WarehouseBox box = Instantiate(boxPrefab, rack.BoxAnchor.position, rack.BoxAnchor.rotation);
            box.name = "Warehouse Box (Order)";
            box.Source = this;
            Current = new Assignment(nextOrderNumber++, rack, dropOff, box);
            Refresh();
        }

        // Avoid sending the player straight back to the rack they just left.
        private WarehouseRack PickRack()
        {
            if (racks.Length == 1) return racks[0];
            for (int attempt = 0; attempt < 8; attempt++)
            {
                WarehouseRack candidate = racks[UnityEngine.Random.Range(0, racks.Length)];
                if (candidate != null && candidate != lastRack) return candidate;
            }
            return racks[0];
        }

        private void Refresh()
        {
            if (objectiveUI == null) return;
            objectiveUI.SetObjective(Current != null
                ? Current.Describe(Completed, assignmentsPerShift)
                : "Work complete — clock out at the time clock");
        }

        internal void TryPickUp(WarehouseBox box, PlayerInteraction player)
        {
            if (Current == null || box != Current.Box || Current.State != AssignmentState.Issued
                || !IsWorker(player) || !player.TryGetComponent(out PlayerCarry carry))
                return;
            if (carry.IsCarrying)
            {
                player.ShowFeedback("You are already carrying a box");
                return;
            }
            if (!carry.TryPickUp(box.Item)) return;
            Current.State = AssignmentState.Carrying;
            Refresh();
        }

        internal void TryDeliver(WarehouseDropOff dropOff, PlayerInteraction player)
        {
            if (Current == null || Current.State != AssignmentState.Carrying || !IsWorker(player)
                || !player.TryGetComponent(out PlayerCarry carry)
                || Current.Box == null || !carry.IsHolding(Current.Box.Item))
                return;
            if (dropOff != Current.DropOff)
            {
                player.ShowFeedback("Wrong destination — this order goes to " + Current.DropOff.DisplayName);
                return;
            }

            carry.Release();
            Current.State = AssignmentState.Delivered;
            Discard(Current.Box.gameObject);
            Current = null;
            Completed++;

            if (QuotaComplete)
            {
                Refresh();
                player.ShowFeedback("All orders complete — clock out when you are ready");
                QuotaCompleted?.Invoke();
                return;
            }

            IssueNext();
            player.ShowFeedback("Order delivered");
        }

        // Edit-mode validation runs outside Play, where Object.Destroy is not permitted.
        private static void Discard(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        private bool IsWorker(PlayerInteraction player) =>
            worker != null && player != null && worker.Worker == player;

        // A worker who disappears mid-shift must not leave an orphaned box in the world.
        private void Update()
        {
            if (worker != null && (!worker.isActiveAndEnabled || !worker.IsClockedIn)) EndShift();
        }

        private void OnDisable() => EndShift();
    }
}
