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
            game.StackTheDeal(DeckStarting(cards));
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
            Assert.AreEqual(49, ChapterRelics.New.Count, "Demo 2.3: thirteen class relics more");
            Assert.AreEqual(53, ChapterRelics.All.Count, "the demo's four and forty-nine");
            Assert.AreEqual(RelicRoster.IconOrder.Count, RelicRoster.IconOrder.Distinct().Count());
            foreach (ChapterRelic relic in ChapterRelics.New.Skip(20))
            {
                CollectionAssert.Contains(RelicRoster.IconOrder, relic.Id);
                Assert.IsNotNull(RelicRoster.Find(relic.Id));
            }
        }

        [Test]
        public void TheClassRelics_NeverInADraw_ThreeAClass_InOrder()
        {
            for (int seed = 0; seed < 400; seed++)
                foreach (string id in ChapterRelics.Draw(new SystemRandomSource(seed), new RelicWeights(1, 1, 1), 3, null, null, Peasant.ClassId))
                    Assert.IsFalse(ChapterRelics.IsClassRelic(id), "Demo 2.3: a class relic comes only by its own ways");
            foreach (SinnerClass cls in SinnerRoster.PhaseTwo)
            {
                IReadOnlyList<string> chain = ChapterRelics.ClassChain(cls.Id);
                Assert.AreEqual(3, chain.Count, cls.Id);
                CollectionAssert.AreEqual(new[] { RelicRarity.Common, RelicRarity.Rare, RelicRarity.Legendary }, chain.Select(ChapterRelics.RarityOf).ToArray(), cls.Id);
                var held = new HashSet<string>();
                foreach (string id in chain)
                {
                    Assert.AreEqual(id, ChapterRelics.NextClassRelic(cls.Id, held.Contains), "the common, then the rare, then the legendary");
                    held.Add(id);
                }
                Assert.IsNull(ChapterRelics.NextClassRelic(cls.Id, held.Contains), "all three held");
            }
        }

        [Test]
        public void TheClassRelics_EachAStepOfTheMainPower_WornNotBagged_AForgedOneTwo()
        {
            var peasant = Journey(SinnerRoster.Peasant).Run;
            Assert.AreEqual(0, peasant.Kit.Level);
            peasant.Effects.Give(ChapterRelicIds.RopeBelt);
            Assert.AreEqual(ClassKit.SatchelStart + 1, peasant.Kit.SatchelCapacity, "a step: a card more");
            peasant.Effects.Give(ChapterRelicIds.PeasantsScythe);
            Assert.AreEqual(2, peasant.Kit.Level);
            Assert.AreEqual(ClassKit.SatchelReRaisePercent + 5, peasant.Kit.SatchelRisk, "the scythe's curse");
            peasant.Effects.Forge(ChapterRelicIds.RopeBelt);
            Assert.AreEqual(3, peasant.Kit.Level, "a forged class relic: another step");
            Assert.AreEqual(2, RelicRoster.Combined(new[] { ChapterRelicIds.RopeBelt }, forged: new[] { ChapterRelicIds.RopeBelt }).Extra(RelicExtras.ClassStep),
                "Demo 2.3.1: the forge adds exactly one step to a class relic (itself one: two in all)");
            Assert.AreEqual(110 * 105 / 100, RelicRoster.Combined(new[] { ChapterRelicIds.BloodRing }, forged: new[] { ChapterRelicIds.BloodRing }).WinPercent,
                "another relic's forge: half its gift again, as before (110% → ×105%)");
            Assert.AreEqual(ClassKit.MaxLevel, peasant.Kit.Level);

            var king = Journey(SinnerRoster.King).Run;
            king.Effects.MaxCarried = 1;
            king.Effects.MaxBag = 5;
            king.Effects.Give(ChapterRelicIds.BloodRing);   // the one slot
            king.Effects.Give(ChapterRelicIds.KingsSignet);   // into the bag
            CollectionAssert.Contains(king.Effects.Bag, ChapterRelicIds.KingsSignet);
            Assert.AreEqual(ClassKit.TaxStart, king.Kit.TaxCapacity, "a class relic in the bag does nothing");
            Assert.IsTrue(king.Effects.Swap(ChapterRelicIds.BloodRing, ChapterRelicIds.KingsSignet));
            Assert.AreEqual(ClassKit.TaxStart + ClassKit.TaxStep, king.Kit.TaxCapacity, "worn: a step");
            Assert.AreEqual(ClassKit.SeizePrice + 1, king.Kit.SeizeFullPrice, "the signet's curse");

            var oracle = Journey(SinnerRoster.Find(Warlock.ClassId)).Run;
            int seen = oracle.Kit.ProphecyCards;
            oracle.Effects.Give(ChapterRelicIds.SeersSpectacles);
            Assert.AreEqual(seen + 1, oracle.Kit.ProphecyCards);
            var thief = Journey(SinnerRoster.Thief).Run;
            Assert.AreEqual(ClassKit.CaughtStart, thief.Kit.ThiefCaughtPercent);
            thief.Effects.Give(ChapterRelicIds.WornGloves);
            Assert.AreEqual(ClassKit.CaughtStart - ClassKit.CaughtStep, thief.Kit.ThiefCaughtPercent);
            var summoner = Journey(SinnerRoster.Summoner).Run;
            Assert.AreEqual(1, summoner.Kit.ImpsOpen);
            summoner.Effects.Give(ChapterRelicIds.ChalkCircle);
            Assert.AreEqual(2, summoner.Kit.ImpsOpen, "the Hand opens");
            Assert.AreEqual(2, Of(ChapterRelicIds.SummonersLeash).Extra(RelicExtras.ImpPrice));
            var jester = Journey(SinnerRoster.Jester).Run;
            Assert.AreEqual(0, jester.Kit.BustShieldsPerTable);
            jester.Effects.Give(ChapterRelicIds.BellCollar);
            Assert.AreEqual(ClassKit.ShieldStep, jester.Kit.BustShieldsPerTable, "Demo 2.3.1: two shields a step");
        }

        [Test]
        public void TheClassRelic_ComesByTheMarket_AndTheWarden_ManyInTheFirstChapter_NeverInTheLoot()
        {
            Assert.AreEqual(50, ChapterRelics.ClassRelicChance, "Demo 2.3.1: the first chapter's market");
            Assert.AreEqual(60, ChapterRelics.WardenClassRelicChance, "the first chapter's warden");
            Assert.AreEqual(15, ChapterRelics.LaterClassRelicChance, "later chapters");
            Assert.IsFalse(ChapterRelics.LootClassRelic);
            int chance = ChapterRelics.ClassRelicChance;
            ChapterRelics.ClassRelicChance = 100;
            try
            {
                ChapterRun run = Journey(SinnerRoster.Peasant, 7).Run;
                BlackMarket market = new BlackMarket(run, new SystemRandomSource(3), MarketVendor.Smuggler);
                Assert.AreEqual(ChapterRelicIds.RopeBelt, market.ClassRelic, "the market's stall: the class's first relic");
                Assert.IsTrue(market.Relics.Any(o => o.RelicId == ChapterRelicIds.RopeBelt));
                run.Effects.Give(ChapterRelicIds.RopeBelt);
                Assert.AreEqual(ChapterRelicIds.PeasantsScythe, run.NextClassRelic, "then the rare one");
                Assert.AreEqual(ChapterRelicIds.PeasantsScythe, run.RollClassRelic(new SystemRandomSource(1)));
                run.PayTribute();
                run.LeaveBossTable(0);
                Assert.IsFalse(run.LootOffers.Any(ChapterRelics.IsClassRelic), "a beaten demon's loot: no class relic (Demo 2.3.1)");
            }
            finally
            {
                ChapterRelics.ClassRelicChance = chance;
            }
            ChapterRelics.ClassRelicChance = 0;
            try
            {
                ChapterRun run = Journey(SinnerRoster.Peasant, 7).Run;
                Assert.IsNull(new BlackMarket(run, new SystemRandomSource(3), MarketVendor.Smuggler).ClassRelic);
            }
            finally
            {
                ChapterRelics.ClassRelicChance = chance;
            }
        }

        [Test]
        public void TheAshPurse_AFloorWinTwoCoinsMore_TheFirstAnteOneMore()
        {
            ChapterRun run = Journey().Run;
            run.Effects.Give(ChapterRelicIds.AshPurse);
            FloorTable table = FloorTable.Imp(run.Rules, run.Sinner, run.Effects, run.Purse, 1);
            Assert.AreEqual(run.Rules.AnteAt(1) + 1, table.AnteNow);
            table.Game.StackTheDeal(DeckStarting("AS KS QS JS 10S 2D 4H 6S 8C 10D"));
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
