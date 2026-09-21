using System;
using UnityEngine;

namespace LifeSimulator
{
    public sealed class PlayerWallet : MonoBehaviour
    {
        [SerializeField, Min(0), Tooltip("Editor/development builds only. Leave at zero for normal gameplay.")]
        private int developmentStartingBalance;

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TryAddMoney(Math.Max(0, developmentStartingBalance));
#endif
        }

        // Whole dollars for V0.1. Runtime-only state belongs to this player.
        public long Balance { get; private set; }
        public event Action<long> BalanceChanged;

        public bool TryAddMoney(long amount)
        {
            if (amount < 0 || amount > long.MaxValue - Balance)
                return false;
            if (amount == 0)
                return true;

            Balance += amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public bool CanAfford(long amount) => amount >= 0 && amount <= Balance;

        public bool TrySpendMoney(long amount)
        {
            if (!CanAfford(amount))
                return false;
            if (amount == 0)
                return true;

            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }
    }
}
