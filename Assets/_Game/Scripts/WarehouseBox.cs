using UnityEngine;

namespace LifeSimulator
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WarehouseBox : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform pickupPoint;
        private WarehouseTask assignedTask;

        public string Prompt => "Pick Up Box";
        internal bool IsAvailable => assignedTask == null && pickupPoint != null;

        public void Interact(PlayerInteraction player)
        {
            if (isActiveAndEnabled && assignedTask != null && assignedTask.Owner == player)
                assignedTask.TryPickUp(this);
        }

        internal void MakeAvailable(WarehouseTask task)
        {
            assignedTask = task;
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(pickupPoint.position, pickupPoint.rotation);
            GetComponent<BoxCollider>().enabled = true;
            enabled = true;
            gameObject.SetActive(true);
        }

        internal void CarryAt(Transform carryPoint)
        {
            // This placeholder has no Rigidbody. Disabling its collider prevents
            // interference with the controller, camera casts, and delivery targeting.
            GetComponent<BoxCollider>().enabled = false;
            enabled = false;
            transform.SetParent(carryPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        internal bool IsCarriedAt(Transform carryPoint) =>
            gameObject.activeInHierarchy && transform.parent == carryPoint && !GetComponent<BoxCollider>().enabled;

        internal void RemoveFromTask()
        {
            assignedTask = null;
            transform.SetParent(null, true);
            gameObject.SetActive(false);
        }
    }
}
