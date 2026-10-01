using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation
{
    /// <summary>Result of evaluating a hand. Compares by category, then by tie-breaker ranks in order.</summary>
    public sealed class HandEvaluation : IComparable<HandEvaluation>
    {
        public Hand Hand { get; }
        public HandCategory Category { get; }

        /// <summary>Ranks compared in order when categories are equal (e.g. pair rank, then kickers).</summary>
        public IReadOnlyList<Rank> TieBreakers { get; }

        public HandEvaluation(Hand hand, HandCategory category, IEnumerable<Rank> tieBreakers)
        {
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            Category = category;
            TieBreakers = (tieBreakers ?? Enumerable.Empty<Rank>()).ToArray();
        }

        public int CompareTo(HandEvaluation other)
        {
            if (other == null) return 1;

            int byCategory = Category.CompareTo(other.Category);
            if (byCategory != 0) return byCategory;

            int length = Math.Min(TieBreakers.Count, other.TieBreakers.Count);
            for (int i = 0; i < length; i++)
            {
                int byRank = TieBreakers[i].CompareTo(other.TieBreakers[i]);
                if (byRank != 0) return byRank;
            }

            return 0;
        }

        public static bool operator >(HandEvaluation left, HandEvaluation right) => Compare(left, right) > 0;
        public static bool operator <(HandEvaluation left, HandEvaluation right) => Compare(left, right) < 0;

        private static int Compare(HandEvaluation left, HandEvaluation right)
        {
            if (left == null) return right == null ? 0 : -1;
            return left.CompareTo(right);
        }

        public override string ToString()
        {
            return $"{Category} [{Hand}]";
        }
    }
}
