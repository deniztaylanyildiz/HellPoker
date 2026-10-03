using System.Collections.Generic;
using HellPoker.Core.Betting;
using HellPoker.Core.Cheats;
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
            new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 70, bluffPercent: 5), soulThreshold: 2000,
            maliceMax: MammonMalice, cheats: MammonCheats);

        // ------------------------------------------------------------------ cheats (malice gauges tuned with BalanceSimulation)

        public const int MammonMalice = 4;
        public const int BelialMalice = 2;   // his cheats are light; even every hand keeps him near 75% (see DEVLOG)
        public const int LilithMalice = 4;   // her cheats bite: a slower gauge holds her near 55%
        public const int LuciferMalice = 1;

        /// <summary>Belial's announced intent is a lie this often (percent).</summary>
        public const int BelialLiePercent = 25;

        /// <summary>A liar's tongue slips: this often (percent) his forked tongue changes a suit at random, maybe to the player's good.</summary>
        public const int BelialBackfirePercent = 20;

        /// <summary>Lucifer's Fall comes only at or below this sentence, once per attempt.</summary>
        public const int TheFallYears = 150;

        /// <summary>Honest: his intent is always the truth.</summary>
        public static ICheatPolicy MammonCheats =>
            new DemonCheatPolicy(new ICheat[] { new CollateralCheat(), new TitheCheat() }, new ICheat[] { new BuyoutCheat() });

        /// <summary>The liar: a quarter of his intents are another of his cheats.</summary>
        public static ICheatPolicy BelialCheats =>
            new DemonCheatPolicy(new ICheat[] { new FalseFaceCheat(), new ForkedTongueCheat() }, new ICheat[] { new SerpentSwapCheat() },
                liePercent: BelialLiePercent);

        public static ICheatPolicy LilithCheats =>
            new DemonCheatPolicy(new ICheat[] { new NightVeilCheat(), new ThornCheat() }, new ICheat[] { new MoonlessCheat() });

        /// <summary>Every hand a cheat, always announced truly; The Fall once per attempt, at 150 years or less.</summary>
        public static ICheatPolicy LuciferCheats =>
            new DemonCheatPolicy(new ICheat[] { new GazeCheat(), new RewriteCheat(), new BurningCardCheat() }, new ICheat[] { new TheFallCheat() },
                ownMajorYears: TheFallYears, majorOncePerTable: true);

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
            new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 60, bluffPercent: 30), soulThreshold: 1750,
            maliceMax: BelialMalice, cheats: BelialCheats, backfirePercent: BelialBackfirePercent);

        /// <summary>
        /// The Queen of the Night, the hardest table: four cards may be exchanged, but losses cost a quarter more,
        /// folding always costs the whole stake, and she presses every edge.
        /// </summary>
        public static Dealer Lilith => new Dealer(LilithId, maxDiscards: 4, houseCardsShown: 2,
            new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 125,
                foldPercentBeforeDraw: 100, foldPercentAfterDraw: 100),
            new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 90, bluffPercent: 10), soulThreshold: 1500,
            maliceMax: LilithMalice, cheats: LilithCheats);

        /// <summary>
        /// The Morning Star, the final table. Nobody chooses him: below the gate (250 years) the player is summoned, and only
        /// at his table can the sentence end. Fixed stakes (50, at most 150), no house cards shown, losses ×1.25, folding
        /// always costs the whole stake, re-raises with Two Pair+ 80% and bluffs 25%.
        /// </summary>
        public static Dealer Lucifer => new Dealer(LuciferId, maxDiscards: 3, houseCardsShown: 0,
            new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 125,
                foldPercentBeforeDraw: 100, foldPercentAfterDraw: 100),
            new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 80, bluffPercent: 25), soulThreshold: 2000,
            stakes: StakeScale.Fixed(LuciferUnit, LuciferCap), isFinalTable: true, maliceMax: LuciferMalice, cheats: LuciferCheats);

        public const string LuciferId = "lucifer";
        public const int LuciferUnit = 50;
        public const int LuciferCap = 150;

        /// <summary>The demons a player may choose to sit with (Lucifer is not among them).</summary>
        public static IReadOnlyList<Dealer> All => new[] { Mammon, Belial, Lilith };

        /// <summary>Any demon by id, Lucifer included; null for an unknown id.</summary>
        public static Dealer Find(string id)
        {
            if (id == LuciferId) return Lucifer;
            foreach (Dealer dealer in All)
            {
                if (dealer.Id == id) return dealer;
            }
            return null;
        }
    }
}
