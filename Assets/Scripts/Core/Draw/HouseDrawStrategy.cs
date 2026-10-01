using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Draw
{
    /// <summary>
    /// Plain casino-style play: stand on made hands, keep pairs and better, chase four-card flushes and straights,
    /// otherwise keep the two highest cards.
    /// </summary>
    public sealed class HouseDrawStrategy : IDrawStrategy
    {
        private readonly int _maxDiscards;

        public HouseDrawStrategy(int maxDiscards = MaxDiscardPolicy.ClassicLimit)
        {
            if (maxDiscards < 0 || maxDiscards > Hand.Size)
                throw new ArgumentOutOfRangeException(nameof(maxDiscards));
            _maxDiscards = maxDiscards;
        }

        public IReadOnlyList<int> ChooseDiscards(Hand hand)
        {
            if (hand == null) throw new ArgumentNullException(nameof(hand));

            var analysis = new HandAnalysis(hand);
            IEnumerable<int> discards = PickDiscards(hand, analysis);

            // If the plan needs more discards than allowed, throw away the lowest cards first.
            return discards
                .OrderBy(index => hand[index].Rank)
                .Take(_maxDiscards)
                .OrderBy(index => index)
                .ToArray();
        }

        private static IEnumerable<int> PickDiscards(Hand hand, HandAnalysis analysis)
        {
            int[] indices = Enumerable.Range(0, Hand.Size).ToArray();
            int largestGroup = analysis.Groups[0].Count;
            bool isFullHouse = largestGroup == 3 && analysis.Groups[1].Count == 2;

            if (analysis.IsStraight || analysis.IsFlush || largestGroup == 4 || isFullHouse)
                return Enumerable.Empty<int>();

            if (largestGroup >= 2)
            {
                var pairedRanks = new HashSet<Rank>(analysis.Groups.Where(group => group.Count >= 2).Select(group => group.Rank));
                return indices.Where(index => !pairedRanks.Contains(hand[index].Rank));
            }

            int? outlier = FindFlushDrawOutlier(hand) ?? FindStraightDrawOutlier(hand);
            if (outlier.HasValue)
                return new[] { outlier.Value };

            return indices.OrderByDescending(index => hand[index].Rank).Skip(2);
        }

        private static int? FindFlushDrawOutlier(Hand hand)
        {
            var flushSuit = hand.GroupBy(card => card.Suit).FirstOrDefault(group => group.Count() == Hand.Size - 1);
            if (flushSuit == null) return null;

            return Enumerable.Range(0, Hand.Size).First(index => hand[index].Suit != flushSuit.Key);
        }

        /// <summary>Finds the card whose removal leaves four ranks inside one five-rank window (open-ended or gutshot).</summary>
        private static int? FindStraightDrawOutlier(Hand hand)
        {
            for (int skip = 0; skip < Hand.Size; skip++)
            {
                int[] ranks = Enumerable.Range(0, Hand.Size)
                    .Where(index => index != skip)
                    .Select(index => (int)hand[index].Rank)
                    .ToArray();

                if (FitsStraightWindow(ranks) || FitsStraightWindow(ranks.Select(rank => rank == (int)Rank.Ace ? 1 : rank)))
                    return skip;
            }

            return null;
        }

        private static bool FitsStraightWindow(IEnumerable<int> ranks)
        {
            int[] distinct = ranks.Distinct().ToArray();
            return distinct.Length == Hand.Size - 1 && distinct.Max() - distinct.Min() <= Hand.Size - 1;
        }
    }
}
