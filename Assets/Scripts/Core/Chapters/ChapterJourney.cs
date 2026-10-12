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
        private static RunEffects PhaseTwoEffects() => new RunEffects { MaxCarried = ChapterRelics.Slots };

        /// <summary>The dice of the Seed (a Peasant's upgrade) at a chapter's start.</summary>
        public const int SeedStream = 22;

        public static int ChapterSeed(int seed, int chapter) => RandomSeeds.Derive(RandomSeeds.Derive(seed, ChapterStream), chapter);

        /// <summary>A new run as <paramref name="sinnerClass"/>: the first chapter, the class's purse.</summary>
        public static ChapterJourney Begin(SinnerClass sinnerClass, int seed)
        {
            if (sinnerClass == null) throw new ArgumentNullException(nameof(sinnerClass));
            var journey = new ChapterJourney(seed, new Sinner(sinnerClass), PhaseTwoEffects()) { Chapter = 1 };
            journey.Run = ChapterRun.Begin(ChapterRules.For(1), journey.Sinner, journey.Effects, ChapterSeed(seed, 1));
            return journey;
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
            ChapterRules rules = ChapterRules.For(Chapter);
            int floor = minimumPurse >= 0 ? minimumPurse : ChapterRules.StartingCoinsFor(Sinner.Id);
            int coins = Math.Max(floor, Run.Purse.Coins);
            PurseToppedUp = coins - Run.Purse.Coins;
            Run = new ChapterRun(rules, Sinner, Effects, BossShares.For(Sinner.Id, rules.BossId), coins, ChapterSeed(Seed, Chapter), kit: Kit);
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
            LuciferBarStart = barStart > 0 ? barStart : BossShares.For(Sinner.Id, Dealers.DealerRoster.LuciferId);
            Effects.SitAt(Dealers.DealerRoster.LuciferId, fresh: true);
            HellPokerGame game = BossTable.Create(template, BossTable.Lucifer, LuciferBarStart, 101, ChapterRules.LuciferWinPercent,
                ChapterRules.LuciferLossPercent, RandomSeeds.Derive(Seed, LuciferStream), Sinner, Sinner);
            game.UseEffects(Effects);
            Kit.EnterLucifer();
            LuciferHooks = ChapterHooks.Attach(game, Kit, Dealers.DealerRoster.LuciferId, LuciferBarStart, RandomSeeds.Derive(Seed, LuciferStream));
            if ((bar > 0 && bar != LuciferBarStart) || handsPlayed > 0) game.TakeOver(Math.Max(1, bar > 0 ? bar : LuciferBarStart), handsPlayed);
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
            if (save.Silenced != null) effects.SilenceCurse(save.Silenced);
            if (save.Amplified != null) effects.Amplify(save.Amplified);
            var sinner = new Sinner(sinnerClass, save.Charge, null, save.Ward,
                sinnerClass.StartingJokers > 0 ? Math.Max(sinnerClass.StartingJokers, save.Jokers) : 0);
            int chapter = Math.Max(1, Math.Min(ChapterRules.Chapters, save.Chapter));
            var journey = new ChapterJourney(save.Seed, sinner, effects) { Chapter = chapter, FloorHands = Math.Max(0, save.FloorHands) };
            for (int i = 0; i < 4 && i < save.BossHands.Count; i++) journey.BossHands[i] = Math.Max(0, save.BossHands[i]);
            ChapterRules rules = ChapterRules.For(chapter);
            journey.Run = new ChapterRun(rules, sinner, effects, Math.Max(1, save.Years), Math.Max(0, save.Coins), ChapterSeed(save.Seed, chapter));
            journey.Run.Restore(save.Trail, save.Years, save.Owed, TableMarks.Decode(save.Marks), save.Breaker, save.Rested, save.EventsSeen,
                save.Deck, save.Matches, save.WardenRelic, save.BossBarStart, save.BossBeaten, save.Loot, save.LootTaken, save.LootUpgrade, save.Whetted);
            journey.Run.Kit.Restore(save.Satchel, save.Taxes, save.LuciferBag, save.LuciferTaxes, save.Upgrades);
            if (save.Lucifer)
            {
                journey.Stage = JourneyStage.Lucifer;
                journey.LuciferBarStart = save.LuciferBarStart;
            }
            return journey;
        }

        /// <summary>The run as it stands, for the save (the driver adds where it is: the node's state, a match, a hand).</summary>
        public ChapterSave Capture()
        {
            ChapterRun run = Run;
            return new ChapterSave
            {
                ClassId = Sinner.Id,
                Seed = Seed,
                Chapter = Chapter,
                Lucifer = Stage == JourneyStage.Lucifer,
                LuciferBarStart = LuciferBarStart,
                Coins = run.Purse.Coins,
                Years = run.Years,
                Owed = run.YearsOwed,
                Charge = Sinner.Charge,
                Ward = Sinner.WardRaised,
                Jokers = Sinner.Jokers,
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
                Deck = run.DeckCards.ToList(),
                Matches = run.MatchesOpened,
                WardenRelic = run.WardenRelicWaiting,
                BossBarStart = run.BossBarStart,
                BossBeaten = run.BossBeaten,
                Loot = run.LootOffers.ToList(),
                LootTaken = run.LootTaken,
                LootUpgrade = run.LootUpgrade,
                Whetted = run.WhettedSoul,
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
