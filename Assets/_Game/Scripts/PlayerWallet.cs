using System;
using UnityEngine;

namespace LifeSimulator
{
    public sealed class PlayerWallet : MonoBehaviour
    {
        // Whole dollars for V0.1. Runtime-only state starts at zero for each player.
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
