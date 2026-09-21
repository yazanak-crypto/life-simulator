using UnityEngine;

namespace LifeSimulator
{
    public sealed class CarPurchase : MonoBehaviour, IInteractable
    {
        // TEMPORARY prototype balancing value. Serialized rather than const so it can be
        // tuned per vehicle without recompiling, and so other assemblies do not bake it in.
        [SerializeField, Min(1)] private int price = 2000;
        public int Price => price;
        [SerializeField] private Transform vehicle;
        [SerializeField] private TextMesh saleSign;
        [SerializeField] private GameObject ownedIndicator;

        // Per-asset runtime ownership. A future persistent player ID can replace this reference.
        public PlayerInteraction Owner { get; private set; }
        private bool sold;
        private bool purchasing;
        public bool IsOwnedBy(PlayerInteraction player) => sold && player != null && Owner == player;
        public string Prompt => sold ? "Starter Car — SOLD" : "Buy Starter Car ($" + price.ToString("N0") + ")";

        public void Interact(PlayerInteraction player)
        {
            if (!isActiveAndEnabled || purchasing || player == null || !player.isActiveAndEnabled)
                return;
            if (sold)
            {
                player.ShowFeedback(IsOwnedBy(player) ? "You already own this car" : "This car is already owned");
                return;
            }
            if (!player.TryGetComponent(out PlayerWallet wallet) || !wallet.isActiveAndEnabled
                || !player.TryGetComponent(out PlayerParking parking) || parking.CarSpot == null
                || vehicle == null || saleSign == null || ownedIndicator == null)
            {
                player.ShowFeedback("Purchase unavailable");
                return;
            }

            // Wallet events are synchronous; guard reentrant purchases as well as repeated E presses.
            purchasing = true;
            try
            {
                if (!wallet.TrySpendMoney(Price))
                {
                    player.ShowFeedback("Not enough money");
                    return;
                }
                Owner = player;
                sold = true; // Remains sold even if the runtime owner object is later destroyed.
                vehicle.SetPositionAndRotation(parking.CarSpot.position, parking.CarSpot.rotation);
                ownedIndicator.SetActive(true);
                saleSign.text = "STARTER CAR\nSOLD";
                player.ShowFeedback("Starter Car purchased!");
            }
            finally
            {
                purchasing = false;
            }
        }
    }
}
