using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>Where a Phase 2 run stands: in a chapter (its floors, its gate, its demon), at Lucifer's table, or over.</summary>
    public enum JourneyStage
    {
        Chapter,
        Lucifer,
        Over
    }

    /// <summary>How a Phase 2 run ended.</summary>
    public enum JourneyEnd
    {
        None,

        /// <summary>The purse went empty at a floor's table (damned).</summary>
        PurseEmptied,

        /// <summary>The soul burned at a demon's table (damned).</summary>
        Damned,

        /// <summary>Cast down from Lucifer's table (damned).</summary>
        CastDown,

        /// <summary>Lucifer's bar is empty: free.</summary>
        Freed,

        /// <summary>A new run was started over this one (damned).</summary>
        Abandoned
    }

    /// <summary>
    /// A whole Phase 2 run: the three chapters in order (<see cref="ChapterRules.For"/>: Mammon, Belial, Lilith), then Lucifer's table.
    /// What goes from chapter to chapter: the purse, the relics, the charge, the Jester's jokers (a silenced or desired relic is itself
    /// again); every chapter starts with a fresh deck and its demon's share of the sentence (<see cref="BossShares"/>). After Lilith,
    /// no loot and no map: Lucifer's table at once — his bar empty is freedom, past <see cref="ChapterRules.LuciferCastDownPercent"/> of its
    /// start the run is over. The driver plays the chapters (<see cref="Run"/>) and reports how the tables end.
    /// </summary>
    public sealed class ChapterJourney
    {
        /// <summary>The dice streams under the run's seed: each chapter's own, Lucifer's table.</summary>
        public const int ChapterStream = 20, LuciferStream = 21;

        public int Seed { get; }
        public Sinner Sinner { get; private set; }
        public RunEffects Effects { get; }

        /// <summary>1, 2, 3 (Lucifer's table counts as after the third).</summary>
        public int Chapter { get; private set; }

        public JourneyStage Stage { get; private set; } = JourneyStage.Chapter;
        public JourneyEnd End { get; private set; }

        /// <summary>The chapter being played (the last one at Lucifer's table).</summary>
        public ChapterRun Run { get; private set; }

        /// <summary>Lucifer's bar as his table began; 0 before it.</summary>
        public int LuciferBarStart { get; private set; }

        /// <summary>Hands played at each boss's table (Mammon, Belial, Lilith, Lucifer); 0 for one not met.</summary>
        public int[] BossHands { get; } = new int[4];

        /// <summary>Hands played at the floors' tables, all chapters.</summary>
        public int FloorHands { get; set; }

        /// <summary>Hands in all.</summary>
        public int TotalHands => FloorHands + BossHands.Sum();

        public bool IsOver => Stage == JourneyStage.Over;
        public bool IsDamned => End == JourneyEnd.PurseEmptied || End == JourneyEnd.Damned || End == JourneyEnd.CastDown || End == JourneyEnd.Abandoned;

        private ChapterJourney(int seed, Sinner sinner, RunEffects effects)
        {
            Seed = seed;
            Sinner = sinner ?? throw new ArgumentNullException(nameof(sinner));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        /// <summary>A Phase 2 run's effects: three relic slots.</summary>
        private static RunEffects PhaseTwoEffects() => new RunEffects { MaxCarried = ChapterRelics.Slots, MaxBag = ChapterRelics.BagSlots };

        /// <summary>The dice of the Seed (a Peasant's upgrade) at a chapter's start.</summary>
        public const int SeedStream = 22;

        public static int ChapterSeed(int seed, int chapter) => RandomSeeds.Derive(RandomSeeds.Derive(seed, ChapterStream), chapter);

        /// <summary>A new run as <paramref name="sinnerClass"/>: the first chapter, the class's purse.</summary>
        /// <param name="difficulty">Demo 3.4: how hard the run is (<see cref="DifficultyRules"/>).</param>
        public static ChapterJourney Begin(SinnerClass sinnerClass, int seed, Difficulty difficulty = Difficulty.Easy)
        {
            if (sinnerClass == null) throw new ArgumentNullException(nameof(sinnerClass));
            var journey = new ChapterJourney(seed, new Sinner(sinnerClass) { JackpotFallsTo = Jester.PhaseTwoJackpotFallsTo }, PhaseTwoEffects())
                { Chapter = 1, Difficulty = difficulty, RunId = NewRunId() };
            journey.Sentence = new RunSentence(RunSentence.StartFor(sinnerClass.Id));
            journey.Run = ChapterRun.Begin(journey.RulesFor(1), journey.Sinner, journey.Effects, ChapterSeed(seed, 1),
                ChapterTuning.MapVersion > 0 ? ChapterTuning.MapVersion : ChapterMap.CurrentVersion);
            journey.Run.Sentence = journey.Sentence;
            journey.Run.RunEventsSeen = journey._runEvents;
            journey.WatchRelics();
            return journey;
        }

        /// <summary>A relic new to the run: Lucifer's Feather puts a tenth on the sentence as it is taken.</summary>
        private void WatchRelics() => Effects.Joined += id =>
        {
            if (id == ChapterRelicIds.LucifersFeather) Sentence.Add(Sentence.Years * FeatherSentencePercent / 100);
        };

        /// <summary>Lucifer's Feather: the sentence this much (percent) longer as it is taken.</summary>
        public const int FeatherSentencePercent = 10;

        /// <summary>Demo 2.1's offers seen this run (each comes once a run): every chapter's run shares the set.</summary>
        private HashSet<string> _runEvents = new HashSet<string>();

        /// <summary>The run's sentence in years (<see cref="RunSentence"/>): shown from the first floor to Lucifer; what is left of it when
        /// the run walks free is the score.</summary>
        public RunSentence Sentence { get; private set; }

        /// <summary>Demo 3.4: how hard the run is (Easy: the designer's numbers).</summary>
        public Difficulty Difficulty { get; private set; }

        /// <summary>Demo 3.4: the run's own id and how many times it was saved (Ironman: a copied old save is known by them).</summary>
        public string RunId { get; private set; }
        public int SaveCount { get; set; }

        private static string NewRunId() => Guid.NewGuid().ToString("N").Substring(0, 12);

        private ChapterRules RulesFor(int chapter) => ChapterRules.For(chapter).WithDifficulty(Difficulty);

        private Func<string, bool> _relicAvailable;

        /// <summary>Which relics may be drawn (the locked ones may not); null: all. Every chapter's run follows it.</summary>
        public Func<string, bool> RelicAvailable
        {
            get => _relicAvailable;
            set
            {
                _relicAvailable = value;
                if (Run != null) Run.RelicAvailable = value;
            }
        }

        /// <summary>The demon of this chapter is beaten and his loot taken: on to the next chapter (or, after Lilith, Lucifer).</summary>
        public bool CanGoOn => Stage == JourneyStage.Chapter && Run.BossBeaten && (Chapter == ChapterRules.Chapters || Run.LootTaken);

        /// <summary>
        /// The next chapter: the purse, relics, charge, jokers and the class's kit go along; a fresh deck and the next demon's share. A
        /// purse below the class's starting purse is topped up to it (<see cref="PurseToppedUp"/>: the coins added).
        /// </summary>
        /// <param name="minimumPurse">A tuning knob (the simulation's): the floor in place of the class's starting purse; -1 for the rule.</param>
        public ChapterRun NextChapter(int minimumPurse = -1)
        {
            if (!CanGoOn || Chapter >= ChapterRules.Chapters) throw new InvalidOperationException("The chapter is not over.");
            Run.EndChapter();
            Chapter++;
            ChapterRules rules = RulesFor(Chapter);
            // Demo 3.4 (Hard): the purse is topped up only to a share of the class's start.
            int floor = minimumPurse >= 0 ? minimumPurse
                : ChapterRules.StartingCoinsFor(Sinner.Id) * DifficultyRules.PurseTopUpFor(Difficulty, Chapter) / 100;
            int coins = Math.Max(floor, Run.Purse.Coins);
            PurseToppedUp = coins - Run.Purse.Coins;
            Run = new ChapterRun(rules, Sinner, Effects, BossShares.For(Sinner.Id, rules.BossId), coins, ChapterSeed(Seed, Chapter), kit: Kit,
                mapVersion: Run.Map.Version)   // a run keeps its maps' generator
            {
                RelicAvailable = _relicAvailable,
                Sentence = Sentence,
                RunEventsSeen = _runEvents
            };
            Kit.EnterChapter(Chapter);
            Sown = Kit.Sow(new SystemRandomSource(RandomSeeds.Derive(RandomSeeds.Derive(Seed, SeedStream), Chapter)));
            return Run;
        }

        /// <summary>The card the Seed put in the satchel as the last chapter began; null for none.</summary>
        public Cards.Card? Sown { get; private set; }

        /// <summary>The coins the last <see cref="NextChapter"/> added to bring the purse up to the class's start (0: none).</summary>
        public int PurseToppedUp { get; private set; }

        /// <summary>The class's Phase 2 mechanics (the Peasant's satchel, the Warlock's prophecy, the King's treasury): the run's.</summary>
        public ClassKit Kit => Run.Kit;

        /// <summary>After Lilith: Lucifer's table, at once (no loot, no map). The bar is the run's share at him.</summary>
        public HellPokerGame OpenLuciferTable(GameRules template, int bar = 0, int handsPlayed = 0, int barStart = 0)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (Stage == JourneyStage.Chapter)
            {
                if (Chapter != ChapterRules.Chapters || !Run.BossBeaten) throw new InvalidOperationException("Lilith is not beaten.");
                Run.EndChapter();
                Stage = JourneyStage.Lucifer;
            }
            LuciferBarStart = barStart > 0 ? barStart : Math.Max(1, BossShares.For(Sinner.Id, Dealers.DealerRoster.LuciferId));
            Effects.SitAt(Dealers.DealerRoster.LuciferId, fresh: true);
            Dealers.Dealer lucifer = BossTable.Lucifer;
            HellPokerGame game = BossTable.Create(template, lucifer, LuciferBarStart, 101,
                BossTable.WinWithRelics(ChapterRules.LuciferWinPercent, Effects.CombinedRelics, Run.Purse.Coins),
                ChapterRules.LuciferLoss * DifficultyRules.BossLossFor(Difficulty, 3) / 100, RandomSeeds.Derive(Seed, LuciferStream), Sinner, Sinner,
                DifficultyRules.MaliceShortFor(Difficulty, 3, lucifer.MaliceMax));
            game.UseEffects(Effects);
            BossTable.Arm(game, Effects);
            Sentence.Follow(game);
            Kit.EnterLucifer();
            LuciferHooks = ChapterHooks.Attach(game, Kit, Dealers.DealerRoster.LuciferId, LuciferBarStart, RandomSeeds.Derive(Seed, LuciferStream),
                Difficulty);
            if ((bar > 0 && bar != LuciferBarStart) || handsPlayed > 0)
            {
                game.TakeOver(Math.Max(1, bar > 0 ? bar : LuciferBarStart), handsPlayed);
                LuciferHooks.Rule?.CheckRage(game, announce: false);
            }
            return game;
        }

        /// <summary>The hooks of Lucifer's table (his mirror, the class's acts); null before it.</summary>
        public ChapterHooks LuciferHooks { get; private set; }

        /// <summary>Between hands at Lucifer's table: the bar past his gate casts the player down.</summary>
        public bool IsCastDown(int bar) => Stage == JourneyStage.Lucifer && BossTable.CastDown(bar, LuciferBarStart);

        /// <summary>The boss of a chapter (0-based: Mammon 0 ... Lucifer 3).</summary>
        public int BossIndex => Stage == JourneyStage.Lucifer ? 3 : Chapter - 1;

        /// <summary>The run is over.</summary>
        public void Finish(JourneyEnd end)
        {
            if (end == JourneyEnd.None) throw new ArgumentOutOfRangeException(nameof(end));
            if (Stage == JourneyStage.Over) return;
            Sentence.Unfollow();
            End = end;
            Stage = JourneyStage.Over;
        }

        // ------------------------------------------------------------------ the save

        /// <summary>A saved run comes back: the chapter (or Lucifer's table) and the parts that go along.</summary>
        public static ChapterJourney Restore(ChapterSave save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            SinnerClass sinnerClass = SinnerRoster.Find(save.ClassId) ?? throw new ArgumentException("Unknown class.", nameof(save));
            var effects = PhaseTwoEffects();
            effects.Restore(HandModifier.None, 0, 0, 0, save.Relics, save.RedrawsLeft, save.RedrawTables);
            effects.RestoreBag(save.Bag, save.Forged, save.Cleansed);
            if (save.Silenced != null) effects.SilenceCurse(save.Silenced);
            if (save.Amplified != null) effects.Amplify(save.Amplified);
            var sinner = new Sinner(sinnerClass, save.Charge, null, save.Ward,
                sinnerClass.StartingJokers > 0 ? Math.Max(sinnerClass.StartingJokers, save.Jokers) : 0) { JackpotFallsTo = Jester.PhaseTwoJackpotFallsTo };
            sinner.RestoreJackpots(save.Jackpots, save.JackpotRewards);
            int chapter = Math.Max(1, Math.Min(ChapterRules.Chapters, save.Chapter));
            var journey = new ChapterJourney(save.Seed, sinner, effects)
            {
                Chapter = chapter, FloorHands = Math.Max(0, save.FloorHands), Difficulty = save.Difficulty,
                RunId = string.IsNullOrEmpty(save.RunId) ? NewRunId() : save.RunId, SaveCount = Math.Max(0, save.SaveCount)
            };
            for (int i = 0; i < 4 && i < save.BossHands.Count; i++) journey.BossHands[i] = Math.Max(0, save.BossHands[i]);
            ChapterRules rules = journey.RulesFor(chapter);
            journey.Sentence = new RunSentence(save.Sentence >= 0 ? save.Sentence : SentenceOfAnOldSave(save));
            journey.Run = new ChapterRun(rules, sinner, effects, Math.Max(1, save.Years), Math.Max(0, save.Coins), ChapterSeed(save.Seed, chapter),
                mapVersion: save.MapVersion)
            {
                Sentence = journey.Sentence
            };
            journey.Run.Restore(save.Trail, save.Years, save.Owed, TableMarks.Decode(save.Marks), save.Breaker, save.Rested, save.EventsSeen,
                null, save.Matches, save.WardenRelic, save.BossBarStart, save.BossBeaten, save.Loot, save.LootTaken, save.LootUpgrade, save.Whetted,
                save.CompassJumps);
            journey.Run.RestoreCompass(save.CompassUsed);
            journey.Run.RestoreWardensBeaten(save.WardensHere);
            journey.Run.Kit.Restore(save.Satchel, save.Taxes, save.LuciferBag, save.LuciferTaxes, save.Upgrades);
            if (save.CampBar.HasValue) journey.Run.RestoreCampBar(save.CampBar.Value);
            journey.Run.RestoreGateCoins(save.GateCoins);
            int vendors = Enum.GetValues(typeof(MarketVendor)).Length;
            journey.Run.RestoreVendors(save.Vendors.Where(v => v >= 0 && v < vendors).Select(v => (MarketVendor)v),
                save.Vendor >= 0 && save.Vendor < vendors ? (MarketVendor)save.Vendor : (MarketVendor?)null);
            journey._runEvents = new HashSet<string>(save.RunEvents ?? new List<string>());
            journey.Run.RestoreOffers(save.AnteCut, save.WardenPurse, save.Loan, save.LoanPays, save.Insured, save.BossMalice, save.Skip, save.Memo,
                save.Offers, journey._runEvents, save.Revealed);
            journey.WatchRelics();
            effects.FeatherSpent = save.FeatherSpent;
            if (save.Lucifer)
            {
                journey.Stage = JourneyStage.Lucifer;
                journey.LuciferBarStart = save.LuciferBarStart;
            }
            return journey;
        }

        /// <summary>A save from before the sentence was kept (Demo 2.0.1): what is owed to this chapter's demon (or Lucifer) and the shares
        /// of the demons still ahead.</summary>
        private static int SentenceOfAnOldSave(ChapterSave save)
        {
            int now = save.Lucifer || (save.BossBarStart > 0 && !save.BossBeaten && save.BossBar > 0)
                ? Math.Max(0, save.BossBar > 0 ? save.BossBar : save.LuciferBarStart)
                : Math.Max(0, save.Years) + Math.Max(0, save.Owed);
            int ahead = 0;
            for (int boss = save.Lucifer ? 4 : Math.Max(1, Math.Min(ChapterRules.Chapters, save.Chapter)) + 1; boss <= 4; boss++)
                ahead += BossShares.For(save.ClassId, BossShares.Bosses[boss - 1]);
            return now + ahead;
        }

        /// <summary>The run as it stands, for the save (the driver adds where it is: the node's state, a match, a hand).</summary>
        public ChapterSave Capture()
        {
            ChapterRun run = Run;
            return new ChapterSave
            {
                ClassId = Sinner.Id,
                Seed = Seed,
                Difficulty = Difficulty,
                RunId = RunId,
                SaveCount = SaveCount,
                Chapter = Chapter,
                MapVersion = run.Map.Version,
                Lucifer = Stage == JourneyStage.Lucifer,
                LuciferBarStart = LuciferBarStart,
                Coins = run.Purse.Coins,
                Years = run.Years,
                Owed = run.YearsOwed,
                Sentence = Sentence.Years,
                Charge = Sinner.Charge,
                Ward = Sinner.WardRaised,
                Jokers = Sinner.Jokers,
                Jackpots = Sinner.JokerJackpots,
                JackpotRewards = Sinner.JackpotRewardsTaken,
                Relics = Effects.Relics.ToList(),
                Silenced = Effects.SilencedCurse,
                Amplified = Effects.AmplifiedRelic,
                RedrawsLeft = Effects.RedrawsLeft,
                RedrawTables = Effects.RedrawTablesCode,
                Trail = run.Trail.Select(n => n.Lane).ToList(),
                Marks = run.NextTableMarks.Encode(),
                Breaker = run.BreaksFirstCheat,
                Rested = run.RestedByTheFire,
                EventsSeen = run.EventsSeen.ToList(),
                Matches = run.MatchesOpened,
                WardenRelic = run.WardenRelicWaiting,
                BossBarStart = run.BossBarStart,
                BossBeaten = run.BossBeaten,
                Loot = run.LootOffers.ToList(),
                LootTaken = run.LootTaken,
                LootUpgrade = run.LootUpgrade,
                Whetted = run.WhettedSoul,
                AnteCut = run.AnteCutTables,
                WardenPurse = run.NextWardenPursePercent,
                Loan = run.LoanDue,
                LoanPays = run.LoanPays,
                Insured = run.Insured,
                BossMalice = run.BossMaliceFull,
                Skip = run.SkipNext,
                Memo = run.EventMemo,
                Offers = run.EventOffers.ToList(),
                RunEvents = _runEvents.ToList(),
                Revealed = run.Revealed.Select(n => n.Floor + ":" + n.Lane).ToList(),
                Bag = Effects.Bag.ToList(),
                Forged = Effects.Forged.ToList(),
                CampBar = run.CampBarPercent,
                GateCoins = run.GateCoins,
                Vendors = run.VendorsSeen.Select(v => (int)v).ToList(),
                Vendor = run.MarketHere.HasValue ? (int)run.MarketHere.Value : -1,
                Cleansed = Effects.Cleansed.ToList(),
                FeatherSpent = Effects.FeatherSpent,
                CompassUsed = run.CompassStepsUsed,
                CompassJumps = run.CompassJumps.OrderBy(f => f).ToList(),
                WardensHere = run.WardensBeatenHere,
                Upgrades = Kit.Upgrades.ToList(),
                BossHands = BossHands.ToList(),
                FloorHands = FloorHands,
                Satchel = Kit.Satchel.ToList(),
                Taxes = Kit.Taxes,
                LuciferBag = Kit.LuciferBag,
                LuciferTaxes = Kit.LuciferTaxes
            };
        }
    }
}
