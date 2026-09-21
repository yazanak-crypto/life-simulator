using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LifeSimulator.Editor
{
    // Repeatable component integration checks; test mutations are never saved.
    public static class DealershipPrototype
    {
        private const string ScenePath = "Assets/_Game/Scenes/PrototypeMovement.unity";
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + message);
            Debug.Log("PASS: " + message);
        }
        private static void Awake(Component component) => component.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(component, null);

        [MenuItem("Tools/Life Simulator/Validate dealership (reopens scene)")]
        public static void Validate()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            try
            {
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
                var wallet = player.GetComponent<PlayerWallet>();
                var purchase = UnityEngine.Object.FindFirstObjectByType<CarPurchase>();
                var car = GameObject.Find("Starter Car").transform;
                Vector3 displayPosition = car.position;
                Awake(wallet);
                Awake(player.GetComponent<WarehouseTask>());
                Check(wallet.Balance == 0, "Normal starting balance is $0");
                int events = 0;
                wallet.BalanceChanged += _ => events++;
                purchase.Interact(player);
                Check(wallet.Balance == 0 && events == 0 && purchase.Owner == null && car.position == displayPosition, "Insufficient $0 leaves balance, ownership and car unchanged");
                Check(GameObject.Find("Interaction Feedback").GetComponent<Text>().text == "Not enough money", "Insufficient funds feedback");
                var workplace = UnityEngine.Object.FindFirstObjectByType<Workplace>();
                var box = UnityEngine.Object.FindFirstObjectByType<WarehouseBox>(FindObjectsInactive.Include);
                var delivery = UnityEngine.Object.FindFirstObjectByType<WarehouseDelivery>();
                for (int i = 1; i <= 3; i++)
                {
                    workplace.Interact(player);
                    box.Interact(player);
                    delivery.Interact(player);
                    Check(wallet.Balance == i * 100 && player.GetComponent<WarehouseTask>().State == WarehouseTaskState.Complete, "Warehouse delivery " + i + " pays exactly $100");
                    if (i < 3)
                    {
                        int previousEvents = events;
                        purchase.Interact(player);
                        Check(wallet.Balance == i * 100 && events == previousEvents && purchase.Owner == null, "Insufficient balance $" + i * 100 + " unchanged");
                    }
                }
                int beforePurchaseEvents = events;
                wallet.BalanceChanged += _ => purchase.Interact(player);
                purchase.Interact(player);
                Check(wallet.Balance == 0 && events == beforePurchaseEvents + 1, "Exactly $300 spent with one balance event, including reentrant purchase attempt");
                Check(purchase.IsOwnedBy(player), "Car owned by purchasing player");
                Check(GameObject.Find("Interaction Feedback").GetComponent<Text>().text == "Starter Car purchased!", "Success feedback");
                Check(car.position == player.GetComponent<PlayerParking>().CarSpot.position && car.position != displayPosition && car.Find("Owned Indicator").gameObject.activeSelf, "Car moved to assigned parking with visible OWNED indicator");
                wallet.TryAddMoney(600);
                purchase.Interact(player);
                Check(wallet.Balance == 600, "Repeat purchase does not charge even with sufficient funds");
                var other = UnityEngine.Object.Instantiate(player.gameObject);
                var otherPlayer = other.GetComponent<PlayerInteraction>();
                var otherWallet = other.GetComponent<PlayerWallet>();
                otherWallet.TryAddMoney(600);
                long otherBefore = otherWallet.Balance;
                purchase.Interact(otherPlayer);
                Check(!purchase.IsOwnedBy(otherPlayer) && purchase.Owner == player && otherWallet.Balance == otherBefore, "Different player neither owns nor can repurchase sold car");
                UnityEngine.Object.DestroyImmediate(other);
                workplace.Interact(player);
                box.Interact(player);
                delivery.Interact(player);
                Check(wallet.Balance == 700, "Warehouse still pays $100 after purchase");
                Debug.Log("DEALERSHIP VALIDATION PASSED (editor component integration; manual visual/input checks still required).");
            }
            finally
            {
                // Discard test mutations, leaving saved scene's balance $0 and car for sale.
                EditorSceneManager.OpenScene(ScenePath);
            }
        }
    }
}

