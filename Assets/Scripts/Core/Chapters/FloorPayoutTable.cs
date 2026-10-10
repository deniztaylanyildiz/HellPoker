using System;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// A floor's payouts, in coins: the chapter demon's hand multipliers, but never above <see cref="Cap"/> (×3), one to one on
    /// raises as at every table. A loss is the pot (no loss surcharge), a fold leaves the stake on the table (no more), and no hand
    /// wipes anything out — the Dead Man's Hand pays as the capped multiplier. The demon's table keeps its own payouts for years.
    /// </summary>
    public sealed class FloorPayoutTable : IPayoutTable, IPayoutInfo
    {
        private readonly IPayoutInfo _source;

        public int Cap { get; }

        public FloorPayoutTable(IPayoutInfo source, int cap)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            if (cap < 1) throw new ArgumentOutOfRangeException(nameof(cap));
            Cap = cap;
        }

        public bool IsAbsolution(HandCategory category) => false;

        public int GetMultiplier(HandCategory category) => Math.Max(1, Math.Min(Cap, _source.GetMultiplier(category)));

        public int LossPercent => 100;
        public int FoldPercentBeforeDraw => 100;
        public int FoldPercentAfterDraw => 100;

        public int GetYearsForgiven(HandCategory playerCategory, int stake, int ante, int currentYears) =>
            Math.Min(currentYears, Settlement(stake, ante, GetMultiplier(playerCategory)));

        public int GetYearsAdded(HandCategory houseCategory, int stake, int ante, int surchargePercent = 100) =>
            Surcharged(Settlement(stake, ante, GetMultiplier(houseCategory)), surchargePercent);

        public int GetFoldPenalty(int stake, bool afterDraw, int surchargePercent = 100)
        {
            if (stake < 0) throw new ArgumentOutOfRangeException(nameof(stake));
            return Surcharged(stake, surchargePercent);
        }

        public int GetLeastYearsForgiven(int stake, int ante, int currentYears) => Math.Min(currentYears, Settlement(stake, ante, 1));

        public int GetLeastYearsAdded(int stake, int ante, int surchargePercent = 100) => Surcharged(Settlement(stake, ante, 1), surchargePercent);

        private static int Surcharged(int coins, int surchargePercent)
        {
            if (surchargePercent < 0) throw new ArgumentOutOfRangeException(nameof(surchargePercent));
            return (int)(((long)coins * surchargePercent + 99) / 100);
        }

        private static int Settlement(int stake, int ante, int multiplier)
        {
            if (stake < 0) throw new ArgumentOutOfRangeException(nameof(stake));
            if (ante < 0 || ante > stake) throw new ArgumentOutOfRangeException(nameof(ante), "The ante is part of the stake.");
            return stake + ante * (multiplier - 1);
        }
    }
}
