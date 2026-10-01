using System.Collections.Generic;
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

        /// <summary>The Usurer: plays strictly by the book — classic rules and payouts.</summary>
        public static Dealer Mammon => new Dealer(MammonId, maxDiscards: 3, houseRevealDecisions: 3, PayoutTable.CreateDefault());

        /// <summary>The Silver Tongue: richer payouts, steeper losses, and he hides most of his hand.</summary>
        public static Dealer Belial => new Dealer(BelialId, maxDiscards: 3, houseRevealDecisions: 1,
            new PayoutTable(new Dictionary<HandCategory, int>
            {
                { HandCategory.HighCard, 1 },
                { HandCategory.OnePair, 2 },
                { HandCategory.TwoPair, 3 },
                { HandCategory.ThreeOfAKind, 5 },
                { HandCategory.Straight, 6 },
                { HandCategory.Flush, 8 },
                { HandCategory.FullHouse, 12 },
                { HandCategory.FourOfAKind, 40 },
                { HandCategory.StraightFlush, 75 },
                { HandCategory.RoyalFlush, 150 }
            }, HandCategory.DeadMansHand, lossPercent: 150));

        /// <summary>The Queen of the Night: four cards may be exchanged, but nobody leaves her table cheaply.</summary>
        public static Dealer Lilith => new Dealer(LilithId, maxDiscards: 4, houseRevealDecisions: 3,
            new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, foldPercent: 100));

        public static IReadOnlyList<Dealer> All => new[] { Mammon, Belial, Lilith };
    }
}
