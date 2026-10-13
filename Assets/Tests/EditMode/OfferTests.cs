using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.1's offers: the choices and their dice, the chapters' scale, the swindlers' hints, the Warlock and the Summoner, once a
    /// run, what waits at the gate, the pickpocket's chase, the ferry, the save.</summary>
    public class OfferTests
    {
        private static ChapterJourney Journey(SinnerClass cls = null, int seed = 3) => ChapterJourney.Begin(cls ?? SinnerRoster.Peasant, seed);

        /// <summary>A chapter run of <paramref name="chapter"/> inside a journey (the sentence along), with a full purse.</summary>
        private static (ChapterJourney journey, ChapterRun run) At(int chapter, SinnerClass cls = null, int coins = 300)
        {
            ChapterJourney journey = Journey(cls);
            for (int c = 1; c < chapter; c++)
            {
                journey.Run.PayTribute();
                journey.Run.LeaveBossTable(0);
                journey.Run.TakeLootCoins();
                journey.NextChapter();
            }
            journey.Run.Purse.Add(coins - journey.Run.Purse.Coins);
            journey.Run.MoveTo(journey.Run.Choices.First());
            return (journey, journey.Run);
        }

        [Test]
        public void EveryOffer_HasItsChoices_AndAllButAnEncounterCanBePassed()
        {
            var (_, run) = At(1);
            run.Effects.AddRelic(RelicIds.RustyCrown);
            foreach (IChoiceEvent offer in ChoiceEventDeck.All)
            {
                IReadOnlyList<string> choices = offer.Choices(run);
                Assert.That(choices.Count, Is.InRange(2, 4), offer.Id);
                Assert.AreEqual(!offer.IsEncounter, choices.Contains(EventChoices.Pass), $"{offer.Id}: pass unless it happens to you");
                Assert.IsNotNull(FloorEventDeck.Find(offer.Id), "in the deck");
            }
            Assert.AreEqual(16, ChoiceEventDeck.All.Count);
            Assert.AreEqual(6, ChoiceEventDeck.All.Count(e => e.Family == EventFamily.Help));
            Assert.AreEqual(4, ChoiceEventDeck.All.Count(e => e.Family == EventFamily.Scam));
            Assert.AreEqual(6, ChoiceEventDeck.All.Count(e => e.Family == EventFamily.Hell));
        }

        [Test]
        public void TheChaptersScale_OneAQuarterAndAHalf()
        {
            Assert.AreEqual(20, At(1).run.Scaled(20));
            Assert.AreEqual(25, At(2).run.Scaled(20));
            Assert.AreEqual(30, At(3).run.Scaled(20));
            var (_, run) = At(3);
            Assert.AreEqual(20 * 3 / 2, new ChainedSoul().Cost(run, "pay"), "the third chapter: the file costs half again");
        }

        [Test]
        public void TheChainedSoul_GratefulOrAnImp_ByTheDice()
        {
            int grateful = 0, imps = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var journey = Journey(seed: seed);
                ChapterRun run = journey.Run;
                run.Purse.Add(100);
                int coins = run.Purse.Coins, years = journey.Sentence.Years;
                EventOutcome o = new ChainedSoul().Choose(run, "pay");
                Assert.AreEqual(-20, run.Purse.Coins - coins, "the file's price");
                if (o.Result == "imp")
                {
                    imps++;
                    Assert.AreEqual(years, journey.Sentence.Years);
                }
                else
                {
                    grateful++;
                    if (o.Result == "years") Assert.AreEqual(years - 100, journey.Sentence.Years, "years off the sentence");
                    else CollectionAssert.Contains(run.Effects.Relics, o.Relic);
                }
            }
            Assert.AreEqual(120, grateful, 25, "six in ten");
            Assert.Greater(imps, 40);
        }

        [Test]
        public void TheFallenAngel_YearsOnTheSentence_ForARareRelicOrAnUpgrade()
        {
            var (journey, run) = At(1);
            int years = journey.Sentence.Years;
            EventOutcome relic = new FallenAngel().Choose(run, "relic");
            Assert.AreEqual(RelicRarity.Rare, ChapterRelics.RarityOf(relic.Relic));
            Assert.AreEqual(years + 150, journey.Sentence.Years);
            Assert.AreEqual(150, run.YearsOwed, "on the demon's bar at the gate");
            EventOutcome upgrade = new FallenAngel().Choose(run, "upgrade");
            CollectionAssert.Contains(run.Kit.Upgrades, upgrade.Upgrade);
        }

        [Test]
        public void TheHungryGambler_TheNextImpsFirstHandShowsTwoCards()
        {
            var (_, run) = At(1);
            new HungryGambler().Choose(run, "pay");
            Assert.AreEqual(2, run.NextTableMarks.FirstHandOpen);
            FloorTable table = FloorTable.Imp(run.Rules, run.Sinner, run.Effects, run.Purse, 1, marks: run.NextTableMarks);
            table.Deal();
            Assert.AreEqual(2, table.Game.HouseCardsRevealed, "two of his cards");
            Assert.AreEqual("1,0,0,100,0,2,0", new TableMarks(openCard: true, firstHandOpen: 2).Encode());
            Assert.AreEqual(2, TableMarks.Decode("0,0,0,100,0,2,-1").FirstHandOpen);
            Assert.AreEqual(-1, TableMarks.Decode("0,0,0,100,0,2,-1").AnteDelta);
            Assert.IsTrue(TableMarks.Decode("1,25,0,100,0").OpenCard, "an older save's five numbers");
        }

        [Test]
        public void TheDebtorImp_PaysBackAtTheGate_OrIsGone()
        {
            int paid = 0, gone = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                ChapterRun run = Journey(seed: seed).Run;
                run.Purse.Add(200);
                new DebtorImp().Choose(run, "lend");
                Assert.AreEqual(60, run.LoanDue);
                int before = run.Purse.Coins;
                GateArrival arrival = run.ArriveAtGate();
                Assert.AreSame(arrival, run.ArriveAtGate(), "once");
                if (arrival.LoanRepaid > 0)
                {
                    paid++;
                    Assert.AreEqual(before + 60, run.Purse.Coins);
                }
                else
                {
                    gone++;
                    Assert.AreEqual(0, arrival.LoanRepaid);
                }
            }
            Assert.AreEqual(70, paid, 15, "seven in ten");
            Assert.Greater(gone, 10);
        }

        [Test]
        public void TheLostSoul_RevealsTheNextTwoFloors_TheNextTableCostsOneMore()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var (_, run) = At(1, coins: 300);
                ChapterJourney j = Journey(seed: seed);
                run = j.Run;
                run.MoveTo(run.Choices.First());
                var ahead = run.Map.Floors.Skip(run.Current.Floor + 1).Take(2).SelectMany(f => f).Where(n => n.Kind == NodeKind.Unknown).ToList();
                if (ahead.Count == 0) continue;
                new LostSoul().Choose(run, "guide");
                Assert.That(ahead.All(n => run.ShownKind(n) == n.Hidden));
                Assert.AreEqual(1, run.NextTableMarks.AnteDelta);
                FloorTable table = FloorTable.Imp(run.Rules, run.Sinner, run.Effects, run.Purse, 1, marks: run.NextTableMarks);
                Assert.AreEqual(run.Rules.AnteAt(1) + 1, table.AnteNow);
                return;
            }
            Assert.Fail("no unknown node ahead");
        }

        [Test]
        public void MammonsAccountant_ThreeCheaperTables_OrCoinsAndARicherWarden()
        {
            var (_, run) = At(1);
            new MammonsAccountant().Choose(run, "fix");
            Assert.AreEqual(3, run.AnteCutTables);
            var (_, other) = At(1);
            int coins = other.Purse.Coins;
            new MammonsAccountant().Choose(other, "report");
            Assert.AreEqual(coins + 40, other.Purse.Coins);
            Assert.AreEqual(150, other.NextWardenPursePercent);
        }

        [Test]
        public void ThreeCups_OneInFive_TheWarlockAlwaysFindsIt()
        {
            int found = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                ChapterRun run = Journey(seed: seed).Run;
                run.Purse.Add(200);
                if (new ThreeCups().Choose(run, "bet1").Result == "found") found++;
            }
            Assert.AreEqual(40, found, 15, "one in five, not one in three");
            for (int seed = 0; seed < 20; seed++)
            {
                ChapterRun run = Journey(SinnerRoster.Find(Warlock.ClassId), seed).Run;
                int coins = run.Purse.Coins;
                Assert.AreEqual("found", new ThreeCups().Choose(run, "bet0").Result);
                Assert.AreEqual(coins + 10, run.Purse.Coins, "the stake doubled");
            }
        }

        [Test]
        public void TheSwindlersTraps_SeenByTheWarlock_TheNaiveReckoningBelievesThem()
        {
            ChapterRun peasant = At(1).run, warlock = At(1, SinnerRoster.Find(Warlock.ClassId)).run;
            Assert.IsFalse(ChoiceEventDeck.SeesTraps(peasant));
            Assert.IsTrue(ChoiceEventDeck.SeesTraps(warlock));
            var pardon = new FakePardon();
            Assert.Greater(pardon.Worth(peasant, "buy", seesTrap: false), 0, "five hundred years for fifty coins: a bargain, he thinks");
            Assert.Less(pardon.Worth(peasant, "buy", seesTrap: true), 0, "the truth: a loss");
            Assert.Less(new SoulInsurance().Worth(peasant, "buy", seesTrap: true), 0);
            Assert.Greater(new ThreeCups().Worth(warlock, "bet2", seesTrap: true), 0, "the Warlock wins the cups");
        }

        [Test]
        public void TheForgedPardon_HalfForged_TheRealOnesStrikeTwoHundred()
        {
            int real = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                ChapterJourney journey = Journey(seed: seed);
                journey.Run.Purse.Add(100);
                int years = journey.Sentence.Years;
                EventOutcome o = new FakePardon().Choose(journey.Run, "buy");
                if (o.Result == "real")
                {
                    real++;
                    Assert.AreEqual(years - 200, journey.Sentence.Years);
                }
                else Assert.AreEqual(years, journey.Sentence.Years);
            }
            Assert.AreEqual(50, real, 15);
        }

        [Test]
        public void TheSoulInsurance_TheInsurerIsGoneAtTheGate()
        {
            var (_, run) = At(1);
            new SoulInsurance().Choose(run, "buy");
            Assert.IsTrue(run.Insured);
            Assert.IsTrue(run.ArriveAtGate().InsurerGone);
            Assert.IsFalse(run.Insured);
        }

        [Test]
        public void ThePickpocket_TakesATenth_AChaseOfThreeHands()
        {
            var (_, run) = At(1, coins: 200);
            var thief = new Pickpocket();
            thief.Appear(run);
            Assert.AreEqual(20, run.EventMemo);
            Assert.AreEqual(180, run.Purse.Coins);
            EventOutcome o = thief.Choose(run, "chase");
            Assert.IsTrue(o.OpensTable);
            FloorTable chase = run.PendingChase;
            Assert.IsNotNull(chase);
            Assert.AreSame(chase, run.PendingSideTable);
            Assert.AreEqual(20 + 10, chase.HouseStart, "his purse: what he took, and his own");
            for (int hand = 0; hand < 3 && !chase.IsOver; hand++)
            {
                chase.Deal();
                HellPokerGame game = chase.Game;
                while (game.Phase != GamePhase.RoundOver)
                {
                    if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                    else game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
                }
                chase.Settle();
            }
            Assert.IsTrue(chase.IsOver, "three hands at most");
            int before = run.Purse.Coins;
            run.FinishTable(chase);
            Assert.IsNull(run.PendingChase);
            Assert.AreEqual(chase.Coins > 0, run.ChaseWon);
            Assert.AreEqual(before + (chase.Coins > 0 ? 30 : 0), run.Purse.Coins);
        }

        [Test]
        public void TheLavaFerry_SkipsAFloor_TheTrailKeepsItsFloors()
        {
            var (journey, run) = At(1);
            new LavaFerryman().Choose(run, "swim");
            Assert.IsTrue(run.SkipNext);
            int floor = run.Current.Floor;
            MapNode target = run.Choices.First();
            Assert.AreEqual(floor + 2, target.Floor, "two floors down");
            run.MoveTo(target);
            Assert.AreEqual(floor + 2, run.Current.Floor);
            Assert.IsFalse(run.SkipNext);
            Assert.AreEqual(run.Trail.Count, run.Current.Floor + 1, "a node for every floor");
            Assert.IsTrue(run.WasSkipped(run.Trail[floor + 1]));

            ChapterSave save = journey.Capture();
            Assert.IsTrue(ChapterSave.TryDecode(save.Encode(), out ChapterSave read));
            Assert.AreEqual(target.ToString(), ChapterJourney.Restore(read).Run.Current.ToString(), "the trail comes back");
        }

        [Test]
        public void HellsLottery_MostlyBlank_SometimesCoins_ARelicOfLegendRarely()
        {
            var results = new Dictionary<string, int>();
            for (int seed = 0; seed < 300; seed++)
            {
                ChapterRun run = Journey(seed: seed).Run;
                run.Purse.Add(100);
                EventOutcome o = new HellLottery().Choose(run, "ticket");
                results[o.Result] = (results.TryGetValue(o.Result, out int n) ? n : 0) + 1;
                if (o.Result == "relic") Assert.AreEqual(RelicRarity.Legendary, ChapterRelics.RarityOf(o.Relic));
                if (o.Result == "coins") Assert.AreEqual(30, o.Coins, "40 won, 10 paid");
            }
            Assert.AreEqual(210, results["blank"], 30);
            Assert.AreEqual(75, results["coins"], 25);
        }

        [Test]
        public void TheConfession_YearsOff_TheDemonSitsDownWithHisMaliceFull()
        {
            var (journey, run) = At(1);
            int years = journey.Sentence.Years;
            new ConfessionCell().Choose(run, "confess");
            Assert.AreEqual(years - 150, journey.Sentence.Years);
            Assert.IsTrue(run.BossMaliceFull);
            run.PayTribute();
            HellPokerGame boss = run.OpenBossTable(GameRules.Default);
            Assert.AreEqual(boss.MaliceMax, boss.Malice, "he heard of it");
            Assert.IsFalse(run.BossMaliceFull);
        }

        [Test]
        public void TheRainOfFire_CoinsOrYears_NoPassing()
        {
            var (journey, run) = At(2);
            var rain = new FireRain();
            Assert.IsTrue(rain.IsEncounter);
            int coins = run.Purse.Coins, years = journey.Sentence.Years;
            rain.Choose(run, "coins");
            Assert.AreEqual(coins - 12, run.Purse.Coins, "10 × 1.25, rounded down");
            rain.Choose(run, "years");
            Assert.AreEqual(years + 62, journey.Sentence.Years, "50 × 1.25");
        }

        [Test]
        public void TheSinSwap_GiveOne_TakeOneOfTwo()
        {
            var (_, run) = At(1);
            run.Effects.AddRelic(RelicIds.RustyCrown);
            var swap = new SinSwap();
            Assert.IsTrue(swap.CanAppear(run));
            EventOutcome given = swap.Choose(run, EventChoices.GivePrefix + RelicIds.RustyCrown);
            Assert.IsTrue(given.Waits);
            CollectionAssert.DoesNotContain(run.Effects.Relics, RelicIds.RustyCrown);
            Assert.AreEqual(2, run.EventOffers.Count);
            Assert.That(swap.Choices(run).All(c => c.StartsWith(EventChoices.TakePrefix, StringComparison.Ordinal)), "now: one of the two");
            string offered = run.EventOffers[1];
            swap.Choose(run, EventChoices.TakePrefix + offered);
            CollectionAssert.Contains(run.Effects.Relics, offered);
            Assert.IsEmpty(run.EventOffers);
        }

        [Test]
        public void TheBlindSeer_TellsTheBarsLength()
        {
            var (_, run) = At(1);
            EventOutcome o = new BlindSeer().Choose(run, "ask");
            Assert.That(new[] { "long", "medium", "short" }, Does.Contain(o.Result));
            Assert.Greater(o.Value, 0);
            run.OweAtGate(2000);
            Assert.AreEqual("long", new BlindSeer().Choose(run, "ask").Result, "a long bar after a bargain");
        }

        [Test]
        public void ADemoTwoOneOffer_ComesOnceARun_AcrossTheChapters()
        {
            ChapterJourney journey = Journey(seed: 11);
            var deck = new IFloorEvent[] { new HellLottery(), new LavaFerryman() };
            ChapterRun run = journey.Run;
            run.MoveTo(run.Map.Floors[0].First());
            MapNode eventNode = new MapNode(1, 0, NodeKind.Event, new[] { 0 });
            ForceCurrent(run, eventNode);
            IFloorEvent first = run.DrawEvent(deck);
            CollectionAssert.Contains(journey.Capture().RunEvents, first.Id);
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
            journey.Run.TakeLootCoins();
            journey.NextChapter();
            ForceCurrent(journey.Run, eventNode);
            IFloorEvent second = journey.Run.DrawEvent(new[] { deck.First(e => e.Id == first.Id) });
            Assert.IsNull(second, "the same offer never comes again in the run");
        }

        [Test]
        public void ThePickpocketStrikes_AsHeIsDrawn()
        {
            ChapterJourney journey = Journey(seed: 5);
            ChapterRun run = journey.Run;
            run.Purse.Add(100 - run.Purse.Coins);
            ForceCurrent(run, new MapNode(1, 0, NodeKind.Event, new[] { 0 }));
            IFloorEvent drawn = run.DrawEvent(new IFloorEvent[] { new Pickpocket() });
            Assert.IsInstanceOf<Pickpocket>(drawn);
            Assert.AreEqual(90, run.Purse.Coins);
            Assert.AreEqual(10, run.EventMemo);
        }

        [Test]
        public void WhatTheOffersLeft_IsSaved()
        {
            var (journey, run) = At(1);
            run.AnteCutTables = 2;
            run.NextWardenPursePercent = 150;
            run.LoanDue = 60;
            run.LoanPays = true;
            run.Insured = true;
            run.BossMaliceFull = true;
            run.EventMemo = 7;
            run.EventOffers.Add(RelicIds.BoneDie);
            run.RunEventsSeen.Add(ChoiceEventIds.HellLottery);
            MapNode unknown = run.Map.Floors.SelectMany(f => f).FirstOrDefault(n => n.Kind == NodeKind.Unknown);
            if (unknown != null) run.Reveal(unknown);
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave read));
            ChapterRun back = ChapterJourney.Restore(read).Run;
            Assert.AreEqual(2, back.AnteCutTables);
            Assert.AreEqual(150, back.NextWardenPursePercent);
            Assert.AreEqual(60, back.LoanDue);
            Assert.IsTrue(back.LoanPays && back.Insured && back.BossMaliceFull);
            Assert.AreEqual(7, back.EventMemo);
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie }, back.EventOffers);
            CollectionAssert.Contains(back.RunEventsSeen.ToList(), ChoiceEventIds.HellLottery);
            if (unknown != null) Assert.AreEqual(unknown.Hidden, back.ShownKind(back.Map.Find(unknown.Floor, unknown.Lane)));
        }

        /// <summary>Stands the run on a node of its own (an event's, for drawing offers).</summary>
        private static void ForceCurrent(ChapterRun run, MapNode node) =>
            typeof(ChapterRun).GetProperty(nameof(ChapterRun.Current)).SetValue(run, node);
    }
}
