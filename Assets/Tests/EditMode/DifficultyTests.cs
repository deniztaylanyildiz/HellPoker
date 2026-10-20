using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.4: the difficulty levels (Easy, Medium, Hard, Ironman) — what each turns on, the numbers, the locks, the save.</summary>
    public class DifficultyTests
    {
        private sealed class Always : IRandomSource
        {
            private readonly int _value;
            public Always(int value) { _value = value; }
            public int Next(int maxExclusive) => System.Math.Min(_value, maxExclusive - 1);
        }

        [TearDown]
        public void Restore()
        {
            ChapterUnlocks.AllClassesOpen = true;
            DifficultyRules.Reset();
        }

        // ------------------------------------------------------------------ which rules a level has

        [Test]
        public void EveryLevel_CarriesTheRulesOfTheOneBefore()
        {
            bool[] Rules(Difficulty d) => new[]
            {
                DifficultyRules.FasterAnte(d), DifficultyRules.TributeFromPurse(d), DifficultyRules.Rage(d), DifficultyRules.SaferMapGone(d),
                DifficultyRules.ShortTopUp(d), DifficultyRules.HeavierLoss(d), DifficultyRules.FasterMalice(d), DifficultyRules.StackedRules(d)
            };
            CollectionAssert.AreEqual(new[] { false, false, false, false, false, false, false, false }, Rules(Difficulty.Easy), "Easy: none");
            CollectionAssert.AreEqual(new[] { true, true, true, true, false, false, false, false }, Rules(Difficulty.Medium), "Medium: 1-4");
            CollectionAssert.AreEqual(new[] { true, true, true, true, true, true, false, false }, Rules(Difficulty.Hard), "Hard: 1-6");
            CollectionAssert.AreEqual(new[] { true, true, true, true, true, true, true, true }, Rules(Difficulty.Ironman), "Ironman: 1-8");
            Assert.IsTrue(DifficultyRules.SavesOnlyAtCamps(Difficulty.Ironman));
            Assert.IsFalse(DifficultyRules.SavesOnlyAtCamps(Difficulty.Hard));
            Assert.AreEqual(Difficulty.Medium, DifficultyRules.Default);
            foreach (Difficulty d in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Ironman })
                Assert.AreEqual(d, DifficultyRules.Parse(DifficultyRules.IdOf(d)));
            Assert.AreEqual(Difficulty.Easy, DifficultyRules.Parse(null), "no difficulty: Easy");
        }

        [Test]
        public void Easy_ChangesNoNumber_AndDrawsTheSameMaps()
        {
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                ChapterRules designed = ChapterRules.For(chapter), easy = designed.WithDifficulty(Difficulty.Easy);
                Assert.AreEqual(designed.AnteStepHands, easy.AnteStepHands);
                Assert.AreEqual(designed.PricePercent, easy.PricePercent);
                for (int seed = 0; seed < 50; seed++)
                {
                    ChapterMap a = ChapterMap.Generate(designed, new SystemRandomSource(seed));
                    ChapterMap b = ChapterMap.Generate(easy, new SystemRandomSource(seed));
                    Assert.AreEqual(Describe(a), Describe(b), $"chapter {chapter}, seed {seed}");
                }
            }
        }

        private static string Describe(ChapterMap map) =>
            string.Join("|", map.Floors.Select(f => string.Join(",", f.Select(n => n.Kind + ":" + n.Hidden + ":" + string.Join(".", n.Next)))));

        // ------------------------------------------------------------------ Medium

        [Test]
        public void Medium_TheAnteGrowsEveryThreeTwoOneHands()
        {
            int[] expected = { 3, 2, 1 };
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                Assert.AreEqual(3, ChapterRules.For(chapter).AnteStepHands, "Easy: every three");
                foreach (Difficulty d in new[] { Difficulty.Medium, Difficulty.Hard, Difficulty.Ironman })
                    Assert.AreEqual(expected[chapter - 1], ChapterRules.For(chapter).WithDifficulty(d).AnteStepHands, $"{d}, chapter {chapter}");
            }
            ChapterRules lilith = ChapterRules.For(3).WithDifficulty(Difficulty.Medium);
            Assert.AreEqual(lilith.Ante, lilith.AnteAt(1), "the first ante the same");
            Assert.AreEqual(lilith.Ante + 1, lilith.AnteAt(2), "Lilith: one more every hand");
        }

        [Test]
        public void Medium_TheTributeIsTheBiggerOfTheFormulaAndAShareOfThePurse()
        {
            ChapterJourney easy = ChapterJourney.Begin(SinnerRoster.Peasant, 3);
            ChapterJourney medium = ChapterJourney.Begin(SinnerRoster.Peasant, 3, Difficulty.Medium);
            int formula = easy.Run.Tribute;
            Assert.AreEqual(formula, medium.Run.Tribute, "a small purse: the formula");
            medium.Run.Purse.Add(2000);
            easy.Run.Purse.Add(2000);
            int purse = medium.Run.Purse.Coins;
            Assert.AreEqual(purse * 40 / 100, medium.Run.Tribute, "Mammon: 40% of a rich purse");
            Assert.AreEqual(formula, easy.Run.Tribute, "Easy never");
            GateToll toll = medium.Run.PayTribute();
            Assert.AreEqual(purse * 40 / 100, toll.Paid, "paid at the gate from the purse brought");
            Assert.AreEqual(purse * 40 / 100, medium.Run.Tribute, "it stays the gate's");
        }

        [Test]
        public void Medium_TheDemonRagesOnceUnderHalfHisBar_TheStakesTimesOneAndAHalf()
        {
            var (game, hooks) = BossTable(SinnerRoster.Peasant, 1, Difficulty.Medium);
            DemonRule rule = hooks.Rule;
            int start = rule.BarStart;
            Assert.IsTrue(rule.CanRage);
            Assert.AreEqual(start, game.FixedStakeYears);
            game.TakeOver(start / 2 + 1, 3);
            rule.CheckRage(game);
            Assert.IsFalse(rule.Enraged, "at half and above: calm");
            game.TakeOver(start / 2 - 1, 4);
            rule.CheckRage(game);
            Assert.IsTrue(rule.Enraged);
            Assert.IsTrue(rule.EnragedThisHand, "the table is told once");
            Assert.AreEqual(start * 150 / 100, game.FixedStakeYears, "the stakes ×1.5");
            game.TakeOver(start, 5);
            rule.CheckRage(game);
            Assert.IsTrue(rule.Enraged, "for good");
            Assert.AreEqual(start * 150 / 100, game.FixedStakeYears, "raised once");

            var (easyGame, easyHooks) = BossTable(SinnerRoster.Peasant, 1, Difficulty.Easy);
            easyGame.TakeOver(10, 3);
            easyHooks.Rule.CheckRage(easyGame);
            Assert.IsFalse(easyHooks.Rule.Enraged, "Easy: never");
        }

        [Test]
        public void TheRage_MakesEveryRuleComeMoreOften()
        {
            var interest = new MammonInterest(1000, new Always(0)) { CanRage = true };
            var game = BossTable(SinnerRoster.Peasant, 1, Difficulty.Medium).game;
            int calm = interest.Interest;
            interest.CheckRage(Bar(game, 1000, 400));
            Assert.IsTrue(interest.Enraged);
            Assert.AreEqual(calm * 150 / 100, interest.Interest, "Mammon's interest ×1.5");

            var swap = new BelialRoleSwap(1000, new Always(0)) { CanRage = true };
            Assert.IsTrue(swap.SwapsIn(4) && !swap.SwapsIn(2));
            swap.CheckRage(Bar(game, 1000, 400));
            Assert.IsTrue(swap.SwapsIn(2), "Belial: every second hand");
        }

        /// <summary>A game standing at <paramref name="bar"/> of a <paramref name="start"/> bar.</summary>
        private static HellPokerGame Bar(HellPokerGame game, int start, int bar)
        {
            game.TakeOver(bar, game.RoundNumber);
            return game;
        }

        [Test]
        public void Medium_TheMap_FewerCamps_DearerMarket_MoreWardens_LilithsEveryPath()
        {
            Assert.AreEqual(120 * 120 / 100, ChapterRules.For(2).WithDifficulty(Difficulty.Medium).PricePercent, "Belial ×1.2");
            Assert.AreEqual(140 * 140 / 100, ChapterRules.For(3).WithDifficulty(Difficulty.Medium).PricePercent, "Lilith ×1.4");
            Assert.AreEqual(100, ChapterRules.For(1).WithDifficulty(Difficulty.Medium).PricePercent, "Mammon ×1");
            int[] camps = { 2, 1, 1 };
            int unknownWardensEasy = 0, unknownWardensMedium = 0, unknownsEasy = 0, unknownsMedium = 0;
            for (int chapter = 1; chapter <= 3; chapter++)
                for (int seed = 0; seed < 400; seed++)
                {
                    ChapterRules rules = ChapterRules.For(chapter).WithDifficulty(Difficulty.Medium);
                    ChapterMap map = ChapterMap.Generate(rules, new SystemRandomSource(seed));
                    int middle = map.Floors.Take(map.FloorCount - 1).Sum(f => f.Count(n => n.Kind == NodeKind.PurgatoryFire));
                    Assert.LessOrEqual(middle, camps[chapter - 1], $"chapter {chapter} seed {seed}: the middle camps");
                    Assert.IsTrue(map.Floors.Take(ChapterMap.WardenFromFloor).All(f => f.All(n => !IsWarden(n))), "no warden before the fourth floor");
                    if (chapter == 3) Assert.IsTrue(EveryPathMeetsAWarden(map), $"Lilith seed {seed}: every path meets a warden");
                    if (chapter == 3)
                    {
                        ChapterMap easy = ChapterMap.Generate(ChapterRules.For(3), new SystemRandomSource(seed));
                        unknownsEasy += easy.Floors.Sum(f => f.Count(n => n.Kind == NodeKind.Unknown && n.Floor >= ChapterMap.WardenFromFloor));
                        unknownWardensEasy += easy.Floors.Sum(f => f.Count(n => n.Kind == NodeKind.Unknown && n.Hidden == NodeKind.Warden));
                        unknownsMedium += map.Floors.Sum(f => f.Count(n => n.Kind == NodeKind.Unknown && n.Floor >= ChapterMap.WardenFromFloor));
                        unknownWardensMedium += map.Floors.Sum(f => f.Count(n => n.Kind == NodeKind.Unknown && n.Hidden == NodeKind.Warden));
                    }
                }
            Assert.Greater(unknownWardensMedium / (float)unknownsMedium, unknownWardensEasy / (float)unknownsEasy * 2,
                "Lilith's \"?\" a warden 15% of the time (Easy: 5%)");
        }

        private static bool IsWarden(MapNode n) => n.Kind == NodeKind.Warden || (n.Kind == NodeKind.Unknown && n.Hidden == NodeKind.Warden);

        private static bool EveryPathMeetsAWarden(ChapterMap map)
        {
            // From the bottom up: can a node reach the last floor without a warden?
            var free = new Dictionary<MapNode, bool>();
            for (int f = map.FloorCount - 1; f >= 0; f--)
                foreach (MapNode n in map.Floors[f])
                    free[n] = !IsWarden(n) && (f == map.FloorCount - 1 || map.NextFrom(n).Any(m => free[m]));
            return map.Floors[0].All(n => !free[n]);
        }

        // ------------------------------------------------------------------ Hard

        [Test]
        public void Hard_ThePurseIsToppedUpToThreeQuartersThenHalf()
        {
            int start = ChapterRules.StartingCoinsFor(Peasant.ClassId);
            foreach (var (difficulty, belial, lilith) in new[] { (Difficulty.Medium, 100, 100), (Difficulty.Hard, 75, 50), (Difficulty.Ironman, 50, 25) })
            {
                ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 5, difficulty);
                Empty(journey);
                journey.NextChapter();
                Assert.AreEqual(start * belial / 100, journey.Run.Purse.Coins, $"{difficulty}: Belial's chapter");
                Empty(journey);
                journey.NextChapter();
                Assert.AreEqual(start * lilith / 100, journey.Run.Purse.Coins, $"{difficulty}: Lilith's chapter");
            }
            ChapterJourney rich = ChapterJourney.Begin(SinnerRoster.Peasant, 5, Difficulty.Hard);
            rich.Run.Purse.Add(500);
            rich.Run.PayTribute();
            rich.Run.LeaveBossTable(0);
            rich.Run.TakeLootCoins();
            int coins = rich.Run.Purse.Coins;
            rich.NextChapter();
            Assert.AreEqual(coins, rich.Run.Purse.Coins, "a richer purse is left alone");
        }

        /// <summary>The chapter's demon beaten, his coins taken, the purse empty: on to the next.</summary>
        private static void Empty(ChapterJourney journey)
        {
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
            journey.Run.TakeLootCoins();
            journey.Run.Purse.Add(-journey.Run.Purse.Coins);
        }

        [Test]
        public void Hard_ALossToADemonHurtsMore_TheWinTheSame()
        {
            int[] factors = { 100, 125, 150 };
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                var easy = (ScaledPayoutTable)BossTable(SinnerRoster.Peasant, chapter, Difficulty.Medium).game.Payouts;
                var hard = (ScaledPayoutTable)BossTable(SinnerRoster.Peasant, chapter, Difficulty.Hard).game.Payouts;
                Assert.AreEqual(easy.LossScalePercent * factors[chapter - 1] / 100, hard.LossScalePercent, $"chapter {chapter}: the loss");
                Assert.AreEqual(easy.WinScalePercent, hard.WinScalePercent, $"chapter {chapter}: the win the same");
            }
            Assert.AreEqual(150, DifficultyRules.BossLossFor(Difficulty.Hard, 3), "Lucifer ×1.5");
            Assert.AreEqual(100, DifficultyRules.BossLossFor(Difficulty.Medium, 3));
            CollectionAssert.AreEqual(new[] { 125, 150, 175, 175 }, Enumerable.Range(0, 4).Select(b => DifficultyRules.BossLossFor(Difficulty.Ironman, b)),
                "Ironman's own, heavier still");
        }

        // ------------------------------------------------------------------ Ironman

        [Test]
        public void Ironman_TheMaliceGaugeIsShorter_NeverLonger()
        {
            int[] most = { 3, 2, 2 };
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                Dealer boss = ChapterRules.For(chapter).Boss;
                HellPokerGame iron = BossTable(SinnerRoster.Peasant, chapter, Difficulty.Ironman).game;
                HellPokerGame hard = BossTable(SinnerRoster.Peasant, chapter, Difficulty.Hard).game;
                Assert.AreEqual(boss.MaliceMax, hard.MaliceMax, $"chapter {chapter}: Hard keeps the gauge");
                Assert.AreEqual(System.Math.Min(boss.MaliceMax, most[chapter - 1]), iron.MaliceMax, $"chapter {chapter}: Ironman");
            }
        }

        [Test]
        public void Ironman_BelialCarriesTheInterest_LilithTheRoleSwap_AtHalfStrength()
        {
            Assert.IsNull(BossTable(SinnerRoster.Peasant, 2, Difficulty.Hard).hooks.Carried, "Hard: none");
            Assert.IsNull(BossTable(SinnerRoster.Peasant, 1, Difficulty.Ironman).hooks.Carried, "Mammon: none");
            var (belialGame, belial) = BossTable(SinnerRoster.Peasant, 2, Difficulty.Ironman);
            var interest = belial.Carried as MammonInterest;
            Assert.IsNotNull(interest);
            Assert.IsTrue(interest.Carried);
            Assert.AreEqual(50, interest.StrengthPercent);
            var full = new MammonInterest(interest.BarStart, new Always(0));
            Assert.AreEqual(System.Math.Max(1, (full.BarStart * MammonInterest.InterestPercent * 50 / 100 + 99) / 100), interest.Interest, "half the interest");
            Assert.IsFalse(interest.CanRage, "a carried rule never rages");
            Assert.AreEqual(0, interest.ExtraMalicePerHand(belialGame), "nor corners its demon twice");
            var swap = BossTable(SinnerRoster.Peasant, 3, Difficulty.Ironman).hooks.Carried as BelialRoleSwap;
            Assert.IsNotNull(swap);
            Assert.IsFalse(swap.SwapsIn(4), "half as often: not every fourth");
            Assert.IsTrue(swap.SwapsIn(8), "every eighth hand");
        }

        // ------------------------------------------------------------------ the locks and the records

        [Test]
        public void TheLevels_OpenWithFreedoms_AndAllClassesOpenOpensThemAll()
        {
            ChapterUnlocks.AllClassesOpen = false;
            var book = new ChapterRecords();
            Assert.IsTrue(book.IsDifficultyOpen(Difficulty.Easy) && book.IsDifficultyOpen(Difficulty.Medium));
            Assert.IsFalse(book.IsDifficultyOpen(Difficulty.Hard));
            book.RunEnded(JourneyEnd.Freed, 4, King.ClassId, 90, Difficulty.Easy);
            Assert.IsFalse(book.IsDifficultyOpen(Difficulty.Hard), "an Easy freedom opens nothing");
            book.RunEnded(JourneyEnd.CastDown, 4, King.ClassId, 90, Difficulty.Medium);
            Assert.IsFalse(book.IsDifficultyOpen(Difficulty.Hard), "a fall opens nothing");
            book.RunEnded(JourneyEnd.Freed, 4, King.ClassId, 90, Difficulty.Medium);
            Assert.IsTrue(book.IsDifficultyOpen(Difficulty.Hard), "any class on Medium opens Hard");
            Assert.IsFalse(book.IsDifficultyOpen(Difficulty.Ironman));
            ChapterRecords read = ChapterRecords.Decode(book.Encode());
            Assert.IsTrue(read.IsDifficultyOpen(Difficulty.Hard), "kept");
            read.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 90, Difficulty.Hard, 1200);
            Assert.IsTrue(read.IsDifficultyOpen(Difficulty.Ironman), "Hard opens Ironman");
            Assert.AreEqual(1200, read.LowestSentence(Peasant.ClassId, Difficulty.Hard));
            Assert.AreEqual(1, read.FreedAt(Peasant.ClassId, Difficulty.Hard));
            Assert.AreEqual(0, read.FreedAt(Peasant.ClassId, Difficulty.Medium));
            ChapterUnlocks.AllClassesOpen = true;
            Assert.IsTrue(new ChapterRecords().IsDifficultyOpen(Difficulty.Ironman), "AllClassesOpen: every level");
        }

        [Test]
        public void AnOldBook_ItsCircleRecordsBecomeTheEasyOnes_TheBest()
        {
            string old = "p2r=1\nruns=4\nfreed=3\nfreed.peasant=3\ncircle.peasant=2\nsentence.peasant.0=900\nsentence.peasant.2=610\nsentence.king.1=1500";
            ChapterRecords book = ChapterRecords.Decode(old);
            Assert.AreEqual(610, book.LowestSentence(Peasant.ClassId, Difficulty.Easy), "the best of the circles");
            Assert.AreEqual(1500, book.LowestSentence(King.ClassId, Difficulty.Easy));
            Assert.AreEqual(3, book.FreedAt(Peasant.ClassId, Difficulty.Easy), "the freedoms count as Easy ones");
            StringAssert.DoesNotContain("circle.", book.Encode(), "nothing of the circles written again");
        }

        // ------------------------------------------------------------------ the save

        [Test]
        public void TheSave_KeepsTheDifficultyTheRunsIdAndItsCount_AnOldSaveIsEasy()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Thief, 77, Difficulty.Ironman);
            journey.SaveCount = 6;
            ChapterSave save = journey.Capture();
            Assert.IsTrue(ChapterSave.TryDecode(save.Encode(), out ChapterSave read));
            Assert.AreEqual(Difficulty.Ironman, read.Difficulty);
            Assert.AreEqual(journey.RunId, read.RunId);
            Assert.AreEqual(6, read.SaveCount);
            ChapterJourney back = ChapterJourney.Restore(read);
            Assert.AreEqual(Difficulty.Ironman, back.Difficulty);
            Assert.AreEqual(Difficulty.Ironman, back.Run.Rules.Difficulty, "the chapter's rules too");
            Assert.AreEqual(journey.RunId, back.RunId);

            // A Demo 3.3 save (version 1, a circle): read, as Easy.
            string old = string.Join("\n", ChapterJourney.Begin(SinnerRoster.King, 5).Capture().Encode().Split('\n')
                .Select(l => l.StartsWith("p2=") ? "p2=1" : l.StartsWith("difficulty=") ? "circle=3" : l)
                .Where(l => !l.StartsWith("run.id=") && !l.StartsWith("saves=")));
            Assert.IsTrue(ChapterSave.TryDecode(old, out ChapterSave older), "an old save loads");
            Assert.AreEqual(Difficulty.Easy, older.Difficulty);
            ChapterJourney resumed = ChapterJourney.Restore(older);
            Assert.AreEqual(Difficulty.Easy, resumed.Difficulty);
            Assert.IsFalse(string.IsNullOrEmpty(resumed.RunId), "a run without an id gets one");
            Assert.AreEqual(2, ChapterSave.Version);
        }

        [Test]
        public void Ironman_ADeadRunNeverLoads_NorAnOlderCopyOfItsSave()
        {
            var book = new ChapterRecords();
            Assert.IsTrue(book.MayLoad("abc", 1));
            book.RunSaved("abc", 4);
            Assert.IsTrue(book.MayLoad("abc", 4));
            Assert.IsFalse(book.MayLoad("abc", 3), "an older copy put back");
            book.RunDied("abc");
            Assert.IsFalse(book.MayLoad("abc", 9), "a dead run");
            ChapterRecords read = ChapterRecords.Decode(book.Encode());
            Assert.IsFalse(read.MayLoad("abc", 9), "remembered");
            Assert.IsTrue(read.MayLoad("other", 1));
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A chapter's demon's table at <paramref name="difficulty"/>, its tribute paid.</summary>
        private static (HellPokerGame game, ChapterHooks hooks) BossTable(SinnerClass cls, int chapter, Difficulty difficulty)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, 9, difficulty);
            for (int c = 1; c < chapter; c++)
            {
                journey.Run.Purse.Add(10000);
                journey.Run.PayTribute();
                journey.Run.LeaveBossTable(0);
                journey.Run.TakeLootCoins();
                journey.NextChapter();
            }
            journey.Run.Purse.Add(10000);
            journey.Run.PayTribute();
            HellPokerGame game = journey.Run.OpenBossTable(GameRules.Default);
            return (game, journey.Run.Hooks);
        }
    }
}
