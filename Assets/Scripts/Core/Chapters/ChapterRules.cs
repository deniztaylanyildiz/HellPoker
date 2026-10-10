using System;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// The numbers of one chapter: a map of floors played for coins, then the gate's tribute, then the chapter's demon for years.
    /// The floors never touch the sentence; the tribute is the only bridge (a missing coin is years at the gate). Every number
    /// is tunable (the chapter simulation tries others); <see cref="For"/> gives the designer's.
    /// </summary>
    public sealed class ChapterRules
    {
        public int Number { get; }

        /// <summary>The demon at the end of the chapter; the floors' imps play by their table's rules (discards, cards shown, payouts).</summary>
        public string BossId { get; }

        /// <summary>Hands at the demon's table (no leaving early; a soul on the table keeps the player there).</summary>
        public int BossHands { get; }

        /// <summary>The floors' ante, in coins. Raises are one ante before the draw, two after.</summary>
        public int Ante { get; }

        /// <summary>Coins paid at the gate; every coin missing (debt too) is <see cref="YearsPerMissingCoin"/> years.</summary>
        public int Tribute { get; }

        public int YearsPerMissingCoin { get; }

        /// <summary>Coins given at the start of the chapter (on top of what is left from the last one).</summary>
        public int StartingCoins { get; }

        /// <summary>Hands of a table node and of a warden node, and the wins each needs for its match bonus.</summary>
        public int TableHands { get; }
        public int TableWinsNeeded { get; }
        public int WardenHands { get; }
        public int WardenWinsNeeded { get; }

        /// <summary>Coins from outside the table's zero sum for a match won (the warden's comes with a relic).</summary>
        public int TableBonus { get; }
        public int WardenBonus { get; }

        /// <summary>A warden's relic when the player carries all they may: these coins instead.</summary>
        public int WardenRelicCoins { get; }

        /// <summary>At most this many antes on the table in a floor's hand (a fixed cap: a debt cannot shrink it).</summary>
        public int CapAntes { get; }

        /// <summary>A floor's hand pays at most this multiplier (a royal flush may not buy the tribute in one hand).</summary>
        public int MultiplierCap { get; }

        /// <summary>The imps re-raise a strong hand (<see cref="ImpStrongFrom"/>+) this often; they never bluff.</summary>
        public int ImpReRaisePercent { get; }
        public HandCategory ImpStrongFrom { get; }

        /// <summary>Facing a raise after the draw, an imp below a pair gives up this often.</summary>
        public int ImpFoldPercent { get; }

        /// <summary>The warden takes this share of the player's coins after every hand the player wins (the golden-eyed collector),
        /// at most <see cref="WardenTollMax"/> a hand.</summary>
        public int WardenTollPercent { get; }
        public int WardenTollMax { get; }

        /// <summary>The treasure node's coins.</summary>
        public int TreasureCoins { get; }

        /// <summary>The black market's prices, in percent of the first chapter's (rounded).</summary>
        public int PricePercent { get; }

        public int Floors { get; }
        public int Lanes { get; }

        /// <summary>A map holds at most this many wardens.</summary>
        public int MaxWardens { get; }

        public ChapterRules(int number, string bossId, int bossHands, int ante, int tribute, int pricePercent, int impReRaisePercent,
            int yearsPerMissingCoin = 5, int startingCoins = 20, int tableHands = 3, int tableWinsNeeded = 2, int wardenHands = 5,
            int wardenWinsNeeded = 3, int tableBonus = 5, int wardenBonus = 15, int wardenRelicCoins = 10, int capAntes = 6,
            int multiplierCap = 3, HandCategory impStrongFrom = HandCategory.TwoPair, int impFoldPercent = 40, int wardenTollPercent = 5,
            int treasureCoins = 20, int floors = 8, int lanes = 6, int maxWardens = 2, int wardenTollMax = 5)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            if (string.IsNullOrEmpty(bossId)) throw new ArgumentException("A chapter needs its demon.", nameof(bossId));
            if (bossHands <= 0 || ante <= 0 || tribute < 0 || pricePercent <= 0) throw new ArgumentOutOfRangeException(nameof(ante));
            if (tableHands <= 0 || wardenHands <= 0 || capAntes <= 0 || multiplierCap <= 0) throw new ArgumentOutOfRangeException(nameof(tableHands));
            if (impReRaisePercent < 0 || impReRaisePercent > 100 || impFoldPercent < 0 || impFoldPercent > 100 ||
                wardenTollPercent < 0 || wardenTollPercent > 100) throw new ArgumentOutOfRangeException(nameof(impReRaisePercent));
            if (wardenTollMax < 0) throw new ArgumentOutOfRangeException(nameof(wardenTollMax));
            if (maxWardens < 1 || maxWardens > lanes) throw new ArgumentOutOfRangeException(nameof(maxWardens));
            if (floors < 8 || lanes <= 0) throw new ArgumentOutOfRangeException(nameof(floors), "Eight floors at least: the treasure is the fifth, the fire the last.");
            Number = number;
            BossId = bossId;
            BossHands = bossHands;
            Ante = ante;
            Tribute = tribute;
            PricePercent = pricePercent;
            ImpReRaisePercent = impReRaisePercent;
            YearsPerMissingCoin = yearsPerMissingCoin;
            StartingCoins = startingCoins;
            TableHands = tableHands;
            TableWinsNeeded = tableWinsNeeded;
            WardenHands = wardenHands;
            WardenWinsNeeded = wardenWinsNeeded;
            TableBonus = tableBonus;
            WardenBonus = wardenBonus;
            WardenRelicCoins = wardenRelicCoins;
            CapAntes = capAntes;
            MultiplierCap = multiplierCap;
            ImpStrongFrom = impStrongFrom;
            ImpFoldPercent = impFoldPercent;
            WardenTollPercent = wardenTollPercent;
            WardenTollMax = wardenTollMax;
            TreasureCoins = treasureCoins;
            Floors = floors;
            Lanes = lanes;
            MaxWardens = maxWardens;
        }

        /// <summary>The designer's chapters: 1 Mammon (8 hands), 2 Belial (10), 3 Lilith (12). The first chapter's ante is 4 (the designer's 5
        /// brought 85 coins to the gate, past the 55-75 target; ChapterSimulation 2026-10-10).</summary>
        public static ChapterRules For(int chapter)
        {
            switch (chapter)
            {
                case 1: return new ChapterRules(1, DealerRoster.MammonId, bossHands: 8, ante: 4, tribute: 70, pricePercent: 100, impReRaisePercent: 10,
                    maxWardens: 3);
                case 2: return new ChapterRules(2, DealerRoster.BelialId, bossHands: 10, ante: 8, tribute: 85, pricePercent: 120, impReRaisePercent: 20);
                case 3: return new ChapterRules(3, DealerRoster.LilithId, bossHands: 12, ante: 10, tribute: 100, pricePercent: 140, impReRaisePercent: 30);
                default: throw new ArgumentOutOfRangeException(nameof(chapter), "There are three chapters.");
            }
        }

        public const int Chapters = 3;

        /// <summary>The same chapter with another match bonus for a table node and another floor ante (the tuning's knobs).</summary>
        public ChapterRules With(int? tableBonus = null, int? ante = null, int? tribute = null, int? impReRaisePercent = null,
            int? impFoldPercent = null) =>
            new ChapterRules(Number, BossId, BossHands, ante ?? Ante, tribute ?? Tribute, PricePercent, impReRaisePercent ?? ImpReRaisePercent,
                YearsPerMissingCoin, StartingCoins, TableHands, TableWinsNeeded, WardenHands, WardenWinsNeeded, tableBonus ?? TableBonus,
                WardenBonus, WardenRelicCoins, CapAntes, MultiplierCap, ImpStrongFrom, impFoldPercent ?? ImpFoldPercent, WardenTollPercent,
                TreasureCoins, Floors, Lanes, MaxWardens, WardenTollMax);

        /// <summary>A black-market price of the first chapter, in this chapter (×1.2, ×1.4; rounded).</summary>
        public int Price(int firstChapterCoins) => (firstChapterCoins * PricePercent + 50) / 100;

        /// <summary>The years a tribute short by <paramref name="coins"/> costs at the gate (debt included).</summary>
        public int TributeYears(int coins) => Math.Max(0, Tribute - coins) * YearsPerMissingCoin;

        /// <summary>The chapter's demon.</summary>
        public Dealer Boss => DealerRoster.Find(BossId) ?? throw new InvalidOperationException($"No demon '{BossId}'.");
    }
}
