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
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The sinner classes: the Peasant's free fold, the Warlock's ward (minor cheats only, the gauge empties, one per table)
    /// and sight of lies, the King's crown (a won hand forgives an ante more) and protection (no cheat touches the card),
    /// charges per run / per table, the save (v=4; an older save is a Peasant's) and the records per class.
    /// </summary>
    public class SinnerTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string KingsPair = "KS KH 2C 5D 9C";
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

        private static void PassToTheEnd(HellPokerGame game)
        {
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        private static CheatResult Only(HellPokerGame game) => game.CheatsThisHand.Single();

        // ------------------------------------------------------------------ the roster

        [Test]
        public void TheRoster_HasThreeClasses_ThePeasantFirst()
        {
            CollectionAssert.AreEqual(new[] { "peasant", "warlock", "king" }, SinnerRoster.All.Select(c => c.Id));
            Assert.AreEqual(1000, SinnerRoster.Peasant.StartingYears);
            Assert.AreEqual(1000, SinnerRoster.Warlock.StartingYears);
            Assert.AreEqual(1250, SinnerRoster.King.StartingYears);
            Assert.Less(SinnerRoster.King.StartingYears, DealerRoster.Lilith.SoulThreshold, "The King starts below every soul line.");
            Assert.AreSame(SinnerRoster.King, SinnerRoster.Find("king"));
            Assert.IsNull(SinnerRoster.Find("jester"));
        }

        // ------------------------------------------------------------------ the Peasant

        [Test]
        public void ThePeasant_FoldsOnceForFree_ThenPays()
        {
            var sinner = new Sinner(new Peasant());
            HellPokerGame game = Game(Nothing, HouseFullHouse, null, sinner);
            game.PlaceBet();

            game.Bet(BetAction.Fold);

            Assert.AreEqual(1000, game.Years, "The honest heart: nothing added.");
            Assert.IsTrue(game.LastRound.FreeFold);
            Assert.AreEqual(0, sinner.Charges);

            game.NextRound();
            game.PlaceBet();
            game.Bet(BetAction.Fold);
            Assert.AreEqual(1050, game.Years, "Once a run.");
            Assert.IsFalse(game.LastRound.FreeFold);
        }

        [Test]
        public void ThePeasantsFreeFold_IsPerRun_NotRefilledAtANewTable()
        {
            var sinner = new Sinner(new Peasant());
            Assert.IsTrue(sinner.TrySpend(SinnerAbility.FreeFold));

            sinner.SitDown();

            Assert.AreEqual(0, sinner.Charges);
        }

        // ------------------------------------------------------------------ the Warlock

        [Test]
        public void TheWarlock_WardsOffAMinorCheat_AndTheGaugeEmpties()
        {
            var sinner = new Sinner(new Warlock());
            HellPokerGame game = Game(Nothing, HouseFullHouse, new CollateralCheat(), sinner);
            game.PlaceBet();
            Assert.AreEqual(1, game.Malice, "Full: the cheat is coming.");

            game.CheckToDraw();

            CheatResult result = Only(game);
            Assert.AreEqual(CheatOutcome.Blocked, result.Outcome);
            Assert.AreEqual(0, game.Malice, "A refused cheat is spent: the gauge empties.");
            Assert.IsFalse(Enumerable.Range(0, 5).Any(game.IsPlayerCardChained), "Nothing happened to the cards.");
            Assert.AreEqual(0, sinner.Charges);
            Assert.AreEqual(1, sinner.WardsUsed);
        }

        [Test]
        public void TheWarlocksWard_IsOncePerTable_AndRefillsAtTheNext()
        {
            var sinner = new Sinner(new Warlock());
            HellPokerGame game = Game(Nothing, HouseFullHouse, new CollateralCheat(), sinner);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            PassToTheEnd(game);
            game.NextRound();

            game.PlaceBet();
            game.CheckToDraw();
            Assert.AreEqual(CheatOutcome.Played, Only(game).Outcome, "No ward left at this table.");

            sinner.SitDown();
            Assert.AreEqual(1, sinner.Charges, "A new table: the ward is back.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TheWarlock_CannotWardOffAMajorCheat(bool theFall)
        {
            var sinner = new Sinner(new Warlock());
            if (theFall)
            {
                Dealer lucifer = DealerRoster.Lucifer;
                var random = new FirstChoice();
                var game = new HellPokerGame(lucifer.ApplyTo(GameRules.Default), TestDecks.Stacked($"2C 9C JC 4C KC {HouseTwos} {Blanks}"),
                    HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy(3)), new HouseDrawStrategy(3), lucifer.Payouts, null,
                    new CheatSession(new OnlyCheat(new TheFallCheat()), 1, random, sinner), random, sinner);
                game.TakeOver(150, 5);
                game.PlaceBet();
                game.CheckToDraw();
                game.Draw(new int[0]);
                PassToTheEnd(game);
                Assert.AreEqual(CheatOutcome.Played, Only(game).Outcome, "The Fall gets through.");
            }
            else
            {
                HellPokerGame game = Game(KingsPair, HouseTwos, new BuyoutCheat(), sinner);
                game.PlaceBet();
                game.CheckToDraw();
                Assert.AreEqual(CheatOutcome.Played, Only(game).Outcome, "A major cheat gets through.");
            }
            Assert.AreEqual(1, sinner.Charges, "The ward is not spent on what it cannot stop.");
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

        [Test]
        public void TheWarlock_KeepsTheWard_WhenTheCheatWouldComeToNothing()
        {
            var sinner = new Sinner(new Warlock());
            HellPokerGame game = Game(Nothing, HouseFullHouse, new TitheCheat(), sinner);   // a lost hand: nothing to tithe
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            PassToTheEnd(game);

            Assert.AreEqual(CheatOutcome.Fizzled, Only(game).Outcome);
            Assert.AreEqual(1, sinner.Charges);
        }

        // ------------------------------------------------------------------ the King

        [Test]
        public void TheKingsCrown_ForgivesAnAnteMore()
        {
            int Win(Sinner sinner)
            {
                HellPokerGame game = Game("2C 9C JC 4C KC", HouseTwos, null, sinner);   // a flush against twos
                game.PlaceBet();
                game.CheckToDraw();
                game.Draw(new int[0]);
                PassToTheEnd(game);
                Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
                return -game.LastRound.YearsChange;
            }

            Assert.AreEqual(Win(new Sinner(new Peasant())) + 25, Win(new Sinner(new King())), "Ante 100: the crown adds a quarter.");
            Assert.AreEqual(Win(new Sinner(new Peasant())) + 100, Win(new Sinner(new King(crownPercent: 100))), "The ante multiplier +1.");
        }

        private static HellPokerGame KingWith(ICheat cheat, string player, out Sinner sinner, string house = HouseTwos)
        {
            sinner = new Sinner(new King());
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
            Assert.AreEqual(0, sinner.Charges);

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
            var sinner = new Sinner(new King());
            Dealer lucifer = DealerRoster.Lucifer;
            var random = new FirstChoice();
            var game = new HellPokerGame(lucifer.ApplyTo(GameRules.Default), TestDecks.Stacked($"2C 9C JC 4C KC {HouseTwos} {Blanks}"),
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
        public void TheKingProtects_OnlyBeforeTheDraw_ACardHeSees_OncePerTable()
        {
            HellPokerGame game = KingWith(null, KingsPair, out Sinner sinner);
            Assert.IsFalse(game.CanProtect(4), "Not yet turned.");
            Assert.IsTrue(game.Protect(2));
            Assert.IsFalse(game.CanProtect(1), "No charge left at this table.");
            game.CheckToDraw();
            game.Draw(new int[0]);
            sinner.SitDown();
            Assert.IsFalse(game.CanProtect(0), "After the draw: too late.");
            Assert.AreEqual(1, sinner.Charges, "A new table: the crown can protect again.");
        }

        [Test]
        public void NoOneElse_CanProtect()
        {
            HellPokerGame game = Game(KingsPair, HouseTwos, null, new Sinner(new Warlock()));
            game.PlaceBet();

            Assert.IsFalse(game.CanProtect(0));
            Assert.IsFalse(game.Protect(0));
        }

        // ------------------------------------------------------------------ the save and the records

        [Test]
        public void TheSave_v4_KeepsTheClassAndItsCharges()
        {
            var snapshot = new RunSnapshot("belial", 900, 7, new RunStats(1000, "belial"), classId: "warlock", classCharges: 0);

            string text = snapshot.Encode();
            StringAssert.StartsWith("v=4", text);
            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot back));

            Assert.AreEqual("warlock", back.ClassId);
            Assert.AreEqual(0, back.ClassCharges);
        }

        [Test]
        public void AV3Save_ReadsAsAPeasantsRun_WithTheAbilityUntouched()
        {
            string text = "v=3\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\n" +
                          "lucifer=0\norigin=\nattempts=0\nmalice=1\ncheat.major=0";

            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot snapshot));
            Assert.AreEqual("peasant", snapshot.ClassId);
            Assert.IsNull(snapshot.ClassCharges);
            Assert.AreEqual(1, new Sinner(SinnerRoster.Find(snapshot.ClassId), snapshot.ClassCharges).Charges);
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
            ICheat shown = null)
        {
            _view = new FakeTableView();
            return _presenter = new TablePresenter((d, sinner) =>
            {
                var random = new FirstChoice();
                ICheatPolicy policy = cheat == null ? null : shown == null ? (ICheatPolicy)new OnlyCheat(cheat) : new Lying(cheat, shown);
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(policy, 1, random, sinner), random, sinner);
            }, _view, null, archive);
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
        public void ANewRun_AsTheKing_StartsAt1250_AndTheDemonGreetsTheCrown()
        {
            Table(null).StartNewRun(DealerRoster.Mammon, SinnerRoster.King);

            Assert.AreEqual(1250, _game.Years);
            Assert.AreEqual(1250, _presenter.Stats.HighestYears);
            Assert.AreEqual("A king! How fortunate. A crown counts as collateral.", _view.DealerView.LastLine);
            Assert.AreEqual("king", _view.Sinner.ClassId);
            Assert.AreEqual(1, _view.Sinner.Charges);
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
        public void AWard_FlaresOverTheCards_AndTheDemonFumes()
        {
            Table(new CollateralCheat()).StartNewRun(DealerRoster.Mammon, SinnerRoster.Warlock);
            _view.PressAction();

            _presenter.CheckToDraw();

            Assert.That(_view.Moments.Select(m => m.moment), Has.Member(TableMoment.Ward));
            Assert.AreEqual("WARD", _view.Moments.Last(m => m.moment == TableMoment.Ward).text);
            Assert.AreEqual(DealerMood.Annoyed, _view.DealerView.LastMood);
            CollectionAssert.Contains(new[] { "A seal on the account? Who taught you that trick?", "Warded. I will note the expense." },
                _view.DealerView.LastLine);
            Assert.AreEqual(0, _view.Sinner.Charges);
        }

        [Test]
        public void TheKing_PressesK_ThenACard_AndTheCrownProtectsIt()
        {
            Table(new BurningCardCheat(), KingsPair, HouseTwos).StartNewRun(DealerRoster.Mammon, SinnerRoster.King);
            _view.PressAction();

            _presenter.ToggleProtect();
            StringAssert.Contains("crown protects", _view.Message);
            _presenter.ToggleDiscard(0);

            Assert.IsTrue(_game.IsPlayerCardProtected(0));
            Assert.AreEqual(CardMark.Protected, _view.PlayerView.Slots[0].Mark);
            Assert.AreEqual(0, _view.Sinner.Charges);
            _presenter.CheckToDraw();
            Assert.AreEqual(new Card(Rank.King, Suit.Spades), _game.PlayerHand[0]);
        }

        [Test]
        public void ThePeasantsFreeFold_IsTold()
        {
            Table(null).StartNewRun(DealerRoster.Mammon, SinnerRoster.Peasant);
            _view.PressAction();

            _view.PressBet(BetAction.Fold);

            Assert.AreEqual(1000, _game.Years);
            StringAssert.Contains("honest heart", _view.Message);
        }

        [Test]
        public void TheClass_GoesAlongToEveryTable_APerTableAbilityRefills_ASavedOneDoesNot()
        {
            var archive = new RunArchive(new MemoryStore());
            Table(new CollateralCheat(), archive: archive).StartNewRun(DealerRoster.Mammon, SinnerRoster.Warlock);
            _view.PressAction();
            _presenter.CheckToDraw();   // the ward is spent
            Assert.AreEqual(0, _presenter.Sinner.Charges);
            _presenter.PerformAction();
            while (_game.Phase != GamePhase.RoundOver) _view.PressBet(BetAction.Pass);
            _presenter.PerformAction();
            Assert.AreEqual(0, archive.LoadRun().ClassCharges, "Saved spent.");
            Assert.AreEqual("warlock", archive.LoadRun().ClassId);

            _presenter.SwitchTable(DealerRoster.Belial);

            Assert.AreEqual("warlock", _game.Sinner.Id, "The same sinner at the new table.");
            Assert.AreEqual(1, _game.Sinner.Charges, "A new table: the ward is back.");

            // Closing and opening the game does not refill it.
            var saved = new RunSnapshot("belial", 1000, 3, new RunStats(1000, "belial"), classId: "warlock", classCharges: 0);
            _presenter.Dispose();
            Table(null).Resume(DealerRoster.Belial, saved);
            Assert.AreEqual("warlock", _game.Sinner.Id);
            Assert.AreEqual(0, _game.Sinner.Charges);
        }
    }
}
