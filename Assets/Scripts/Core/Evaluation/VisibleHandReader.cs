using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation
{
    /// <summary>
    /// Names what the face-up cards already make. With all five showing it is the full evaluation (the Dead Man's Hand
    /// included); with fewer, only cards of a rank can combine yet (pairs, two pair, trips, quads) — straights and
    /// flushes need all five.
    /// </summary>
    public static class VisibleHandReader
    {
        /// <returns>Null when no card is showing.</returns>
        public static HandCategory? Read(IReadOnlyList<Card> faceUp, IHandEvaluator evaluator)
        {
            if (faceUp == null) throw new ArgumentNullException(nameof(faceUp));
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            if (faceUp.Count == 0) return null;
            if (faceUp.Count >= Hand.Size) return evaluator.Evaluate(new Hand(faceUp.Take(Hand.Size))).Category;

            int[] groups = faceUp.GroupBy(card => card.Rank).Select(group => group.Count()).OrderByDescending(count => count).ToArray();
            if (groups[0] >= 4) return HandCategory.FourOfAKind;
            if (groups[0] == 3) return HandCategory.ThreeOfAKind;
            if (groups[0] == 2) return groups.Length > 1 && groups[1] == 2 ? HandCategory.TwoPair : HandCategory.OnePair;
            return HandCategory.HighCard;
        }
    }
}
