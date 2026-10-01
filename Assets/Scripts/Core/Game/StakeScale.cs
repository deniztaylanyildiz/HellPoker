using System;
using System.Collections.Generic;
using System.Linq;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// How big the bets are, measured against the sentence at the start of the hand.
    /// One betting unit is a tenth of the sentence, rounded down to a readable step
    /// (≥1000 → steps of 100, ≥500 → 50, ≥250 → 25, below → 10), never less than <see cref="MinimumUnit"/>.
    /// Everything on the table together may be at most <see cref="TableCapPercent"/> of the sentence.
    /// </summary>
    public sealed class StakeScale
    {
        /// <summary>A sentence of at least <see cref="FromYears"/> bets in multiples of <see cref="Step"/>.</summary>
        public readonly struct Tier
        {
            public int FromYears { get; }
            public int Step { get; }

            public Tier(int fromYears, int step)
            {
                if (fromYears < 0) throw new ArgumentOutOfRangeException(nameof(fromYears));
                if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
                FromYears = fromYears;
                Step = step;
            }
        }

        public int Divisor { get; }
        public int MinimumUnit { get; }
        public int TableCapPercent { get; }

        /// <summary>Highest threshold first.</summary>
        public IReadOnlyList<Tier> Tiers { get; }

        public StakeScale(int divisor = 10, int minimumUnit = 10, int tableCapPercent = 50, IEnumerable<Tier> tiers = null)
        {
            if (divisor <= 0) throw new ArgumentOutOfRangeException(nameof(divisor));
            if (minimumUnit <= 0) throw new ArgumentOutOfRangeException(nameof(minimumUnit));
            if (tableCapPercent <= 0 || tableCapPercent > 100) throw new ArgumentOutOfRangeException(nameof(tableCapPercent));

            Divisor = divisor;
            MinimumUnit = minimumUnit;
            TableCapPercent = tableCapPercent;
            Tiers = (tiers ?? new[] { new Tier(1000, 100), new Tier(500, 50), new Tier(250, 25), new Tier(0, 10) })
                .OrderByDescending(tier => tier.FromYears)
                .ToArray();
            if (Tiers.Count == 0) throw new ArgumentException("At least one tier is needed.", nameof(tiers));
        }

        public static StakeScale Default => new StakeScale();

        /// <summary>The betting unit for a hand started with this sentence: 1000 → 100, 650 → 50, 340 → 25, 180 → 10.</summary>
        public int UnitFor(int years)
        {
            int step = StepFor(years);
            int unit = years / Divisor / step * step;
            return Math.Max(MinimumUnit, unit);
        }

        /// <summary>The ante actually put down: one unit, or everything that is left when the sentence is smaller (all in).</summary>
        public int AnteFor(int years)
        {
            return Math.Max(0, Math.Min(UnitFor(years), years));
        }

        /// <summary>The most that may be on the table in a hand started with this sentence. Never below the ante, never above the sentence.</summary>
        public int CapFor(int years)
        {
            int cap = years * TableCapPercent / 100;
            return Math.Min(years, Math.Max(cap, AnteFor(years)));
        }

        private int StepFor(int years)
        {
            foreach (Tier tier in Tiers)
            {
                if (years >= tier.FromYears)
                    return tier.Step;
            }
            return Tiers[Tiers.Count - 1].Step;
        }
    }
}
