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

        /// <summary>Years added (positive) or forgiven (negative) by <see cref="Add"/> / <see cref="Forgive"/> — never by <see cref="Reset"/>.</summary>
        public event Action<int> Moved;

        public void Add(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years += years;
            if (years != 0) Moved?.Invoke(years);
        }

        public void Forgive(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            int before = Years;
            Years = Math.Max(0, Years - years);
            if (Years != before) Moved?.Invoke(Years - before);
        }

        public void Reset(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            Years = years;
        }
    }
}
