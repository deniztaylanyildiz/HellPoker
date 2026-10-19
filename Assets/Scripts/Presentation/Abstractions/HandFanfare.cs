using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// Demo 3.1: how loudly a chapter table's hand ends, by the hand's strength. Every tier is plainly more than the one before it.
    /// A fold and a push play nothing.
    /// </summary>
    public enum HandTier
    {
        /// <summary>A fold, a push, a loss to a small hand: nothing.</summary>
        None,

        /// <summary>A win with a high card or a pair (or the imp folding): coins fly to the purse, the sinner smiles.</summary>
        Plain,

        /// <summary>Two pair or trips: the winning cards hit, the hand's name, more coins.</summary>
        Good,

        /// <summary>A straight or a flush: the five cards light up one by one, the name, a burst of coins, the screen's edge glows.</summary>
        Strong,

        /// <summary>A full house: the trips and the pair hit apart, a burst of coins, a short shake.</summary>
        Full,

        /// <summary>Four of a kind: a white flash (a sprite, not alpha), the name, a rain of coins; the demon fumes.</summary>
        Quads,

        /// <summary>A straight flush, a royal flush (and the Dead Man's Hand on a floor): the whole screen — darkness, a burning edge, the
        /// cards catching fire one by one, the name, the sinner's joy; the longest.</summary>
        Blaze,

        /// <summary>Lost to the House's trips or better: the House's winning cards hit on its own side.</summary>
        HouseStrong
    }

    /// <summary>A hand's ending as the table plays it: its tier, the name to write big (or none), the cards that hit, group by group.</summary>
    public sealed class HandFanfare
    {
        public HandTier Tier { get; }

        /// <summary>The hand's name in big letters (Good and up); null for none.</summary>
        public string Title { get; }

        /// <summary>The cards that hit, group by group (indices into the winner's row: the House's for <see cref="HandTier.HouseStrong"/>).</summary>
        public IReadOnlyList<IReadOnlyList<int>> Groups { get; }

        public bool OnHouseSide => Tier == HandTier.HouseStrong;

        /// <summary>The coins fly to the demon's bar (his table) rather than the purse (a floor).</summary>
        public bool CoinsToBar { get; }

        public HandFanfare(HandTier tier, string title, IReadOnlyList<IReadOnlyList<int>> groups, bool coinsToBar = false)
        {
            Tier = tier;
            Title = title;
            Groups = groups ?? Array.Empty<IReadOnlyList<int>>();
            CoinsToBar = coinsToBar;
        }

        /// <summary>
        /// The tier of a hand's end: a win by the player's hand (jokers counted as the cards they became; no hand shown — the imp folded,
        /// or the hand bust — the plainest), a loss by the House's hand (trips and up), nothing for a fold or a push.
        /// </summary>
        public static HandTier TierOf(HandOutcome outcome, HandCategory? player, HandCategory? house)
        {
            if (outcome == HandOutcome.Win)
            {
                if (!player.HasValue) return HandTier.Plain;
                switch (player.Value)
                {
                    case HandCategory.HighCard:
                    case HandCategory.OnePair: return HandTier.Plain;
                    case HandCategory.TwoPair:
                    case HandCategory.ThreeOfAKind: return HandTier.Good;
                    case HandCategory.Straight:
                    case HandCategory.Flush: return HandTier.Strong;
                    case HandCategory.FullHouse: return HandTier.Full;
                    case HandCategory.FourOfAKind: return HandTier.Quads;
                    default: return HandTier.Blaze;   // straight flush, royal flush, the Dead Man's Hand
                }
            }
            if (outcome == HandOutcome.Loss && house.HasValue && house.Value >= HandCategory.ThreeOfAKind)
                return HandTier.HouseStrong;
            return HandTier.None;
        }

        /// <summary>
        /// The cards that make the hand, as they hit: the sets by size (a full house's trips, then its pair; two pair high first), every card
        /// of a straight or a flush one by one from the left, the high card alone. <paramref name="hand"/> is the hand as judged (a joker
        /// is the card it became).
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<int>> GroupsOf(Hand hand, HandCategory category)
        {
            if (hand == null) return Array.Empty<IReadOnlyList<int>>();
            var indices = Enumerable.Range(0, Hand.Size).ToArray();
            switch (category)
            {
                case HandCategory.Straight:
                case HandCategory.Flush:
                case HandCategory.StraightFlush:
                case HandCategory.RoyalFlush:
                    return indices.Select(i => (IReadOnlyList<int>)new[] { i }).ToArray();
                case HandCategory.HighCard:
                    int high = indices.OrderByDescending(i => hand[i].Rank).First();
                    return new IReadOnlyList<int>[] { new[] { high } };
                default:
                    // Pairs, trips, quads, a full house, the Dead Man's Hand: the cards of each rank that has company.
                    return indices.GroupBy(i => hand[i].Rank).Where(g => g.Count() >= 2)
                        .OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key)
                        .Select(g => (IReadOnlyList<int>)g.ToArray()).ToArray();
            }
        }

        /// <summary>How long each tier holds the table (seconds at normal speed; a skip ends it at once). Each is longer than the one before.</summary>
        public static float SecondsOf(HandTier tier)
        {
            switch (tier)
            {
                case HandTier.Plain: return 0.8f;
                case HandTier.Good: return 1.5f;
                case HandTier.Strong: return 1.7f;
                case HandTier.Full: return 2.0f;
                case HandTier.Quads: return 2.3f;
                case HandTier.Blaze: return 2.9f;
                case HandTier.HouseStrong: return 0.7f;
                default: return 0f;
            }
        }
    }
}
