using System;

namespace HellPoker.Core.Game
{
    /// <summary>The player's remaining sentence in years. Never goes below zero.</summary>
    public sealed class PunishmentLedger
    {
        public int Years { get; private set; }

        public PunishmentLedger(int startingYears)
        {
            Reset(startingYears);
        }

        public bool IsServed => Years == 0;

        public void Add(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years += years;
        }

        public void Forgive(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years = Math.Max(0, Years - years);
        }

        public void Reset(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years = years;
        }
    }
}
