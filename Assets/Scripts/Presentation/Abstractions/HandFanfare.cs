using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// Demo 3.1: how a chapter table's hand ends, by the hand (Demo 3.1.1: every hand its own motion). Each is plainly more than the one
    /// before it. A fold and a push play nothing; a big loss has its own mark (<see cref="HandFanfare.BigLoss"/>).
    /// </summary>
    public enum HandTier
    {
        /// <summary>A fold, a push, a loss to a small hand: nothing.</summary>
        None,

        /// <summary>A win with a high card or a pair (or the imp folding): the pair taps, coins fly to the purse, the sinner smiles.</summary>
        Plain,

        /// <summary>Two pair: each pair hits on its own, the higher first; the name; coins.</summary>
        TwoPair,

        /// <summary>Trips: the three cards hit together in a ring of gold sparks; the screen knocks a pixel up and down; the name; coins.</summary>
        Trips,

        /// <summary>A straight: a wave — the cards rise and light one after another from the left, a glint on the last; the name; coins.</summary>
        Straight,

        /// <summary>A flush: the suit's symbols rain onto the cards, the cards flash once in the suit's tone; the name; a burst of coins.</summary>
        Flush,

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
        /// <summary>A loss of at least this many antes is a big one: a short shake and the screen's edge flickering red.</summary>
        public const int BigLossAntes = 4;

        public HandTier Tier { get; }

        /// <summary>The hand's name in big letters (two pair and up); null for none.</summary>
        public string Title { get; }

        /// <summary>The cards that hit, group by group (indices into the winner's row: the House's for <see cref="HandTier.HouseStrong"/>).</summary>
        public IReadOnlyList<IReadOnlyList<int>> Groups { get; }

        public bool OnHouseSide => Tier == HandTier.HouseStrong;

        /// <summary>The coins fly to the demon's bar (his table) rather than the purse (a floor).</summary>
        public bool CoinsToBar { get; }

        /// <summary>Demo 3.1.1: a big loss — after the House's cards (if they hit), a short shake and the edge flickering red.</summary>
        public bool BigLoss { get; }

        /// <summary>A flush's suit: its symbols rain onto the cards.</summary>
        public Suit? FlushSuit { get; }

        /// <summary>Something to play: a tier, or a big loss on its own (lost to the House's pair).</summary>
        public bool Plays => Tier != HandTier.None || BigLoss;

        public HandFanfare(HandTier tier, string title, IReadOnlyList<IReadOnlyList<int>> groups, bool coinsToBar = false, bool bigLoss = false,
            Suit? flushSuit = null)
        {
            Tier = tier;
            Title = title;
            Groups = groups ?? Array.Empty<IReadOnlyList<int>>();
            CoinsToBar = coinsToBar;
            BigLoss = bigLoss;
            FlushSuit = flushSuit;
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
                    case HandCategory.TwoPair: return HandTier.TwoPair;
                    case HandCategory.ThreeOfAKind: return HandTier.Trips;
                    case HandCategory.Straight: return HandTier.Straight;
                    case HandCategory.Flush: return HandTier.Flush;
                    case HandCategory.FullHouse: return HandTier.Full;
                    case HandCategory.FourOfAKind: return HandTier.Quads;
                    default: return HandTier.Blaze;   // straight flush, royal flush, the Dead Man's Hand
                }
            }
            if (outcome == HandOutcome.Loss && house.HasValue && house.Value >= HandCategory.ThreeOfAKind)
                return HandTier.HouseStrong;
            return HandTier.None;
        }

        /// <summary>A loss is big from <see cref="BigLossAntes"/> antes up (coins on a floor, the bar's years at a demon's table).</summary>
        public static bool IsBigLoss(HandOutcome outcome, int lost, int ante) => outcome == HandOutcome.Loss && ante > 0 && lost >= BigLossAntes * ante;

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

        /// <summary>How long each tier holds the table (seconds at normal speed; a skip ends it at once). Each win is longer than the one before.</summary>
        public static float SecondsOf(HandTier tier)
        {
            switch (tier)
            {
                case HandTier.Plain: return 0.8f;
                case HandTier.TwoPair: return 1.3f;
                case HandTier.Trips: return 1.4f;
                case HandTier.Straight: return 1.6f;
                case HandTier.Flush: return 1.8f;
                case HandTier.Full: return 2.0f;
                case HandTier.Quads: return 2.3f;
                case HandTier.Blaze: return 2.9f;
                case HandTier.HouseStrong: return 0.7f;
                default: return 0f;
            }
        }

        /// <summary>A big loss adds this much (the shake and the red flicker).</summary>
        public const float BigLossSeconds = 0.6f;
    }
}
