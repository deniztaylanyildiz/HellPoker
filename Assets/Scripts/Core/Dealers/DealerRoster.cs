using System.Collections.Generic;
using HellPoker.Core.Betting;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Core.Dealers
{
    /// <summary>The demons who deal Hell Poker.</summary>
    public static class DealerRoster
    {
        public const string MammonId = "mammon";
        public const string BelialId = "belial";
        public const string LilithId = "lilith";

        /// <summary>The Usurer: plays by the book and honestly — re-raises on strength, hardly ever bluffs.</summary>
        public static Dealer Mammon => new Dealer(MammonId, maxDiscards: 3, houseCardsShown: 2, PayoutTable.CreateDefault(),
            new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 70, bluffPercent: 5));

        /// <summary>The Silver Tongue: richer payouts, shows only one card, and bluffs shamelessly.</summary>
        public static Dealer Belial => new Dealer(BelialId, maxDiscards: 3, houseCardsShown: 1,
            new PayoutTable(new Dictionary<HandCategory, int>
            {
                { HandCategory.HighCard, 1 },
                { HandCategory.OnePair, 2 },
                { HandCategory.TwoPair, 3 },
                { HandCategory.ThreeOfAKind, 5 },
                { HandCategory.Straight, 6 },
                { HandCategory.Flush, 8 },
                { HandCategory.FullHouse, 12 },
                { HandCategory.FourOfAKind, 15 },
                { HandCategory.StraightFlush, 25 },
                { HandCategory.RoyalFlush, 30 }
            }, HandCategory.DeadMansHand),
            new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 60, bluffPercent: 30));

        /// <summary>The Queen of the Night: four cards may be exchanged, but folding always costs the whole stake, and she presses every edge.</summary>
        public static Dealer Lilith => new Dealer(LilithId, maxDiscards: 4, houseCardsShown: 2,
            new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, foldPercentBeforeDraw: 100, foldPercentAfterDraw: 100),
            new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 90, bluffPercent: 10));

        public static IReadOnlyList<Dealer> All => new[] { Mammon, Belial, Lilith };
    }
}
