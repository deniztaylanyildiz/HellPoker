namespace HellPoker.Core.Chapters
{
    /// <summary>The run's coins on the floors. Below zero is a debt (no limit): it is paid in years at the gate.</summary>
    public sealed class CoinPurse
    {
        public int Coins { get; private set; }

        public bool InDebt => Coins < 0;

        public CoinPurse(int coins = 0)
        {
            Coins = coins;
        }

        /// <summary>Coins in (positive) or out (negative); a debt may grow without limit.</summary>
        public void Add(int coins) => Coins += coins;

        /// <summary>Pays <paramref name="coins"/> if the purse holds them (a purchase is never made on debt).</summary>
        public bool TrySpend(int coins)
        {
            if (coins < 0 || Coins < coins) return false;
            Coins -= coins;
            return true;
        }
    }
}
