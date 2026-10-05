using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The sinner classes: passive traits (the Warlock's sight, the King's crown), and the power each one buys with the run's
    /// charge gauge — a win +1, a loss +2, a fold +1, a tie nothing, up to 5; full, it waits for the player (nothing is
    /// spent on its own) and empties when used: the Peasant's free fold, the Warlock's ward (minor cheats only), the King's
    /// protection (before the draw). The gauge follows the run to every table; the save (v=4 "class.charge"); records per class.
    /// (The designer's rule: a win +1, a loss +2, a fold +1, a tie nothing; a hand the Peasant walks away from with his power, nothing.)
    /// </summary>
    public class SinnerTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string KingsPair = "KS KH 2C 5D 9C";
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseFullHouse = "QS QH QD 4C 4H";
        private const string HouseTwos = "2D 2H 5S 7H 9D";
        private const string Blanks = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S 10D 3D";

        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            public OnlyCheat(ICheat cheat) => _cheat = cheat;
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, null);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        /// <summary>A game where the demon plays <paramref name="cheat"/> every hand, for this sinner.</summary>
        private static HellPokerGame Game(string player, string house, ICheat cheat, Sinner sinner, GameRules rules = null,
            IPayoutTable payouts = null)
        {
            var random = new FirstChoice();
            rules = rules ?? new GameRules(1000, 5000, luciferGateYears: 0);
            return new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards),
                payouts ?? PayoutTable.CreateDefault(), null,
                new CheatSession(cheat == null ? null : new OnlyCheat(cheat), 1, random, sinner), random, sinner);
        }

        private static Sinner Charged(SinnerClass sinnerClass) => new Sinner(sinnerClass, charge: ChargeRules.Default.Full);

        private static void PassToTheEnd(HellPokerGame game)
        {
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        private static void PlayOut(HellPokerGame game)
        {
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
            if (game.Phase == GamePhase.Drawing) game.Draw(new int[0]);
            PassToTheEnd(game);
        }

        private static CheatResult Only(HellPokerGame game) => game.CheatsThisHand.Single();

        // ------------------------------------------------------------------ the roster

        [Test]
        public void TheRoster_HasThreeClasses_ThePeasantFirst_EachWithAPower()
        {
            CollectionAssert.AreEqual(new[] { "peasant", "warlock", "king" }, SinnerRoster.All.Select(c => c.Id));
            Assert.AreEqual(1000, SinnerRoster.Peasant.StartingYears);
            Assert.AreEqual(1000, SinnerRoster.Warlock.StartingYears);
            Assert.AreEqual(1250, SinnerRoster.King.StartingYears);
            Assert.Less(SinnerRoster.King.StartingYears, DealerRoster.Lilith.SoulThreshold, "The King starts below every soul line.");
            Assert.AreSame(SinnerRoster.King, SinnerRoster.Find("king"));
            Assert.IsNull(SinnerRoster.Find("jester"));
            CollectionAssert.AreEqual(new[] { SinnerAbility.FreeFold, SinnerAbility.Ward, SinnerAbility.Protect }, SinnerRoster.All.Select(c => c.Ability));
        }

        // ------------------------------------------------------------------ the charge

        [Test]
        public void TheCharge_WinOne_LossTwo_FoldOne_TieNothing_UpToFive()
        {
            var sinner = new Sinner(new Peasant());
            Assert.AreEqual(0, sinner.Charge, "A run starts empty.");

            sinner.HandSettled(false, ShowdownOutcome.PlayerWins);
            Assert.AreEqual(1, sinner.Charge);
            sinner.HandSettled(false, ShowdownOutcome.HouseWins);
            Assert.AreEqual(3, sinner.Charge);
            sinner.HandSettled(true, null);
            Assert.AreEqual(4, sinner.Charge, "A fold is a loss, but pays one: folding cannot be farmed.");
            sinner.HandSettled(false, ShowdownOutcome.Push);
            Assert.AreEqual(4, sinner.Charge);
            Assert.IsFalse(sinner.IsCharged);
            sinner.HandSettled(false, ShowdownOutcome.HouseWins);
            Assert.AreEqual(5, sinner.Charge, "Never past five.");
            Assert.IsTrue(sinner.IsCharged);
        }

        [Test]
        public void TheChargeRules_CanBeTuned()
        {
            var sinner = new Sinner(new Peasant(), rules: new ChargeRules(full: 6, perWin: 1, perLoss: 1, perFold: 0));
            sinner.HandSettled(false, ShowdownOutcome.HouseWins);
            sinner.HandSettled(true, null);
            Assert.AreEqual(1, sinner.Charge);
            Assert.AreEqual(5, ChargeRules.Default.Full);
            Assert.AreEqual(1, ChargeRules.Default.PerWin);
            Assert.AreEqual(2, ChargeRules.Default.PerLoss);
            Assert.AreEqual(1, ChargeRules.Default.PerFold);
            Assert.AreEqual(0, ChargeRules.Default.PerTie);
        }

        [Test]
        public void EverySettledHand_Charges_AtTheTable()
        {
            int After(string player, string house, bool fold)
            {
                var sinner = new Sinner(new Peasant());
                HellPokerGame game = Game(player, house, null, sinner);
                game.PlaceBet();
                if (fold) game.Bet(BetAction.Fold);
                else PlayOut(game);
                return sinner.Charge;
            }

            Assert.AreEqual(1, After(Flush, HouseTwos, fold: false), "A win.");
            Assert.AreEqual(2, After(Nothing, HouseFullHouse, fold: false), "A loss.");
            Assert.AreEqual(1, After(Nothing, HouseFullHouse, fold: true), "A fold.");
        }

        [Test]
        public void AHandLeftBehind_ChargesAsTheFoldItWas_OrTheLossOfASealedHand()
        {
            var folded = new Sinner(new Peasant());
            Game(Nothing, HouseFullHouse, null, folded).ForfeitHand(new HandInProgress(100, 100, false, false, false));
            Assert.AreEqual(1, folded.Charge, "A fold.");

            var sealedHand = new Sinner(new Peasant());
            Game(Nothing, HouseFullHouse, null, sealedHand).ForfeitHand(new HandInProgress(300, 100, true, false, true));
            Assert.AreEqual(2, sealedHand.Charge, "The loss of a sealed hand.");
        }

        [Test]
        public void ThePower_WaitsForAFullGauge_AndEmptiesIt()
        {
            var sinner = new Sinner(new Peasant(), charge: 4);
            HellPokerGame game = Game(Nothing, HouseFullHouse, null, sinner);
            game.PlaceBet();

            Assert.AreEqual(PowerRefusal.NotCharged, game.WhyNoPower());
            Assert.IsFalse(game.UsePower());
            Assert.AreEqual(4, sinner.Charge);
        }

        [Test]
        public void NothingIsSpentOnItsOwn_AFullGaugeWaits()
        {
            // The Peasant folds the usual way: he pays, and his power is still there.
            var peasant = Charged(new Peasant());
            HellPokerGame fold = Game(Nothing, HouseFullHouse, null, peasant);
            fold.PlaceBet();
            fold.Bet(BetAction.Fold);
            Assert.AreEqual(1050, fold.Years);
            Assert.IsFalse(fold.LastRound.FreeFold);
            Assert.IsTrue(peasant.IsCharged);

            // A minor cheat strikes the Warlock who raised no ward.
            var warlock = Charged(new Warlock());
            HellPokerGame cheat = Game(Nothing, HouseFullHouse, new CollateralCheat(), warlock);
            cheat.PlaceBet();
            cheat.CheckToDraw();
            Assert.AreEqual(CheatOutcome.Played, Only(cheat).Outcome);
            Assert.IsTrue(warlock.IsCharged);
        }

        // ------------------------------------------------------------------ the Peasant

        [Test]
        public void ThePeasant_WalksAwayForNothing()
        {
            var sinner = Charged(new Peasant());
            HellPokerGame game = Game(Nothing, HouseFullHouse, null, sinner);
            game.PlaceBet();
            Assert.AreEqual(PowerRefusal.None, game.WhyNoPower());

            Assert.IsTrue(game.UsePower());

            Assert.AreEqual(1000, game.Years, "The honest heart: nothing added.");
            Assert.IsTrue(game.LastRound.Folded);
            Assert.IsTrue(game.LastRound.FreeFold);
            Assert.AreEqual(0, sinner.Charge, "Emptied — the hand walked away from with the power charges nothing.");
            Assert.AreEqual(1, sinner.PowersUsed);
        }

        [Test]
        public void ThePeasant_CannotWalkAwayFromASealedHand_HisPowerWaits()
        {
            var sinner = Charged(new Peasant());
            HellPokerGame game = Game(Nothing, HouseFullHouse, null, sinner);
            game.TakeOver(10, 3);   // the ante is all he has: sealed at the deal
            game.PlaceBet();
            Assert.IsTrue(game.IsCommitted);

            Assert.IsFalse(game.UsePower(), "No fold in a sealed hand.");
            Assert.IsTrue(sinner.IsCharged);
            Assert.IsTrue(game.ArmPower(), "But it can wait, switched on, for a later fold.");
            Assert.AreEqual(5, sinner.Charge);
        }

        [Test]
        public void BetweenHands_ThePeasantMaySwitchOn_TheKingHasNothingToPick()
        {
            HellPokerGame peasant = Game(Nothing, HouseFullHouse, null, Charged(new Peasant()));
            Assert.AreEqual(PowerRefusal.None, peasant.WhyNoPower(), "His power waits for his next fold.");
            Assert.IsTrue(peasant.ArmPower());
            Assert.IsTrue(peasant.PowerArmed);

            HellPokerGame king = Game(KingsPair, HouseTwos, null, Charged(new King()));
            Assert.AreEqual(PowerRefusal.NoHand, king.WhyNoPower());
            Assert.IsFalse(king.ArmPower());
        }

        // ------------------------------------------------------------------ the Warlock

        [Test]
        public void TheWarlock_RaisesAWard_TheAnnouncedMinorCheatIsRefused_AndTheGaugeEmpties()
        {
            var sinner = Charged(new Warlock());
            HellPokerGame game = Game(Nothing, HouseFullHouse, new CollateralCheat(), sinner);
            game.PlaceBet();
            Assert.AreEqual(1, game.Malice, "Full: the cheat is announced.");
            Assert.AreEqual(PowerRefusal.None, game.WhyNoPower());

            Assert.IsTrue(game.UsePower());
            Assert.IsTrue(sinner.WardRaised);
            Assert.AreEqual(0, sinner.Charge);
            game.CheckToDraw();

            Assert.AreEqual(CheatOutcome.Blocked, Only(game).Outcome);
            Assert.AreEqual(0, game.Malice, "A refused cheat is spent: the demon's gauge empties.");
            Assert.IsFalse(Enumerable.Range(0, 5).Any(game.IsPlayerCardChained), "Nothing happened to the cards.");
            Assert.IsFalse(sinner.WardRaised);
            Assert.AreEqual(1, sinner.WardsUsed);
        }

        [Test]
        public void TheWarlock_HasNothingToWard_WithoutAnAnnouncedCheat()
        {
            var sinner = Charged(new Warlock());
            HellPokerGame game = Game(Nothing, HouseFullHouse, null, sinner);
            game.PlaceBet();

            Assert.AreEqual(PowerRefusal.NoCheatAnnounced, game.WhyNoPower());
            Assert.IsFalse(game.UsePower());
            Assert.IsTrue(sinner.IsCharged);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TheWarlock_CannotWardAMajorCheat(bool theFall)
        {
            var sinner = Charged(new Warlock());
            HellPokerGame game;
            if (theFall)
            {
                Dealer lucifer = DealerRoster.Lucifer;
                var random = new FirstChoice();
                game = new HellPokerGame(lucifer.ApplyTo(GameRules.Default), TestDecks.Stacked($"{Flush} {HouseTwos} {Blanks}"),
                    HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy(3)), new HouseDrawStrategy(3), lucifer.Payouts, null,
                    new CheatSession(new OnlyCheat(new TheFallCheat()), 1, random, sinner), random, sinner);
                game.TakeOver(150, 5);
            }
            else
            {
                game = Game(KingsPair, HouseTwos, new BuyoutCheat(), sinner);
            }
            game.PlaceBet();

            Assert.AreEqual(PowerRefusal.MajorCheat, game.WhyNoPower());
            Assert.IsFalse(game.UsePower());
            Assert.IsTrue(sinner.IsCharged, "Not spent on what it cannot stop.");
        }

        [Test]
        public void TheWard_WaitsThroughACheatThatComesToNothing()
        {
            var sinner = new Sinner(new Warlock(), wardRaised: true);
            HellPokerGame game = Game(Nothing, HouseFullHouse, new TitheCheat(), sinner);   // a lost hand: nothing to tithe
            game.PlaceBet();
            PlayOut(game);

            Assert.AreEqual(CheatOutcome.Fizzled, Only(game).Outcome);
            Assert.IsTrue(sinner.WardRaised, "Still up for the next minor cheat.");
        }

        [Test]
        public void OneWardAtATime()
        {
            var sinner = new Sinner(new Warlock(), charge: 5, wardRaised: true);
            HellPokerGame game = Game(Nothing, HouseFullHouse, new CollateralCheat(), sinner);
            game.PlaceBet();

            Assert.AreEqual(PowerRefusal.WardAlreadyRaised, game.WhyNoPower());
        }

        [Test]
        public void TheWarlock_SeesTwoHouseCards_WhereTheHouseHidesItsHand_OnlyThere()
        {
            GameRules At(Dealer dealer) => dealer.ApplyTo(GameRules.Default);
            var warlock = new Warlock();

            Assert.AreEqual(1, At(DealerRoster.Belial).HouseCardsShown, "Belial hides his hand...");
            Assert.AreEqual(2, warlock.HouseCardsShownAt(At(DealerRoster.Belial)), "...the Warlock sees two.");
            Assert.AreEqual(2, warlock.HouseCardsShownAt(At(DealerRoster.Mammon)));
            Assert.AreEqual(2, warlock.HouseCardsShownAt(At(DealerRoster.Lilith)));
            Assert.AreEqual(0, warlock.HouseCardsShownAt(At(DealerRoster.Lucifer)), "The Morning Star keeps his darkness.");
            Assert.AreEqual(1, SinnerRoster.Peasant.HouseCardsShownAt(At(DealerRoster.Belial)));
        }

        [Test]
        public void AtBelialsTable_TheWarlocksHand_TurnsTwoHouseCards()
        {
            var random = new FirstChoice();
            Dealer belial = DealerRoster.Belial;
            GameRules rules = belial.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0));
            var sinner = new Sinner(new Warlock());
            var game = new HellPokerGame(rules, TestDecks.Stacked($"{Nothing} {HouseTwos} {Blanks}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), belial.Payouts, null,
                new CheatSession(null, 0, random), random, sinner);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            game.Bet(BetAction.Pass);

            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
            Assert.AreEqual(2, game.HouseCardsRevealed);
        }

        // ------------------------------------------------------------------ the King

        [Test]
        public void TheKingsCrown_ForgivesAnAnteMore()
        {
            int Win(Sinner sinner)
            {
                HellPokerGame game = Game(Flush, HouseTwos, null, sinner);   // a flush against twos
                game.PlaceBet();
                PlayOut(game);
                Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
                return -game.LastRound.YearsChange;
            }

            Assert.AreEqual(Win(new Sinner(new Peasant())) + 25, Win(new Sinner(new King())), "Ante 100: the crown adds a quarter.");
            Assert.AreEqual(Win(new Sinner(new Peasant())) + 100, Win(new Sinner(new King(crownPercent: 100))), "The ante multiplier +1.");
        }

        private static HellPokerGame KingWith(ICheat cheat, string player, out Sinner sinner, string house = HouseTwos)
        {
            sinner = Charged(new King());
            HellPokerGame game = Game(player, house, cheat, sinner);
            game.PlaceBet();
            return game;
        }

        [Test]
        public void TheKingsProtectedCard_IsBeyondTheBurningCard()
        {
            HellPokerGame game = KingWith(new BurningCardCheat(), KingsPair, out Sinner sinner);
            Assert.IsTrue(game.CanProtect(0));
            Assert.IsTrue(game.Protect(0));
            Assert.IsTrue(game.IsPlayerCardProtected(0));
            Assert.AreEqual(0, sinner.Charge);

            game.CheckToDraw();   // the fire strikes as the draw opens

            CheatResult burn = Only(game);
            Assert.AreEqual(CheatOutcome.Played, burn.Outcome);
            CollectionAssert.DoesNotContain(burn.PlayerCards, 0);
            Assert.AreEqual(new Card(Rank.King, Suit.Spades), game.PlayerHand[0]);
        }

        [Test]
        public void TheKingsProtectedCard_IsBeyondTheRewrite()
        {
            HellPokerGame game = KingWith(new RewriteCheat(), KingsPair, out _);
            game.Protect(0);
            game.CheckToDraw();
            game.Draw(new int[0]);

            CollectionAssert.DoesNotContain(Only(game).PlayerCards, 0);
            Assert.AreEqual(new Card(Rank.King, Suit.Spades), game.PlayerHand[0]);
        }

        [Test]
        public void TheKingsProtectedCard_IsBeyondTheSerpent()
        {
            HellPokerGame game = KingWith(new SerpentSwapCheat(), KingsPair, out _);
            game.Protect(0);
            game.CheckToDraw();
            game.Draw(new int[0]);

            CollectionAssert.DoesNotContain(Only(game).PlayerCards, 0);
            Assert.AreEqual(new Card(Rank.King, Suit.Spades), game.PlayerHand[0]);
        }

        [Test]
        public void TheKingsProtectedCards_AreBeyondTheFall_ItFallsElsewhere()
        {
            var sinner = Charged(new King());
            Dealer lucifer = DealerRoster.Lucifer;
            var random = new FirstChoice();
            var game = new HellPokerGame(lucifer.ApplyTo(GameRules.Default), TestDecks.Stacked($"{Flush} {HouseTwos} {Blanks}"),
                HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy(3)), new HouseDrawStrategy(3), lucifer.Payouts, null,
                new CheatSession(new OnlyCheat(new TheFallCheat()), 1, random, sinner), random, sinner);
            game.TakeOver(150, 5);
            game.PlaceBet();
            int king = 4;   // K♣, the highest card
            game.CheckToDraw();
            Assert.IsTrue(game.Protect(king), "At the draw too.");

            game.Draw(new int[0]);
            PassToTheEnd(game);

            CheatResult fall = Only(game);
            Assert.AreEqual(CheatOutcome.Played, fall.Outcome);
            CollectionAssert.DoesNotContain(fall.PlayerCards, king);
            Assert.AreEqual(new Card(Rank.King, Suit.Clubs), game.PlayerHand[king]);
        }

        [Test]
        public void TheKingProtects_OnlyBeforeTheDraw_ACardHeSees_WithAFullGauge()
        {
            var empty = new Sinner(new King(), charge: 4);
            HellPokerGame notYet = Game(KingsPair, HouseTwos, null, empty);
            notYet.PlaceBet();
            Assert.IsFalse(notYet.CanProtect(0), "The gauge is not full.");
            Assert.AreEqual(PowerRefusal.NotCharged, notYet.WhyNoPower());

            HellPokerGame game = KingWith(null, KingsPair, out Sinner sinner);
            Assert.IsFalse(game.CanProtect(4), "Not yet turned.");
            Assert.AreEqual(PowerRefusal.None, game.WhyNoPower());
            Assert.IsFalse(game.UsePower(), "The King's power picks a card: Protect.");
            Assert.IsTrue(game.Protect(2));
            Assert.IsFalse(game.CanProtect(1), "The gauge is empty again.");

            var late = Charged(new King());
            HellPokerGame afterDraw = Game(KingsPair, HouseTwos, null, late);
            afterDraw.PlaceBet();
            afterDraw.CheckToDraw();
            afterDraw.Draw(new int[0]);
            Assert.AreEqual(PowerRefusal.NotBeforeDraw, afterDraw.WhyNoPower());
            Assert.IsFalse(afterDraw.CanProtect(0), "After the draw: too late.");
            Assert.IsTrue(late.IsCharged);
        }

        [Test]
        public void NoOneElse_CanProtect()
        {
            HellPokerGame game = Game(KingsPair, HouseTwos, null, Charged(new Warlock()));
            game.PlaceBet();

            Assert.IsFalse(game.CanProtect(0));
            Assert.IsFalse(game.Protect(0));
        }

        // ------------------------------------------------------------------ the save and the records

        [Test]
        public void TheSave_v4_KeepsTheClass_TheCharge_AndARaisedWard()
        {
            var snapshot = new RunSnapshot("belial", 900, 7, new RunStats(1000, "belial"), classId: "warlock", classCharge: 3, wardRaised: true);

            string text = snapshot.Encode();
            StringAssert.StartsWith("v=4", text);
            StringAssert.Contains("class.charge=3", text);
            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot back));

            Assert.AreEqual("warlock", back.ClassId);
            Assert.AreEqual(3, back.ClassCharge);
            Assert.IsTrue(back.WardRaised);
        }

        [Test]
        public void AnOlderSave_StartsTheGaugeEmpty_AndItsPerTableChargesAreIgnored()
        {
            string v4 = "v=4\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\n" +
                        "lucifer=0\norigin=\nattempts=0\nmalice=1\ncheat.major=0\ngrudge=0\nclass=king\nclass.charges=1\nclass.charges.tables=belial:0";
            Assert.IsTrue(RunSnapshot.TryDecode(v4, out RunSnapshot older));
            Assert.AreEqual("king", older.ClassId);
            Assert.AreEqual(0, older.ClassCharge);
            Assert.IsFalse(older.WardRaised);

            string v3 = "v=3\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\n" +
                        "lucifer=0\norigin=\nattempts=0\nmalice=1\ncheat.major=0";
            Assert.IsTrue(RunSnapshot.TryDecode(v3, out RunSnapshot snapshot));
            Assert.AreEqual("peasant", snapshot.ClassId);
            Assert.AreEqual(0, new Sinner(SinnerRoster.Find(snapshot.ClassId), snapshot.ClassCharge).Charge);
        }

        [Test]
        public void ABrokenCharge_IsClamped_OrTheSaveIsNotRead()
        {
            Assert.AreEqual(5, new Sinner(new King(), charge: 9).Charge);
            string text = new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"), classId: "king").Encode() + "\nclass.charge=-2";
            Assert.IsFalse(RunSnapshot.TryDecode(text, out _));
        }

        [Test]
        public void TheRecords_CountAbsolutionsPerClass_AndAnOldBookHasNone()
        {
            var book = new RecordBook();
            book.RunEnded(true, "mammon", 30, classId: "king");
            book.RunEnded(true, "belial", 20, classId: "king");
            book.RunEnded(false, "lilith", 10, classId: "warlock");

            RecordBook back = RecordBook.Decode(book.Encode());

            Assert.AreEqual(2, back.AbsolutionsAs("king"));
            Assert.AreEqual(0, back.AbsolutionsAs("warlock"));
            Assert.AreEqual(1, back.AbsolutionsAt("mammon"), "The demons' own counts are untouched.");
            Assert.IsFalse(back.AbsolutionsByDealer.ContainsKey("class.king"), "A class never reads as a demon.");
            Assert.AreEqual(0, RecordBook.Decode("v=1\nruns=1\nabsolved=0\ndamned=0\nfastest=").AbsolutionsAs("king"));
        }

        // ------------------------------------------------------------------ at the table

        private FakeTableView _view;
        private TablePresenter _presenter;
        private HellPokerGame _game;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private TablePresenter Table(ICheat cheat, string player = Nothing, string house = HouseFullHouse, RunArchive archive = null,
            ICheat shown = null, Dealer finalDealer = null)
        {
            _view = new FakeTableView();
            return _presenter = new TablePresenter((d, sinner) =>
            {
                var random = new FirstChoice();
                ICheatPolicy policy = cheat == null ? null : shown == null ? (ICheatPolicy)new OnlyCheat(cheat) : new Lying(cheat, shown);
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: finalDealer == null ? 0 : 250)),
                    TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(policy, 1, random, sinner), random, sinner);
            }, _view, null, archive, finalDealer);
        }

        /// <summary>A run of this class picked up from a save with a full gauge.</summary>
        private void ChargedRun(SinnerClass sinnerClass, Dealer dealer, ICheat cheat = null, string player = Nothing, string house = HouseFullHouse,
            Dealer finalDealer = null, int years = 1000)
        {
            Table(cheat, player, house, finalDealer: finalDealer).Resume(dealer,
                new RunSnapshot(dealer.Id, years, 3, new RunStats(years, dealer.Id), classId: sinnerClass.Id, classCharge: 5));
        }

        private sealed class Lying : ICheatPolicy
        {
            private readonly ICheat _cheat;
            private readonly ICheat _shown;
            public Lying(ICheat cheat, ICheat shown) { _cheat = cheat; _shown = shown; }
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, _shown);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        [Test]
        public void ANewRun_AsTheKing_StartsAt1250_WithAnEmptyGauge_AndTheDemonGreetsTheCrown()
        {
            Table(null).StartNewRun(DealerRoster.Mammon, SinnerRoster.King);

            Assert.AreEqual(1250, _game.Years);
            Assert.AreEqual(1250, _presenter.Stats.HighestYears);
            Assert.AreEqual("A king! How fortunate. A crown counts as collateral.", _view.DealerView.LastLine);
            Assert.AreEqual("king", _view.Sinner.ClassId);
            Assert.AreEqual(0, _view.Sinner.Charge);
            Assert.AreEqual(5, _view.Sinner.Full);
            Assert.IsFalse(_view.Sinner.Usable);
        }

        [Test]
        public void K_BeforeTheGaugeIsFull_TellsHowItCharges()
        {
            Table(null).StartNewRun(DealerRoster.Mammon, SinnerRoster.Peasant);
            _view.PressAction();

            _presenter.UsePower();

            Assert.AreEqual("Your power charges: 0 / 5. Win +1, lose +2, fold +1.", _view.Message);
            Assert.AreEqual(GamePhase.PlayerReveal, _game.Phase, "Nothing happened.");
        }

        [Test]
        public void TheReasons_AreToldInBothLanguages()
        {
            Assert.AreEqual("Gücün doluyor: 2 / 5. Kazanç +1, kayıp +2, çekilme +1.",
                WithTurkish(() => UiText.PowerRefused(SinnerAbility.FreeFold, PowerRefusal.NotCharged, 2, 5)));
            Assert.AreEqual("Mühür vuruldu: bu el bırakılamaz.", WithTurkish(() => UiText.PowerRefused(SinnerAbility.FreeFold, PowerRefusal.CannotFold, 5, 5)));
            Assert.AreEqual("Büyük bir hileye koruma işlemez.", WithTurkish(() => UiText.PowerRefused(SinnerAbility.Ward, PowerRefusal.MajorCheat, 5, 5)));
            Assert.AreEqual("Taç bir kartı ancak değişten önce korur.", WithTurkish(() => UiText.PowerRefused(SinnerAbility.Protect, PowerRefusal.NotBeforeDraw, 5, 5)));
            Assert.AreEqual("A major cheat is beyond a ward.", UiText.PowerRefused(SinnerAbility.Ward, PowerRefusal.MajorCheat, 5, 5));
        }

        private static string WithTurkish(System.Func<string> words)
        {
            Lang.Set(Language.Turkish);
            try { return words(); }
            finally { Lang.Set(Language.English); }
        }

        [Test]
        public void ThePeasant_PressesK_AndWalksAwayForNothing()
        {
            ChargedRun(SinnerRoster.Peasant, DealerRoster.Mammon);
            Assert.IsTrue(_view.Sinner.IsCharged, "A full gauge glows.");
            Assert.IsTrue(_view.Sinner.Usable, "READY: K — his power can wait for a fold even between hands.");
            _view.PressAction();
            Assert.IsTrue(_view.Sinner.Usable);

            _view.PressSinner();   // switched on: the next fold is free
            Assert.AreEqual(5, _view.Sinner.Charge, "Not spent yet.");
            _view.PressBet(BetAction.Fold);

            Assert.AreEqual(1000, _game.Years);
            StringAssert.Contains("honest heart", _view.Message);
            Assert.AreEqual(0, _view.Sinner.Charge, "Spent at the fold.");
        }

        [Test]
        public void TheWarlock_PressesK_AtTheAnnouncement_AndTheWardFlares()
        {
            ChargedRun(SinnerRoster.Warlock, DealerRoster.Mammon, new CollateralCheat());
            _view.PressAction();   // the deal: the cheat is announced

            _presenter.UsePower();
            StringAssert.Contains("ward is up", _view.Message);
            Assert.IsTrue(_view.Sinner.WardRaised);
            Assert.AreEqual(0, _view.Sinner.Charge);
            _presenter.CheckToDraw();

            Assert.That(_view.Moments.Select(m => m.moment), Has.Member(TableMoment.Ward));
            Assert.AreEqual("WARD", _view.Moments.Last(m => m.moment == TableMoment.Ward).text);
            Assert.AreEqual(DealerMood.Annoyed, _view.DealerView.LastMood);
            CollectionAssert.Contains(new[] { "A seal on the account? Who taught you that trick?", "Warded. I will note the expense." },
                _view.DealerView.LastLine);
            Assert.IsFalse(_view.Sinner.WardRaised);
        }

        [Test]
        public void TheWarlock_SeesTheLie_TheMomentItIsTold()
        {
            Table(new CollateralCheat(), shown: new ThornCheat()).StartNewRun(DealerRoster.Belial, SinnerRoster.Warlock);

            _view.PressAction();   // the deal: the intent is announced

            Assert.AreEqual(1, _view.Lies.Count, "The lie breaks at once, before anything strikes.");
            Assert.AreEqual(CheatIds.Collateral, _view.Lies[0].Id);
            Assert.AreEqual(CheatIds.Collateral, _view.Intent.Id);
        }

        [Test]
        public void TheKing_PressesK_ThenACard_AndTheCrownProtectsIt()
        {
            ChargedRun(SinnerRoster.King, DealerRoster.Mammon, new BurningCardCheat(), KingsPair, HouseTwos, years: 1250);
            _view.PressAction();

            _presenter.UsePower();
            StringAssert.Contains("crown protects", _view.Message);
            _presenter.ToggleDiscard(0);

            Assert.IsTrue(_game.IsPlayerCardProtected(0));
            Assert.AreEqual(CardMark.Protected, _view.PlayerView.Slots[0].Mark);
            Assert.AreEqual(0, _view.Sinner.Charge);
            _presenter.CheckToDraw();
            Assert.AreEqual(new Card(Rank.King, Suit.Spades), _game.PlayerHand[0]);
        }

        [Test]
        public void TheKing_AfterTheDraw_HearsWhyNot()
        {
            ChargedRun(SinnerRoster.King, DealerRoster.Mammon, null, KingsPair, HouseTwos, years: 1250);
            _view.PressAction();
            _presenter.CheckToDraw();
            _view.PressAction();   // stand pat

            _presenter.UsePower();

            Assert.AreEqual("The crown protects a card only before the draw.", _view.Message);
            Assert.AreEqual(5, _view.Sinner.Charge);
        }

        [Test]
        public void TheGauge_FollowsTheRun_ToEveryTable_ToLucifer_AndBackDown()
        {
            ChargedRun(SinnerRoster.Warlock, DealerRoster.Mammon, finalDealer: DealerRoster.Lucifer);
            Sinner run = _presenter.Sinner;

            _presenter.SwitchTable(DealerRoster.Belial);
            Assert.AreSame(run, _game.Sinner);
            Assert.AreEqual(5, _game.Sinner.Charge, "A new table keeps the gauge.");

            _game.TakeOver(200, 3);
            _presenter.SwitchTable(DealerRoster.Mammon);   // below the gate: summoned
            Assert.IsTrue(_game.Rules.IsFinalTable);
            Assert.AreEqual(5, _game.Sinner.Charge, "So does Lucifer's.");

            _view.PressAction();
            _presenter.CheckToDraw();
            _view.PressAction();
            while (_game.Phase != GamePhase.RoundOver && !_game.IsGameOver) _view.PressBet(BetAction.Pass);   // a full house: 700, cast down
            _view.PressAction();
            Assert.IsFalse(_game.Rules.IsFinalTable, "Cast down.");
            Assert.AreEqual(5, _game.Sinner.Charge, "And the fall: the same gauge (full stays full).");
        }

        [Test]
        public void TheGauge_IsSaved_AndComesBack()
        {
            var archive = new RunArchive(new MemoryStore());
            Table(null, archive: archive).StartNewRun(DealerRoster.Mammon, SinnerRoster.Warlock);
            _view.PressAction();
            _presenter.CheckToDraw();
            _view.PressAction();
            while (_game.Phase != GamePhase.RoundOver) _view.PressBet(BetAction.Pass);   // lost: +2
            _view.PressAction();

            RunSnapshot saved = archive.LoadRun();
            Assert.AreEqual(2, saved.ClassCharge);
            _presenter.Dispose();
            Table(null).Resume(DealerRoster.Belial, saved);
            Assert.AreEqual("warlock", _game.Sinner.Id);
            Assert.AreEqual(2, _game.Sinner.Charge);
            Assert.AreEqual(2, _view.Sinner.Charge);
        }
    }
}
