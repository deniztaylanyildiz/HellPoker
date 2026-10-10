using System;

namespace HellPoker.Core.Chapters
{
    /// <summary>The coins of one side on the floors: the run's, or an imp's. Never below zero — an empty purse loses the match.</summary>
    public sealed class CoinPurse
    {
        public int Coins { get; private set; }

        public bool IsEmpty => Coins == 0;

        public CoinPurse(int coins = 0)
        {
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins), "A purse holds no debt.");
            Coins = coins;
        }

        /// <summary>Coins in (positive) or out (negative), never more out than the purse holds; returns what really moved.</summary>
        public int Add(int coins)
        {
            int moved = Math.Max(coins, -Coins);
            Coins += moved;
            return moved;
        }

        /// <summary>Pays <paramref name="coins"/> if the purse holds them.</summary>
        public bool TrySpend(int coins)
        {
            if (coins < 0 || Coins < coins) return false;
            Coins -= coins;
            return true;
        }
    }
}
