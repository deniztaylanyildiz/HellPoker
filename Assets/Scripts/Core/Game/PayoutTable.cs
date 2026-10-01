using System;
using System.Collections.Generic;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// A winning hand forgives stake × multiplier years. Winning with the absolution hand wipes the whole sentence.
    /// A loss always adds the stake; folding adds half of it.
    /// </summary>
    public sealed class PayoutTable : IPayoutTable, IPayoutInfo
    {
        private readonly IReadOnlyDictionary<HandCategory, int> _multipliers;

        public HandCategory AbsolutionCategory { get; }

        public PayoutTable(IReadOnlyDictionary<HandCategory, int> multipliers, HandCategory absolutionCategory)
        {
            _multipliers = multipliers ?? throw new ArgumentNullException(nameof(multipliers));
            AbsolutionCategory = absolutionCategory;
        }

        public static PayoutTable CreateDefault()
        {
            return new PayoutTable(new Dictionary<HandCategory, int>
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
            }, HandCategory.DeadMansHand);
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
            return stake;
        }

        /// <summary>Folding costs half the stake, rounded up.</summary>
        public int GetFoldPenalty(int stake)
        {
            return (stake + 1) / 2;
        }
    }
}
