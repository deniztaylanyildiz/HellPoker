using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Phase 2 as a whole run: the chapters in a row, what goes along, the spoils, Lucifer and the ends, the save, the later
    /// chapters' maps, offers and wardens, the imp's purse cap.</summary>
    public class JourneyTests
    {
        private const string Royal = "AS KS QS JS 10S";
        private const string HouseNothing = "2D 4H 6S 8C 10D";
        private const string HouseQuads = "9C 9D 9H 9S 2C";
        private const string PlayerNothing = "3D 5C 7H JC KD";

        private static IReadOnlyList<Card> DeckStarting(string first)
        {
            var head = TestCards.Cards(first).ToList();
            return head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList();
        }

        private static Sinner NewPeasant() => new Sinner(SinnerRoster.Find(Sinners.Peasant.ClassId));

        private static FloorHand PlayOne(FloorTable table, BetAction afterDraw = BetAction.Pass)
        {
            table.Deal();
            HellPokerGame game = table.Game;
            if (game.Phase == GamePhase.PlayerReveal && game.CanCheckToDraw(out _)) game.CheckToDraw();
            if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
            bool first = true;
            while (game.Phase != GamePhase.RoundOver)
            {
                if (game.Phase == GamePhase.HouseReRaise) game.Bet(BetAction.Call);
                else if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                else
                {
                    game.Bet(first && game.CanBet(afterDraw, out _) ? afterDraw : BetAction.Pass);
                    first = false;
                }
            }
            return table.Settle();
        }

        /// <summary>The chapter's demon beaten, as if his bar were emptied.</summary>
        private static void BeatTheDemon(ChapterJourney journey)
        {
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
        }

        // ------------------------------------------------------------------ the chapters in a row

        [Test]
        public void ARunGoesFromChapterToChapter_CarryingThePurseRelicsChargeAndJokers()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Find(Jester.ClassId), 99);
            Assert.AreEqual(1, journey.Chapter);
            Assert.AreEqual(DealerRoster.MammonId, journey.Run.Rules.BossId);
            Assert.AreEqual(120, journey.Run.Purse.Coins, "the Jester's purse");
            Assert.AreEqual(1000, journey.Run.Years, "the Jester's share at Mammon");

            journey.Run.Purse.Add(200);
            journey.Effects.AddRelic(RelicIds.RustyCrown);
            journey.Effects.SilenceCurse(RelicIds.RustyCrown);
            journey.Sinner.ChangeJokers(1);
            BeatTheDemon(journey);
            Assert.IsFalse(journey.CanGoOn, "the spoils first");
            Assert.AreEqual(3, journey.Run.LootOffers.Count, "three relics to choose from");
            Assert.IsTrue(journey.Run.TakeLootCoins());
            Assert.IsTrue(journey.CanGoOn);
            int coins = journey.Run.Purse.Coins;

            ChapterRun belial = journey.NextChapter();
            Assert.AreEqual(2, journey.Chapter);
            Assert.AreEqual(DealerRoster.BelialId, belial.Rules.BossId);
            Assert.AreEqual(coins, belial.Purse.Coins, "the purse goes on, nothing added");
            Assert.AreEqual(1500, belial.Years, "the Jester's share at Belial");
            CollectionAssert.Contains(belial.Effects.Relics, RelicIds.RustyCrown, "the relics go on");
            Assert.IsNull(belial.Effects.SilencedCurse, "a silenced curse speaks again");
            Assert.AreEqual(3, journey.Sinner.Jokers, "the Jester's jokers go on");
            Assert.IsEmpty(belial.DeckCards, "a fresh deck");
            Assert.AreEqual(10, belial.Map.FloorCount);
            Assert.AreEqual(120 + 75, belial.Tribute, "the Jester's start + 75");
        }

        [Test]
        public void TheSpoils_ARelicOrTheCoins_ASwapWhenTheRunCarriesAllItMay()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 5);
            Assert.IsFalse(journey.Run.TakeLootCoins(), "nothing before the demon is beaten");
            journey.Effects.AddRelic(RelicIds.BoneDie);
            journey.Effects.AddRelic(RelicIds.RustyCrown);
            BeatTheDemon(journey);
            IReadOnlyList<string> offers = journey.Run.LootOffers;
            Assert.AreEqual(2, offers.Count, "only relics not carried (four offered relics, two carried)");
            Assert.IsFalse(journey.Run.TakeLoot(offers[0]), "two carried: one must go");
            Assert.IsTrue(journey.Run.TakeLoot(offers[0], RelicIds.RustyCrown));
            CollectionAssert.AreEquivalent(new[] { RelicIds.BoneDie, offers[0] }, journey.Effects.Relics);
            Assert.IsFalse(journey.Run.TakeLootCoins(), "once");
        }

        [Test]
        public void AfterLilith_Lucifer_NoSpoils_HisStakesOnTheBar_FallPastAQuarter()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 8);
            BeatTheDemon(journey);
            journey.Run.TakeLootCoins();
            journey.NextChapter();
            BeatTheDemon(journey);
            journey.Run.TakeLootCoins();
            journey.NextChapter();
            Assert.AreEqual(12, journey.Run.Map.FloorCount, "Lilith's night: twelve floors");
            BeatTheDemon(journey);
            Assert.IsTrue(journey.CanGoOn, "no spoils after Lilith");

            HellPokerGame game = journey.OpenLuciferTable(GameRules.Default);
            Assert.AreEqual(JourneyStage.Lucifer, journey.Stage);
            Assert.AreEqual(1050, game.Years, "the Peasant's share at Lucifer");
            Assert.IsTrue(game.Rules.IsFinalTable);
            Assert.AreEqual(100, game.UpcomingAnte, "his stakes measured against the bar, not 50");
            Assert.AreEqual(1312, BossTable.LuciferGate(1050));
            Assert.IsFalse(journey.IsCastDown(1312));
            Assert.IsTrue(journey.IsCastDown(1313), "past 125% of the start: cast down");
        }

        [Test]
        public void TheRunsEnds_AreCountedInTheirOwnRecords()
        {
            var records = new ChapterRecords();
            records.RunStarted();
            records.RunEnded(JourneyEnd.Freed, 4, Sinners.Peasant.ClassId, 120);
            records.RunStarted();
            records.RunEnded(JourneyEnd.CastDown, 4, King.ClassId, 90);
            records.RunStarted();
            records.RunEnded(JourneyEnd.PurseEmptied, 1, Jester.ClassId, 7);
            ChapterRecords back = ChapterRecords.Decode(records.Encode());
            Assert.AreEqual(3, back.Runs);
            Assert.AreEqual(1, back.Freed);
            Assert.AreEqual(1, back.CastDown);
            Assert.AreEqual(1, back.PurseEmptied);
            Assert.AreEqual(2, back.Damned);
            Assert.AreEqual(2, back.LuciferReached);
            Assert.AreEqual(4, back.DeepestChapter);
            Assert.AreEqual(120, back.FastestFreedom);
            Assert.AreEqual(1, back.FreedByClass[Sinners.Peasant.ClassId]);
            Assert.AreEqual(0, ChapterRecords.Decode("garbage").Runs);
        }

        // ------------------------------------------------------------------ the save

        [Test]
        public void ASavedRunComesBackTheSame()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Find(Warlock.ClassId), 31);
            ChapterRun run = journey.Run;
            run.MoveTo(run.Map[0, 2]);
            run.Purse.Add(17);
            run.OweAtGate(100);
            journey.Effects.AddRelic(RelicIds.FerrymansCoin);
            run.AddTableMarks(new TableMarks(openCard: true, openCardLiePercent: 25));
            journey.FloorHands = 9;

            ChapterSave save = journey.Capture();
            save.Pending = "market";
            Assert.IsTrue(ChapterSave.TryDecode(save.Encode(), out ChapterSave read));
            ChapterJourney back = ChapterJourney.Restore(read);
            Assert.AreEqual(journey.Sinner.Id, back.Sinner.Id);
            Assert.AreEqual(run.Purse.Coins, back.Run.Purse.Coins);
            Assert.AreEqual(run.Years, back.Run.Years);
            Assert.AreEqual(100, back.Run.YearsOwed);
            Assert.AreEqual(run.Current.Lane, back.Run.Current.Lane);
            Assert.AreEqual(run.Current.Floor, back.Run.Current.Floor);
            Assert.AreEqual(run.Map[3, 3].Kind, back.Run.Map[3, 3].Kind, "the same seed, the same map");
            CollectionAssert.AreEqual(journey.Effects.Relics, back.Effects.Relics);
            Assert.IsTrue(back.Run.NextTableMarks.OpenCard);
            Assert.AreEqual(25, back.Run.NextTableMarks.OpenCardLiePercent);
            Assert.AreEqual(9, back.FloorHands);
            Assert.AreEqual("market", read.Pending);

            Assert.IsFalse(ChapterSave.TryDecode("p2=99\nclass=peasant", out _), "another version");
            Assert.IsFalse(ChapterSave.TryDecode("p2=1\nclass=nobody", out _), "an unknown class");
            Assert.IsFalse(ChapterSave.TryDecode("rubbish", out _));
        }

        [Test]
        public void AFloorHandLeftInTheMiddleIsLost_ItsStakeGoesToTheImp()
        {
            var purse = new CoinPurse(50);
            FloorTable table = FloorTable.Imp(ChapterRules.For(1), NewPeasant(), new RunEffects(), purse, 3);
            table.Resume(2);
            int coins = table.ForfeitHand(12);
            Assert.AreEqual(-12, coins);
            Assert.AreEqual(38, purse.Coins);
            Assert.AreEqual(3, table.HandsPlayed);
            Assert.AreEqual(ChapterRules.For(1).ImpCoins + 12, table.HousePurse.Coins);
        }

        // ------------------------------------------------------------------ the imp's purse

        [Test]
        public void AnImpsPurseNeverHoldsMoreThanTwiceItsStart_TheRestGoesToTheVault()
        {
            var purse = new CoinPurse(500);
            ChapterRules rules = ChapterRules.For(1);
            FloorTable table = FloorTable.Imp(rules, NewPeasant(), new RunEffects(), purse, 1, floor: 0);
            Assert.AreEqual(10, table.HouseStart, "the first floor's imp: half a purse");
            for (int i = 0; i < 3 && !table.IsOver; i++)
            {
                table.Game.RestoreDeck(DeckStarting(PlayerNothing + " " + HouseQuads + " 2H 3H 4D"));
                PlayOne(table);
            }
            Assert.AreEqual(20, table.HousePurse.Coins, "capped at twice its start");
            Assert.Greater(table.CoinsToVault, 0);
        }

        // ------------------------------------------------------------------ the later chapters

        [Test]
        public void TheLaterChaptersMaps_TenAndTwelveFloors_TheirTreasureFloor_TwoWardens()
        {
            foreach (var (chapter, floors, treasure) in new[] { (2, 10, 5), (3, 12, 6) })
            {
                ChapterRules rules = ChapterRules.For(chapter);
                for (int seed = 0; seed < 60; seed++)
                {
                    ChapterMap map = ChapterMap.Generate(rules, new SystemRandomSource(seed));
                    Assert.AreEqual(floors, map.FloorCount);
                    Assert.That(map.Floors[0].All(n => n.Kind == NodeKind.Table));
                    Assert.That(map.Floors[treasure].All(n => n.Kind == NodeKind.Treasure), $"chapter {chapter}: the treasure on floor {treasure + 1}");
                    Assert.That(map.Floors[floors - 1].All(n => n.Kind == NodeKind.PurgatoryFire));
                    Assert.That(map.Floors.Take(2).SelectMany(f => f).All(n => n.Kind != NodeKind.BlackMarket));
                    Assert.That(map.Floors.Take(3).SelectMany(f => f).All(n => n.Kind != NodeKind.Warden));
                    Assert.LessOrEqual(map.Count(NodeKind.Warden), 2);
                }
            }
            Assert.AreEqual(25, ChapterRules.For(2).TreasureCoins);
            Assert.AreEqual(30, ChapterRules.For(3).TreasureCoins);
            Assert.AreEqual(5, ChapterRules.For(2).Ante);
            Assert.AreEqual(6, ChapterRules.For(3).Ante);
            Assert.AreEqual(30, ChapterRules.For(2).ImpCoins);
            Assert.AreEqual(80, ChapterRules.For(3).WardenCoins);
        }

        private static ChapterRun RunIn(int chapter, int coins = 200) =>
            new ChapterRun(ChapterRules.For(chapter), NewPeasant(), new RunEffects(), 1000, coins, 7);

        [Test]
        public void BelialsOffers_TheWitnessTheSpectacleTheFalseCoin()
        {
            ChapterRun run = RunIn(2);
            Assert.IsFalse(new LyingWitness().CanAppear(RunIn(1)), "Belial's chapter only");
            new LyingWitness().Accept(run);
            Assert.AreEqual(185, run.Purse.Coins);
            Assert.IsTrue(run.NextTableMarks.OpenCard);
            Assert.AreEqual(25, run.NextTableMarks.OpenCardLiePercent);

            run = RunIn(2);
            new Spectacle().Accept(run);
            Assert.IsTrue(run.NextTableMarks.HidesAll);
            Assert.AreEqual(150, run.NextTableMarks.WinPercent);

            run = RunIn(2);
            new FalseCoin().Accept(run);
            Assert.AreEqual(240, run.Purse.Coins);
            Assert.AreEqual(150, run.YearsOwed, "Belial's bar starts longer");
        }

        [Test]
        public void TheSpectaclesTable_ShowsNoCard_AndPaysHalfAgainUpToTheCap()
        {
            ChapterRules rules = ChapterRules.For(2);
            var payouts = new FloorPayoutTable(rules.Boss.Payouts, 3, 150);
            Assert.AreEqual(15, payouts.GetYearsForgiven(HandCategory.HighCard, 10, 5, int.MaxValue), "10 × 1.5");
            Assert.AreEqual(20, payouts.GetYearsForgiven(HandCategory.RoyalFlush, 10, 5, int.MaxValue), "never past the ×3 win: 10 + 5 × 2");
            FloorTable table = FloorTable.Imp(rules, NewPeasant(), new RunEffects(), new CoinPurse(100), 1,
                marks: new TableMarks(hidesAll: true, winPercent: 150));
            Assert.AreEqual(0, table.Game.Rules.HouseCardsShown, "the imp shows nothing");
        }

        [Test]
        public void LilithsOffers_TheBargainDesireInsomnia()
        {
            ChapterRun run = RunIn(3);
            new NightBargain().Accept(run);
            Assert.AreEqual(250, run.Purse.Coins);
            Assert.AreEqual(200, run.YearsOwed);

            run = RunIn(3);
            Assert.IsFalse(new Desire().CanAppear(run), "no relic, no desire");
            run.Effects.AddRelic(RelicIds.RustyCrown);
            Assert.IsTrue(new Desire().CanAppear(run));
            new Desire { RelicId = RelicIds.RustyCrown }.Accept(run);
            Assert.AreEqual(2, run.Effects.CombinedRelics.MaliceExtraPerHand, "the curse twice");
            Assert.AreEqual(105 * 105 / 100, run.Effects.CombinedRelics.WinPercent, "the gift twice");
            run.EndChapter();
            Assert.AreEqual(1, run.Effects.CombinedRelics.MaliceExtraPerHand, "until the chapter ends");

            run = RunIn(3);
            new Insomnia().Accept(run);
            Assert.AreEqual(3, run.NextTableMarks.FreeHands);
        }

        [Test]
        public void InsomniasFreeHands_NoAnteLost_NoRaise()
        {
            var purse = new CoinPurse(100);
            FloorTable table = FloorTable.Imp(ChapterRules.For(3), NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(PlayerNothing + " " + HouseQuads + " 2H 3H 4D 5D"), new TableMarks(freeHands: 3));
            Assert.AreEqual(3, table.FreeHandsLeft);
            table.Deal();
            Assert.IsFalse(table.Game.CanBet(BetAction.Raise, out _), "too tired to raise");
            HellPokerGame game = table.Game;
            while (game.Phase != GamePhase.RoundOver)
            {
                if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                else game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
            }
            FloorHand hand = table.Settle();
            Assert.AreEqual(-(game.Ante * 3) + game.Ante, hand.Coins, "quads take the ante ×3, the ante itself comes back");
            Assert.AreEqual(2, table.FreeHandsLeft);
        }

        [Test]
        public void TheWardensTricks_TheProphetLies_TheNurseVeilsADrawnCard()
        {
            ICheatPolicy prophet = FloorTable.WardenCheatsOf(ChapterRules.For(2));
            Assert.AreEqual(CheatIds.FalseFace, prophet.Cheats.Single().Id);
            ICheatPolicy nurse = FloorTable.WardenCheatsOf(ChapterRules.For(3));
            Assert.IsInstanceOf<NightNurseCheat>(nurse.Cheats.Single());
            Assert.AreEqual(CheatTiming.AfterDraw, nurse.Cheats.Single().Timing);
            Assert.AreEqual(ChapterCast.ProphetId, ChapterCast.WardenOf(ChapterRules.For(2)));
            Assert.AreEqual(ChapterCast.NurseId, ChapterCast.WardenOf(ChapterRules.For(3)));

            // The nurse veils one of the cards just drawn — never one kept.
            var table = new CheatTable(TestCards.Hand(PlayerNothing), TestCards.Hand(HouseNothing), new Deck(new FisherYatesShuffler(new SystemRandomSource(1))),
                HandEvaluator.CreateDefault(), new SystemRandomSource(2), new CheatMarks(), 10, drawnIndices: new[] { 1, 3 });
            CheatResult result = new NightNurseCheat().Apply(table);
            Assert.AreEqual(CheatOutcome.Played, result.Outcome);
            Assert.AreEqual(1, table.Marks.HiddenFromPlayer.Count);
            Card veiled = table.Marks.HiddenFromPlayer.Single();
            Assert.That(new[] { table.PlayerHand[1], table.PlayerHand[3] }, Does.Contain(veiled));
        }

        [Test]
        public void TheWitnessSometimesLies_TheOpenCardShowsAnotherFace()
        {
            int lies = 0, deals = 0;
            for (int seed = 0; seed < 80; seed++)
            {
                FloorTable table = FloorTable.Imp(ChapterRules.For(2), NewPeasant(), new RunEffects(), new CoinPurse(100), seed,
                    marks: new TableMarks(openCard: true, openCardLiePercent: 25));
                table.Deal();
                if (table.Game.HouseCardsRevealed == 0) continue;
                deals++;
                if (!table.OpenCardLies) continue;
                lies++;
                Assert.IsTrue(table.Game.IsHouseCardFalse(0));
                Assert.AreNotEqual(table.Game.HouseHand[0], table.Game.HouseCardFace(0));
            }
            Assert.Greater(lies, 5, "a lie one time in four");
            Assert.Less(lies, deals / 2);
        }
    }
}
