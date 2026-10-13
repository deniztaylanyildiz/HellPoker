using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.1's fifteen relics (24 → 39): each one's gift and curse where its rule is, the class relics' draws, the locks.</summary>
    public class NewRelicTests
    {
        private static ChapterJourney Journey(SinnerClass cls = null, int seed = 2) => ChapterJourney.Begin(cls ?? SinnerRoster.Peasant, seed);

        private static RelicEffects Of(string id) => RelicRoster.Combined(new[] { id });

        private static IReadOnlyList<Card> DeckStarting(string first)
        {
            var head = TestCards.Cards(first).ToList();
            return head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList();
        }

        private static RoundResult PlayOne(HellPokerGame game, string cards)
        {
            if (game.Phase == GamePhase.RoundOver) game.NextRound();
            game.RestoreDeck(DeckStarting(cards));
            game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal && game.CanCheckToDraw(out _)) game.CheckToDraw();
            while (game.Phase != GamePhase.RoundOver && !game.IsGameOver)
            {
                if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                else game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
            }
            return game.LastRound;
        }

        private const string Lose = "3D 5C 7H JC KD 9C 9D 9H 9S 2C";

        [Test]
        public void ThirtyNineRelics_EachWithWordsAndAnIcon()
        {
            Assert.AreEqual(35, ChapterRelics.New.Count);
            Assert.AreEqual(39, ChapterRelics.All.Count, "the demo's four and thirty-five");
            Assert.AreEqual(RelicRoster.IconOrder.Count, RelicRoster.IconOrder.Distinct().Count());
            foreach (ChapterRelic relic in ChapterRelics.New.Skip(20))
            {
                CollectionAssert.Contains(RelicRoster.IconOrder, relic.Id);
                Assert.IsNotNull(RelicRoster.Find(relic.Id));
            }
        }

        [Test]
        public void TheClassRelics_OnlyForTheirClass_ThreeTimesAsOften()
        {
            string[] classRelics = { ChapterRelicIds.JestersHat, ChapterRelicIds.SeersSpectacles, ChapterRelicIds.PeasantsScythe, ChapterRelicIds.KingsSignet,
                ChapterRelicIds.SummonersLeash };
            var counts = new Dictionary<string, int>();
            for (int seed = 0; seed < 400; seed++)
                foreach (string id in ChapterRelics.Draw(new SystemRandomSource(seed), new RelicWeights(0, 1, 0), 3, null, null, Peasant.ClassId))
                    counts[id] = (counts.TryGetValue(id, out int n) ? n : 0) + 1;
            Assert.IsFalse(classRelics.Where(id => id != ChapterRelicIds.PeasantsScythe).Any(counts.ContainsKey), "another class's: never");
            int scythe = counts[ChapterRelicIds.PeasantsScythe];
            double others = counts.Where(p => p.Key != ChapterRelicIds.PeasantsScythe).Average(p => p.Value);
            Assert.Greater(scythe, others * 1.8, "the Peasant's own: about three times as often");
        }

        [Test]
        public void TheClassRelics_BendTheKit()
        {
            var peasant = Journey(SinnerRoster.Peasant).Run;
            peasant.Effects.Give(ChapterRelicIds.PeasantsScythe);
            Assert.AreEqual(4, peasant.Kit.SatchelCapacity);
            Assert.AreEqual(ClassKit.SatchelReRaisePercent + 5, peasant.Kit.SatchelRisk);
            var king = Journey(SinnerRoster.King).Run;
            king.Effects.Give(ChapterRelicIds.KingsSignet);
            Assert.AreEqual(15, king.Kit.TaxCapacity);
            Assert.AreEqual(5, king.Kit.PriceOf(TaxAct.Seize));
            var warlock = Journey(SinnerRoster.Find(Warlock.ClassId)).Run;
            int seen = warlock.Kit.ProphecyCards;
            warlock.Effects.Give(ChapterRelicIds.SeersSpectacles);
            Assert.AreEqual(seen + 1, warlock.Kit.ProphecyCards);
            Assert.IsFalse(warlock.Kit.CanSendToBottom);
            Assert.AreEqual(-5, Of(ChapterRelicIds.SummonersLeash).Extra(RelicExtras.ImpWild));
            Assert.AreEqual(2, Of(ChapterRelicIds.SummonersLeash).Extra(RelicExtras.ImpPrice));
        }

        [Test]
        public void TheAshPurse_AFloorWinTwoCoinsMore_TheFirstAnteOneMore()
        {
            ChapterRun run = Journey().Run;
            run.Effects.Give(ChapterRelicIds.AshPurse);
            FloorTable table = FloorTable.Imp(run.Rules, run.Sinner, run.Effects, run.Purse, 1);
            Assert.AreEqual(run.Rules.AnteAt(1) + 1, table.AnteNow);
            table.Game.RestoreDeck(DeckStarting("AS KS QS JS 10S 2D 4H 6S 8C 10D"));
            int coins = run.Purse.Coins;
            table.Deal();
            HellPokerGame game = table.Game;
            while (game.Phase != GamePhase.RoundOver)
            {
                if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                else game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
            }
            FloorHand hand = table.Settle();
            Assert.IsTrue(hand.Won);
            Assert.AreEqual(coins + hand.Coins, run.Purse.Coins);
            Assert.AreEqual(run.Rules.AnteAt(2), table.AnteNow, "only the first hand's ante");
        }

        [Test]
        public void TheLantern_ShowsTheUnknown_TheMarketDearer()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                ChapterRun run = Journey(seed: seed).Run;
                MapNode unknown = run.Map.Floors.SelectMany(f => f).FirstOrDefault(n => n.Kind == NodeKind.Unknown);
                if (unknown == null) continue;
                Assert.AreEqual(NodeKind.Unknown, run.ShownKind(unknown));
                int price = run.PriceOf(45);
                run.Effects.Give(ChapterRelicIds.FerrymansLantern);
                Assert.AreEqual(unknown.Hidden, run.ShownKind(unknown));
                Assert.AreEqual(price * 110 / 100, run.PriceOf(45), 1);
                return;
            }
            Assert.Fail("no unknown node");
        }

        [Test]
        public void TheRustedChain_RestsHalfAgain_NoForge()
        {
            ChapterRun run = Journey().Run;
            int rest = run.RestYears;
            run.Effects.Give(ChapterRelicIds.RustedChain);
            Assert.AreEqual(rest * 3 / 2, run.RestYears, 1);
            Assert.IsEmpty(run.Forgeable);
        }

        [Test]
        public void TheLeadSeal_TheFirstLossAPush_WinsTenPercentLess()
        {
            ChapterJourney journey = Journey();
            journey.Effects.Give(ChapterRelicIds.LeadSeal);
            Assert.AreEqual(90, BossTable.WinWithRelics(100, journey.Effects.CombinedRelics, 0));
            journey.Run.PayTribute();
            HellPokerGame game = journey.Run.OpenBossTable(GameRules.Default);
            int bar = game.Years;
            PlayOne(game, Lose);
            Assert.IsTrue(game.SparedThisHand);
            Assert.AreEqual(bar + game.HookYearsThisHand, game.Years, "spared: only Mammon's interest");
            PlayOne(game, Lose);
            Assert.IsFalse(game.SparedThisHand, "the first one only");
            Assert.Greater(game.Years, bar);
        }

        [Test]
        public void TheBloodPact_TheBarHealsHalfAsMuch_TheSentenceAQuarterMore()
        {
            ChapterJourney journey = Journey();
            journey.Effects.Give(ChapterRelicIds.BloodPact);
            journey.Run.PayTribute();
            HellPokerGame game = journey.Run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(25, game.BarHealPercent, "a quarter of a loss, not half");
            Assert.AreEqual(125, game.SentenceLossPercent);
            int sentence = journey.Sentence.Years;
            RoundResult round = PlayOne(game, Lose);
            int lostOnBar = round.YearsChange - game.HookYearsThisHand;
            int onSentence = journey.Sentence.Years - sentence - game.HookYearsThisHand;
            Assert.Greater(lostOnBar, 0);
            Assert.Greater(onSentence, lostOnBar * 4, "the sentence takes the loss and a quarter more; the bar a quarter of it");
        }

        [Test]
        public void TheLiarsTongue_ReadsTraps_TheLettersHelpCostsYears()
        {
            ChapterRun run = Journey().Run;
            run.Effects.Give(ChapterRelicIds.LiarsTongue);
            Assert.IsTrue(ChoiceEventDeck.SeesTraps(run));
            Assert.AreEqual(51, run.HelpPercent(60), "60 less 15%");
            ChapterJourney journey = Journey();
            journey.Effects.Give(ChapterRelicIds.LostSoulsLetter);
            Assert.AreEqual(72, journey.Run.HelpPercent(60));
            int years = journey.Sentence.Years;
            new HellLottery().Choose(journey.Run, "ticket");
            Assert.AreEqual(years + 20, journey.Sentence.Years, "every offer taken");
        }

        [Test]
        public void LucifersFeather_TheSentenceATenthLonger_OneLossSparedARun()
        {
            ChapterJourney journey = Journey();
            int sentence = journey.Sentence.Years;
            journey.Effects.Give(ChapterRelicIds.LucifersFeather);
            Assert.AreEqual(sentence + sentence / 10, journey.Sentence.Years);
            journey.Run.PayTribute();
            HellPokerGame game = journey.Run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(1, game.SparesLeft);
            PlayOne(game, Lose);
            Assert.IsTrue(game.SparedThisHand);
            Assert.IsTrue(journey.Effects.FeatherSpent);
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            Assert.IsTrue(ChapterJourney.Restore(read).Effects.FeatherSpent, "once a run, saved");
        }

        [Test]
        public void MammonsGoldTooth_ThreePointsForEveryFifty_TheTributeFifteenPercentMore()
        {
            RelicEffects tooth = Of(ChapterRelicIds.MammonsGoldTooth);
            Assert.AreEqual(109, BossTable.WinWithRelics(100, tooth, 150));
            Assert.AreEqual(115, BossTable.WinWithRelics(100, tooth, 5000), "15 at most");
            Assert.AreEqual(115, tooth.TributePercent);
        }

        [Test]
        public void MammonsGoldTooth_CountsThePurseBroughtToTheGate_NotWhatTheTributeLeft_Saved()
        {
            ChapterJourney journey = Journey();
            ChapterRun run = journey.Run;
            run.Effects.Give(ChapterRelicIds.MammonsGoldTooth);
            Assert.AreEqual(-1, run.GateCoins, "not at the gate yet");
            run.Purse.Add(run.Tribute + 250 - run.Purse.Coins);
            run.PayTribute();
            Assert.AreEqual(run.Tribute + 250, run.GateCoins);
            Assert.Less(run.Purse.Coins, run.GateCoins);
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            Assert.AreEqual(run.GateCoins, ChapterJourney.Restore(read).Run.GateCoins);
            Assert.AreEqual(115, BossTable.WinWithRelics(100, run.Effects.CombinedRelics, run.GateCoins), "at least 250 brought: the most");
        }

        [Test]
        public void FiveOfTheNewOnesAreLocked()
        {
            var locked = new[] { ChapterRelicIds.BloodPact, ChapterRelicIds.TwinDice, ChapterRelicIds.LostSoulsLetter, ChapterRelicIds.MammonsGoldTooth,
                ChapterRelicIds.LucifersFeather };
            var fresh = new ChapterRecords();
            Assert.That(locked.All(id => !ChapterUnlocks.IsRelicOpen(id, fresh)));
            Assert.IsTrue(ChapterUnlocks.IsRelicOpen(ChapterRelicIds.AshPurse, fresh));
        }
    }
}
