using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Draw
{
    /// <summary>
    /// Plain casino-style play: stand on made hands, keep pairs and better, chase four-card flushes and straights,
    /// otherwise keep the two highest cards. A joker is kept and planned as the best card it can be; a second joker (or more) is
    /// thrown back first — two at the showdown lose the hand.
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

            int[] jokers = Enumerable.Range(0, Hand.Size).Where(i => hand[i].IsJoker).ToArray();
            Hand plan = jokers.Length == 0 ? hand : PlanningHand(hand, jokers);
            var analysis = new HandAnalysis(plan);
            IEnumerable<int> discards = PickDiscards(plan, analysis);
            if (jokers.Length > 0)
                discards = discards.Where(index => index != jokers[0]).Concat(jokers.Skip(1)).Distinct();

            // If the plan needs more discards than allowed, throw away the lowest cards first (extra jokers, rankless, before all).
            return discards
                .OrderBy(index => hand[index].Rank)
                .Take(_maxDiscards)
                .OrderBy(index => index)
                .ToArray();
        }

        /// <summary>
        /// The hand to plan with: the first joker as the best card it can be, the extra jokers (which go back anyway) as low
        /// cards of new ranks that join nothing.
        /// </summary>
        private static Hand PlanningHand(Hand hand, int[] jokers)
        {
            var cards = hand.ToArray();
            var ranks = new HashSet<Rank>(cards.Where(card => !card.IsJoker).Select(card => card.Rank));
            Suit common = cards.Where(card => !card.IsJoker).GroupBy(card => card.Suit).OrderByDescending(g => g.Count())
                .Select(g => g.Key).DefaultIfEmpty(Suit.Spades).First();
            Suit off = common == Suit.Clubs ? Suit.Diamonds : Suit.Clubs;
            foreach (int extra in jokers.Skip(1))
            {
                Rank filler = Enum.GetValues(typeof(Rank)).Cast<Rank>().OrderBy(r => r).First(r => !ranks.Contains(r));
                ranks.Add(filler);
                cards[extra] = new Card(filler, off);
            }
            return JokerResolver.Resolve(new Hand(cards));
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
