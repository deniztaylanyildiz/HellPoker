using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// Symmetric payouts: a winning hand forgives stake × the player's multiplier; a losing one adds
    /// stake × the house's multiplier × <see cref="LossPercent"/>. Winning with the absolution hand wipes the whole sentence.
    /// Folding adds a share of the stake that depends on whether the cards were already exchanged.
    /// Partial years always round up — the House never rounds in your favour.
    /// </summary>
    public sealed class PayoutTable : IPayoutTable, IPayoutInfo
    {
        public const int DefaultLossPercent = 100;
        public const int DefaultFoldPercentBeforeDraw = 50;
        public const int DefaultFoldPercentAfterDraw = 100;

        private readonly IReadOnlyDictionary<HandCategory, int> _multipliers;

        public HandCategory AbsolutionCategory { get; }
        public int LossPercent { get; }
        public int FoldPercentBeforeDraw { get; }
        public int FoldPercentAfterDraw { get; }

        public PayoutTable(IReadOnlyDictionary<HandCategory, int> multipliers, HandCategory absolutionCategory,
            int lossPercent = DefaultLossPercent, int foldPercentBeforeDraw = DefaultFoldPercentBeforeDraw,
            int foldPercentAfterDraw = DefaultFoldPercentAfterDraw)
        {
            _multipliers = multipliers ?? throw new ArgumentNullException(nameof(multipliers));
            if (_multipliers.Values.Any(m => m < 0)) throw new ArgumentOutOfRangeException(nameof(multipliers), "Multipliers cannot be negative.");
            if (lossPercent < 0) throw new ArgumentOutOfRangeException(nameof(lossPercent));
            if (foldPercentBeforeDraw < 0) throw new ArgumentOutOfRangeException(nameof(foldPercentBeforeDraw));
            if (foldPercentAfterDraw < 0) throw new ArgumentOutOfRangeException(nameof(foldPercentAfterDraw));

            AbsolutionCategory = absolutionCategory;
            LossPercent = lossPercent;
            FoldPercentBeforeDraw = foldPercentBeforeDraw;
            FoldPercentAfterDraw = foldPercentAfterDraw;
        }

        public static IReadOnlyDictionary<HandCategory, int> DefaultMultipliers => new Dictionary<HandCategory, int>
        {
            { HandCategory.HighCard, 1 },
            { HandCategory.OnePair, 1 },
            { HandCategory.TwoPair, 2 },
            { HandCategory.ThreeOfAKind, 3 },
            { HandCategory.Straight, 4 },
            { HandCategory.Flush, 5 },
            { HandCategory.FullHouse, 8 },
            { HandCategory.FourOfAKind, 10 },
            { HandCategory.StraightFlush, 15 },
            { HandCategory.RoyalFlush, 20 }
        };

        public static PayoutTable CreateDefault()
        {
            return new PayoutTable(DefaultMultipliers, HandCategory.DeadMansHand);
        }

        public bool IsAbsolution(HandCategory category) => category == AbsolutionCategory;

        /// <summary>
        /// The multiplier for a hand. The absolution hand has no entry of its own: it counts as the top multiplier
        /// (it only matters when the house wins with it — a player winning with it is absolved instead).
        /// </summary>
        public int GetMultiplier(HandCategory category)
        {
            if (_multipliers.TryGetValue(category, out int multiplier)) return multiplier;
            if (IsAbsolution(category) && _multipliers.Count > 0) return _multipliers.Values.Max();
            return 1;
        }

        public int GetYearsForgiven(HandCategory playerCategory, int stake, int currentYears)
        {
            if (IsAbsolution(playerCategory))
                return currentYears;

            return Math.Min(currentYears, stake * GetMultiplier(playerCategory));
        }

        public int GetYearsAdded(HandCategory houseCategory, int stake)
        {
            return PercentRoundedUp(stake * GetMultiplier(houseCategory), LossPercent);
        }

        public int GetFoldPenalty(int stake, bool afterDraw)
        {
            return PercentRoundedUp(stake, afterDraw ? FoldPercentAfterDraw : FoldPercentBeforeDraw);
        }

        public int GetLeastYearsForgiven(int stake, int currentYears)
        {
            return Math.Min(currentYears, stake * LowestMultiplier());
        }

        public int GetLeastYearsAdded(int stake)
        {
            return PercentRoundedUp(stake * LowestMultiplier(), LossPercent);
        }

        private int LowestMultiplier()
        {
            return _multipliers.Count > 0 ? _multipliers.Values.Min() : 1;
        }

        private static int PercentRoundedUp(int value, int percent)
        {
            return (int)(((long)value * percent + 99) / 100);
        }
    }
}
