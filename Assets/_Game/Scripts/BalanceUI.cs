using UnityEngine;
using UnityEngine.UI;

namespace LifeSimulator
{
    public sealed class BalanceUI : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private Text balanceText;

        private void OnEnable()
        {
            if (wallet == null || balanceText == null)
            {
                Debug.LogError("Assign a player wallet and Text to BalanceUI.", this);
                enabled = false;
                return;
            }

            wallet.BalanceChanged += Refresh;
            Refresh(wallet.Balance);
        }

        private void OnDisable()
        {
            if (wallet != null)
                wallet.BalanceChanged -= Refresh;
        }

        private void Refresh(long balance) => balanceText.text = $"Balance: ${balance:N0}";
    }
}
