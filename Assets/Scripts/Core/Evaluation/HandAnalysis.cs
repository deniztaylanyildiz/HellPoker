using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation
{
    /// <summary>Precomputed facts about a hand, shared by all rules so each rule stays a simple predicate.</summary>
    public sealed class HandAnalysis
    {
        public Hand Hand { get; }

        /// <summary>Ranks grouped by occurrence, ordered by group size then rank (e.g. full house: [K,K,K],[4,4]).</summary>
        public IReadOnlyList<RankGroup> Groups { get; }

        public bool IsFlush { get; }
        public bool IsStraight { get; }

        /// <summary>Top card of the straight; Five for the A-2-3-4-5 wheel. Only meaningful if <see cref="IsStraight"/>.</summary>
        public Rank StraightHighRank { get; }

        public HandAnalysis(Hand hand)
        {
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));

            Groups = hand
                .GroupBy(card => card.Rank)
                .Select(group => new RankGroup(group.Key, group.Count()))
                .OrderByDescending(group => group.Count)
                .ThenByDescending(group => group.Rank)
                .ToArray();

            IsFlush = hand.Select(card => card.Suit).Distinct().Count() == 1;

            if (Groups.Count == Hand.Size)
            {
                Rank highest = Groups[0].Rank;
                Rank lowest = Groups[Hand.Size - 1].Rank;

                if (highest - lowest == Hand.Size - 1)
                {
                    IsStraight = true;
                    StraightHighRank = highest;
                }
                else if (highest == Rank.Ace && Groups[1].Rank == Rank.Five)
                {
                    IsStraight = true;
                    StraightHighRank = Rank.Five;
                }
            }
        }

        public IEnumerable<int> GroupSizes => Groups.Select(group => group.Count);

        /// <summary>Ranks in tie-break order: biggest groups first, higher ranks first within equal sizes.</summary>
        public IEnumerable<Rank> GroupRanks => Groups.Select(group => group.Rank);
    }

    public readonly struct RankGroup
    {
        public Rank Rank { get; }
        public int Count { get; }

        public RankGroup(Rank rank, int count)
        {
            Rank = rank;
            Count = count;
        }
    }
}
