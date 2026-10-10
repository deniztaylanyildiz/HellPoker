using System;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// The numbers of one chapter: a map of floors played for coins, then the gate's tribute, then the chapter's demon for years.
    /// The floors never touch the sentence; the tribute is the only bridge (a missing coin is years at the gate) — but a purse
    /// emptied at a floor's table ends the run. Every number
    /// is tunable (the chapter simulation tries others); <see cref="For"/> gives the designer's.
    /// </summary>
    public sealed class ChapterRules
    {
        public int Number { get; }

        /// <summary>The demon at the end of the chapter; the floors' imps play by their table's rules (discards, cards shown, payouts).</summary>
        public string BossId { get; }

        /// <summary>The demon's soul line in percent of the bar the table starts with (Mammon 200, Belial 175, Lilith 150). The table
        /// itself has no hand limit: it ends when the bar is empty (the demon beaten) or the soul burns.</summary>
        public int SoulLinePercent { get; }

        /// <summary>The demon's table pays this share of its payouts off the bar on a won hand, and puts this share of a loss on it
        /// (<see cref="ScaledPayoutTable"/>; the tuning's knobs, 100 / 100 by default).</summary>
        public int BossWinPercent { get; private set; } = 100;
        public int BossLossPercent { get; private set; } = 100;

        /// <summary>The floors' first ante, in coins; it grows by <see cref="AnteStep"/> every <see cref="AnteStepHands"/> hands of a
        /// match (<see cref="AnteAt"/>). Raises are one ante before the draw, two after.</summary>
        public int Ante { get; }

        public int AnteStepHands { get; }
        public int AnteStep { get; }

        /// <summary>The gate's tribute over the class's starting purse (<see cref="TributeFor"/>): what every class must win on the floors.</summary>
        public int TributeOverStart { get; }

        /// <summary>Every coin missing at the gate is this many years.</summary>
        public int YearsPerMissingCoin { get; }

        /// <summary>The purses of a table's imp and of a warden: a match goes on until one side's purse is empty.</summary>
        public int ImpCoins { get; }
        public int WardenCoins { get; }

        /// <summary>A warden's relic when the player carries all they may: these coins instead.</summary>
        public int WardenRelicCoins { get; }

        /// <summary>At most this many antes (the ante of the hand) on the table in a floor's hand.</summary>
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

        public ChapterRules(int number, string bossId, int soulLinePercent, int ante, int tributeOverStart, int pricePercent, int impReRaisePercent,
            int impCoins, int wardenCoins, int yearsPerMissingCoin = 5, int anteStepHands = 3, int anteStep = 1, int wardenRelicCoins = 10,
            int capAntes = 6, int multiplierCap = 3, HandCategory impStrongFrom = HandCategory.TwoPair, int impFoldPercent = 40,
            int wardenTollPercent = 5, int treasureCoins = 20, int floors = 8, int lanes = 6, int maxWardens = 2, int wardenTollMax = 5)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            if (string.IsNullOrEmpty(bossId)) throw new ArgumentException("A chapter needs its demon.", nameof(bossId));
            if (soulLinePercent <= 100 || ante <= 0 || tributeOverStart < 0 || pricePercent <= 0) throw new ArgumentOutOfRangeException(nameof(ante));
            if (impCoins <= 0 || wardenCoins <= 0) throw new ArgumentOutOfRangeException(nameof(impCoins), "An imp sits down with coins.");
            if (anteStepHands <= 0 || anteStep < 0) throw new ArgumentOutOfRangeException(nameof(anteStepHands));
            if (capAntes <= 0 || multiplierCap <= 0) throw new ArgumentOutOfRangeException(nameof(capAntes));
            if (impReRaisePercent < 0 || impReRaisePercent > 100 || impFoldPercent < 0 || impFoldPercent > 100 ||
                wardenTollPercent < 0 || wardenTollPercent > 100) throw new ArgumentOutOfRangeException(nameof(impReRaisePercent));
            if (wardenTollMax < 0) throw new ArgumentOutOfRangeException(nameof(wardenTollMax));
            if (maxWardens < 1 || maxWardens > lanes) throw new ArgumentOutOfRangeException(nameof(maxWardens));
            if (floors < 8 || lanes <= 0) throw new ArgumentOutOfRangeException(nameof(floors), "Eight floors at least: the treasure is the fifth, the fire the last.");
            Number = number;
            BossId = bossId;
            SoulLinePercent = soulLinePercent;
            Ante = ante;
            AnteStepHands = anteStepHands;
            AnteStep = anteStep;
            TributeOverStart = tributeOverStart;
            PricePercent = pricePercent;
            ImpReRaisePercent = impReRaisePercent;
            ImpCoins = impCoins;
            WardenCoins = wardenCoins;
            YearsPerMissingCoin = yearsPerMissingCoin;
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

        /// <summary>The designer's chapters: 1 Mammon (soul line 2× his bar), 2 Belial (1.75×), 3 Lilith (1.5×). The floors: a match goes
        /// on until a purse is empty (an imp 30 / 40 / 50 coins, a warden 60 / 80 / 100), the ante grows by one every three hands; the
        /// tribute is the class's starting purse + 60 / 75 / 90.</summary>
        public static ChapterRules For(int chapter)
        {
            switch (chapter)
            {
                // Mammon's table: wins 175%, losses 125% of his payouts (BossSimulation 2026-10-10: 17.5 hands, 11% damned; at
                // 100 / 100 it was 54 hands).
                case 1: return new ChapterRules(1, DealerRoster.MammonId, soulLinePercent: 200, ante: 4, tributeOverStart: 60, pricePercent: 100,
                    impReRaisePercent: 10, impCoins: 30, wardenCoins: 60, maxWardens: 3).WithBossPayouts(175, 125);
                case 2: return new ChapterRules(2, DealerRoster.BelialId, soulLinePercent: 175, ante: 8, tributeOverStart: 75, pricePercent: 120,
                    impReRaisePercent: 20, impCoins: 40, wardenCoins: 80);
                case 3: return new ChapterRules(3, DealerRoster.LilithId, soulLinePercent: 150, ante: 10, tributeOverStart: 90, pricePercent: 140,
                    impReRaisePercent: 30, impCoins: 50, wardenCoins: 100);
                default: throw new ArgumentOutOfRangeException(nameof(chapter), "There are three chapters.");
            }
        }

        public const int Chapters = 3;

        /// <summary>The same chapter with other numbers (the tuning's knobs); the starting purses and the boss's percents go along.</summary>
        public ChapterRules With(int? ante = null, int? tributeOverStart = null, int? impReRaisePercent = null, int? impFoldPercent = null,
            int? impCoins = null, int? wardenCoins = null, int? anteStepHands = null, int? capAntes = null, int? multiplierCap = null,
            int? soulLinePercent = null) =>
            new ChapterRules(Number, BossId, soulLinePercent ?? SoulLinePercent, ante ?? Ante, tributeOverStart ?? TributeOverStart, PricePercent,
                impReRaisePercent ?? ImpReRaisePercent, impCoins ?? ImpCoins, wardenCoins ?? WardenCoins, YearsPerMissingCoin,
                anteStepHands ?? AnteStepHands, AnteStep, WardenRelicCoins, capAntes ?? CapAntes, multiplierCap ?? MultiplierCap, ImpStrongFrom,
                impFoldPercent ?? ImpFoldPercent,
                WardenTollPercent, TreasureCoins, Floors, Lanes, MaxWardens, WardenTollMax)
            {
                StartPercent = StartPercent,
                BossWinPercent = BossWinPercent,
                BossLossPercent = BossLossPercent
            };

        /// <summary>The same chapter whose demon's table pays <paramref name="winPercent"/> of a win off the bar and puts
        /// <paramref name="lossPercent"/> of a loss on it.</summary>
        public ChapterRules WithBossPayouts(int winPercent, int lossPercent)
        {
            if (winPercent <= 0 || lossPercent <= 0) throw new ArgumentOutOfRangeException(nameof(winPercent));
            ChapterRules copy = With();
            copy.BossWinPercent = winPercent;
            copy.BossLossPercent = lossPercent;
            return copy;
        }

        /// <summary>The ante of a match's hand <paramref name="hand"/> (1 = the first): 4, 4, 4, 5, 5, 5, 6...</summary>
        public int AnteAt(int hand) => Ante + Math.Max(0, hand - 1) / AnteStepHands * AnteStep;

        /// <summary>The coins a sinner of this class starts the first chapter with: the Peasant 30, the Jester 40, the Warlock 50, the
        /// King 100 (any other: the Peasant's).</summary>
        public static int StartingCoinsFor(string classId)
        {
            switch (classId)
            {
                case Sinners.King.ClassId: return 100;
                case Sinners.Warlock.ClassId: return 50;
                case Sinners.Jester.ClassId: return 40;
                default: return 30;
            }
        }

        /// <summary>The starting purses in percent of the designer's (a tuning knob; 100 at every chapter).</summary>
        public int StartPercent { get; private set; } = 100;

        /// <summary>The coins a sinner of this class starts with under <see cref="StartPercent"/>.</summary>
        public int StartingCoinsOf(string classId) => StartingCoinsFor(classId) * StartPercent / 100;

        /// <summary>The gate's tribute for a sinner of this class: their starting purse and <see cref="TributeOverStart"/>
        /// (90 / 100 / 110 / 160 in the first chapter).</summary>
        public int TributeFor(string classId) => StartingCoinsOf(classId) + TributeOverStart;

        /// <summary>The same chapter with the starting purses at <paramref name="percent"/> of the designer's (the tribute follows).</summary>
        public ChapterRules WithStartPercent(int percent)
        {
            if (percent <= 0) throw new ArgumentOutOfRangeException(nameof(percent));
            ChapterRules copy = With();
            copy.StartPercent = percent;
            return copy;
        }

        /// <summary>A black-market price of the first chapter, in this chapter (×1.2, ×1.4; rounded).</summary>
        public int Price(int firstChapterCoins) => (firstChapterCoins * PricePercent + 50) / 100;

        /// <summary>The chapter's demon.</summary>
        public Dealer Boss => DealerRoster.Find(BossId) ?? throw new InvalidOperationException($"No demon '{BossId}'.");
    }
}
