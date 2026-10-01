using System;
using System.Collections.Generic;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// A winning hand forgives stake × multiplier years. Winning with the absolution hand wipes the whole sentence.
    /// A loss adds <see cref="LossPercent"/> of the stake (default all of it); folding adds <see cref="FoldPercent"/> (default half).
    /// Partial years always round up — the House never rounds in your favour.
    /// </summary>
    public sealed class PayoutTable : IPayoutTable, IPayoutInfo
    {
        public const int DefaultLossPercent = 100;
        public const int DefaultFoldPercent = 50;

        private readonly IReadOnlyDictionary<HandCategory, int> _multipliers;

        public HandCategory AbsolutionCategory { get; }
        public int LossPercent { get; }
        public int FoldPercent { get; }

        public PayoutTable(IReadOnlyDictionary<HandCategory, int> multipliers, HandCategory absolutionCategory,
            int lossPercent = DefaultLossPercent, int foldPercent = DefaultFoldPercent)
        {
            _multipliers = multipliers ?? throw new ArgumentNullException(nameof(multipliers));
            if (lossPercent < 0) throw new ArgumentOutOfRangeException(nameof(lossPercent));
            if (foldPercent < 0) throw new ArgumentOutOfRangeException(nameof(foldPercent));

            AbsolutionCategory = absolutionCategory;
            LossPercent = lossPercent;
            FoldPercent = foldPercent;
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
            { HandCategory.FourOfAKind, 25 },
            { HandCategory.StraightFlush, 50 },
            { HandCategory.RoyalFlush, 100 }
        };

        public static PayoutTable CreateDefault()
        {
            return new PayoutTable(DefaultMultipliers, HandCategory.DeadMansHand);
        }

        public bool IsAbsolution(HandCategory category) => category == AbsolutionCategory;

        public int GetMultiplier(HandCategory category)
        {
            return _multipliers.TryGetValue(category, out int multiplier) ? multiplier : 1;
        }

        public int GetYearsForgiven(HandCategory playerCategory, int stake, int currentYears)
        {
            if (IsAbsolution(playerCategory))
                return currentYears;

            return Math.Min(currentYears, stake * GetMultiplier(playerCategory));
        }

        public int GetYearsAdded(HandCategory houseCategory, int stake)
        {
            return PercentRoundedUp(stake, LossPercent);
        }

        public int GetFoldPenalty(int stake)
        {
            return PercentRoundedUp(stake, FoldPercent);
        }

        private static int PercentRoundedUp(int value, int percent)
        {
            return (value * percent + 99) / 100;
        }
    }
}
