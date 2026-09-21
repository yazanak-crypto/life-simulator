using UnityEngine;

namespace LifeSimulator
{
    // Physical attach/detach only. Deliberately job-agnostic so groceries or furniture can reuse
    // it later without depending on warehouse code.
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CarryableItem : MonoBehaviour
    {
        private BoxCollider body;
        public PlayerCarry Holder { get; private set; }
        public bool IsCarried => Holder != null;

        private void Awake() => body = GetComponent<BoxCollider>();

        internal void AttachTo(PlayerCarry carry)
        {
            if (body == null) body = GetComponent<BoxCollider>();
            Holder = carry;
            // No Rigidbody on this placeholder; disabling the collider keeps it clear of the
            // character controller, camera casts and interaction raycasts while carried.
            body.enabled = false;
            transform.SetParent(carry.CarryPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        internal void Detach()
        {
            if (body == null) body = GetComponent<BoxCollider>();
            Holder = null;
            transform.SetParent(null, true);
            body.enabled = true;
        }

        public bool IsCarriedBy(PlayerCarry carry) =>
            carry != null && Holder == carry && transform.parent == carry.CarryPoint;
    }
}
