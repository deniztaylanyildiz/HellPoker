using System.Linq;
using System.Text.RegularExpressions;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The sealed pact and CHECK TO DRAW at the table, and a hand left unfinished when the game closes:
    /// saved from the deal on, and forfeited when the run is resumed.
    /// </summary>
    public class PactPresenterTests
    {
        private const string Blanks = "3S 6C JD QC 10S 2S 4H 5C 6D 7S";

        private MemoryStore _store;
        private RunArchive _archive;
        private FakeTableView _view;
        private HellPokerGame _game;
        private TablePresenter _presenter;

        /// <summary>Default: the player's flush against the House's pair of twos (which draws three blanks).</summary>
        private TablePresenter CreatePresenter(string player = "2C 9C JC 4C KC", string house = "2D 2H 5S 7H 9D", int startingYears = 1000)
        {
            _view = new FakeTableView();
            return new TablePresenter(d => _game = new HellPokerGame(
                d.ApplyTo(new GameRules(startingYears, 5000)),
                TestDecks.Stacked($"{player} {house} {Blanks}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                d.Payouts), _view, null, _archive);
        }

        private void Start(Dealer dealer = null, int startingYears = 1000)
        {
            _presenter = CreatePresenter(startingYears: startingYears);
            _presenter.StartNewRun(dealer ?? DealerRoster.Mammon);
        }

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _archive = new RunArchive(_store);
        }

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        // ------------------------------------------------------------------ the pact is sealed

        [Test]
        public void Sealing_IsAnnounced_ByTheTable_AndTheDealer()
        {
            Start();
            _view.PressAction();
            _view.PressBet(BetAction.Raise);
            int lines = _view.DealerView.LinesSaid;

            _view.PressBet(BetAction.Raise);   // 300: the table is full

            Assert.That(_view.Moments.Select(m => m.moment), Has.Member(TableMoment.PactSealed));
            Assert.AreEqual("THE PACT IS SEALED", _view.Moments.Last(m => m.moment == TableMoment.PactSealed).text);
            Assert.Greater(_view.DealerView.LinesSaid, lines);
            CollectionAssert.Contains(new[]
            {
                "Signed and witnessed. No backing out of this contract.",
                "The terms are final. Let the cards settle the account.",
                "Sealed in wax and ink. Now we see who pays."
            }, _view.DealerView.LastLine);
            StringAssert.Contains("sealed", _view.Message);
        }

        [Test]
        public void Sealed_TheRemainingCardsTurnOneByOne_WithABeatBetween()
        {
            Start();
            _view.PressAction();               // cards 1-3 face up
            _view.PressBet(BetAction.Raise);   // card 4
            _view.PlayerView.ShownFaceUp.Clear();
            _view.Pauses.Clear();

            _view.PressBet(BetAction.Raise);   // sealed: card 5 turns on its own

            Assert.AreEqual(GamePhase.Drawing, _game.Phase);
            CollectionAssert.IsSubsetOf(new[] { 4, 5 }, _view.PlayerView.ShownFaceUp);
            Assert.AreEqual(1, _view.Pauses.Count, "One beat per card left.");
            Assert.IsFalse(_view.BetControls.Visible, "Pass and fold are gone.");
        }

        [Test]
        public void Sealed_AfterTheDraw_TheHouseTurnsCardByCard_IntoTheShowdown()
        {
            Start();
            _view.PressAction();
            _view.PressBet(BetAction.Raise);
            _view.PressBet(BetAction.Raise);   // sealed before the draw
            _view.HouseView.ShownFaceUp.Clear();
            _view.Pauses.Clear();

            _view.PressAction();               // stand pat: the hand plays out

            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, _view.HouseView.ShownFaceUp.Take(6).ToArray());
            Assert.GreaterOrEqual(_view.Pauses.Count, 5, "A beat before each of the House's five cards.");
            Assert.AreEqual("NEXT HAND", _view.ActionLabel);
            Assert.AreEqual(1, _view.Moments.Count(m => m.moment == TableMoment.PactSealed), "The seal is announced once.");
        }

        [Test]
        public void ARaiseThatFillsTheTable_OnTheLastDecision_IsNoSeal()
        {
            Start();
            _view.PressAction();
            _presenter.CheckToDraw();
            _view.PressAction();               // stand pat
            _view.PressBet(BetAction.Pass);    // the House shows its cards

            _view.PressBet(BetAction.Raise);   // 100 + 200: full, but nothing is left to play out

            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            Assert.That(_view.Moments.Select(m => m.moment), Has.No.Member(TableMoment.PactSealed));
        }

        [Test]
        public void SealedInTheSoulZone_ShowsNoNumbers()
        {
            _presenter = CreatePresenter(player: "2C 5D 7H 9S JC", house: "KS KH 4D 4C 9H");
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.TakeOver(2100, 2);
            _presenter.SwitchTable(DealerRoster.Mammon);
            _view.TextLog.Clear();
            _view.NumberLog.Clear();

            _view.PressAction();
            _view.PressBet(BetAction.Raise);
            _view.PressBet(BetAction.Raise);   // the soul's table is full
            _view.PressAction();

            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            Assert.That(_view.NumberLog, Has.All.EqualTo(0));
            foreach (string text in _view.TextLog)
                Assert.IsFalse(text != null && Regex.IsMatch(text, @"\d{2,}"), $"A number reached the table: \"{text}\"");
        }

        // ------------------------------------------------------------------ CHECK TO DRAW

        [Test]
        public void CheckToDraw_ShowsBeforeTheDraw_AndTurnsTheRestOfTheCards()
        {
            Start();
            _view.PressAction();
            Assert.IsTrue(_view.BetControls.ShowCheckToDraw);
            Assert.IsTrue(_view.BetControls.CanCheckToDraw);

            _view.PressCheckToDraw();

            Assert.AreEqual(GamePhase.Drawing, _game.Phase);
            Assert.AreEqual(5, _view.PlayerView.FaceUpCount);
            Assert.AreEqual(100, _view.Pot, "Nothing raised.");
        }

        [Test]
        public void CheckToDraw_IsNotOffered_AfterTheDraw()
        {
            Start();
            _view.PressAction();
            _view.PressCheckToDraw();
            _view.PressAction();

            Assert.AreEqual(GamePhase.DrawReveal, _game.Phase);
            Assert.IsTrue(_view.BetControls.Visible);
            Assert.IsFalse(_view.BetControls.ShowCheckToDraw);
        }

        [Test]
        public void CheckToDraw_IsLocked_InTheFinalStretch_AndSaysWhy()
        {
            Start(startingYears: 200);
            _view.PressAction();

            Assert.IsTrue(_view.BetControls.ShowCheckToDraw);
            Assert.IsFalse(_view.BetControls.CanCheckToDraw);
            _view.PressCheckToDraw();

            Assert.AreEqual(GamePhase.PlayerReveal, _game.Phase);
            Assert.AreEqual(3, _game.PlayerCardsRevealed);
            StringAssert.Contains("No checking", _view.Message);
            Assert.AreEqual(Tone.Warning, _view.MessageTone);
        }

        [Test]
        public void CheckToDraw_WhileAnimating_OnlyHurries()
        {
            Start();
            _view.PressAction();
            _view.IsBusy = true;

            _view.PressCheckToDraw();

            Assert.AreEqual(GamePhase.PlayerReveal, _game.Phase);
            Assert.AreEqual(1, _view.Skips);
        }

        // ------------------------------------------------------------------ a hand in progress is saved

        [Test]
        public void TheDeal_SavesTheHandInProgress()
        {
            Start();

            _view.PressAction();

            RunSnapshot saved = _archive.LoadRun();
            Assert.IsNotNull(saved.Hand, "From the deal on, the save knows a hand is being played.");
            Assert.AreEqual(100, saved.Hand.Stake);
            Assert.AreEqual(100, saved.Hand.Ante);
            Assert.IsFalse(saved.Hand.IsAfterDraw);
            Assert.AreEqual(1000, saved.Years, "Nothing settled yet.");
            Assert.AreEqual(1, saved.RoundsPlayed);
        }

        [Test]
        public void TheSave_FollowsTheStakeAndTheDraw()
        {
            Start();
            _view.PressAction();
            _view.PressBet(BetAction.Raise);
            Assert.AreEqual(200, _archive.LoadRun().Hand.Stake);

            _presenter.CheckToDraw();
            _view.PressAction();

            Assert.IsTrue(_archive.LoadRun().Hand.IsAfterDraw);
        }

        [Test]
        public void ASettledHand_LeavesNoHandInTheSave()
        {
            Start();
            _view.PressAction();
            _presenter.CheckToDraw();
            _view.PressAction();
            while (_game.Phase != GamePhase.RoundOver) _view.PressBet(BetAction.Pass);

            Assert.IsNull(_archive.LoadRun().Hand);
            _view.PressAction();
            Assert.IsNull(_archive.LoadRun().Hand);
        }

        // ------------------------------------------------------------------ ... and forfeited when the game comes back

        private void ResumeWithHand(Dealer dealer, int years, HandInProgress hand, RunStats stats = null)
        {
            _archive.SaveRun(new RunSnapshot(dealer.Id, years, 5, stats ?? new RunStats(4, 800, years, null, new[] { dealer.Id }, false), hand));
            _presenter = CreatePresenter();
            _presenter.Resume(dealer, _archive.LoadRun());
        }

        [Test]
        public void ClosingMidHand_CountsAsAFold_BeforeTheDraw()
        {
            ResumeWithHand(DealerRoster.Mammon, 900, new HandInProgress(200, 100, false, false, false));

            Assert.AreEqual(1000, _game.Years, "Half the 200 on the table, as a fold before the draw.");
            Assert.AreEqual(GamePhase.Betting, _game.Phase);
            Assert.AreEqual(5, _game.RoundNumber, "The hand was already counted at the deal.");
            Assert.AreEqual(5, _presenter.Stats.HandsPlayed);
            Assert.AreEqual(1000, _view.SentenceView.Years);
            StringAssert.Contains("+100", _view.Message);
            Assert.AreEqual(Tone.Bad, _view.MessageTone);
            Assert.AreEqual(DealerMood.Gloating, _view.DealerView.LastMood);
            CollectionAssert.Contains(new[]
            {
                "You walked out mid-hand? The ledger noticed. Debited, with interest.",
                "Skipping out on an open account? I charged it as forfeit."
            }, _view.DealerView.LastLine);
        }

        [Test]
        public void ClosingMidHand_AfterTheDraw_CostsTheWholeStake()
        {
            ResumeWithHand(DealerRoster.Mammon, 900, new HandInProgress(200, 100, true, false, false));

            Assert.AreEqual(1100, _game.Years);
        }

        [Test]
        public void ClosingASealedHand_LosesTheWholeWager()
        {
            ResumeWithHand(DealerRoster.Lilith, 900, new HandInProgress(300, 100, false, false, true));

            Assert.AreEqual(900 + 375, _game.Years, "300 lost at Lilith's × 1.25 — no fold for a sealed hand.");
            StringAssert.Contains("sealed", _view.Message);
        }

        [Test]
        public void ClosingMidHand_OnTheSoul_BurnsHalfAgain_WithoutNumbers()
        {
            ResumeWithHand(DealerRoster.Mammon, 2100, new HandInProgress(200, 100, false, true, false));

            Assert.AreEqual(2100 + 150, _game.Years, "200 × 50% × 1.5.");
            Assert.IsTrue(_view.Soul.Visible);
            Assert.IsFalse(Regex.IsMatch(_view.Message, @"\d{2,}"), _view.Message);
            StringAssert.Contains("soul", _view.Message);
        }

        [Test]
        public void AfterTheForfeit_TheSaveIsBetweenHands()
        {
            ResumeWithHand(DealerRoster.Mammon, 900, new HandInProgress(200, 100, false, false, false));

            RunSnapshot saved = _archive.LoadRun();
            Assert.IsNull(saved.Hand, "Closing again does not charge twice.");
            Assert.AreEqual(1000, saved.Years);
            Assert.IsTrue(_presenter.CanContinue);
        }

        [Test]
        public void ClosingMidHand_CanDamn()
        {
            ResumeWithHand(DealerRoster.Mammon, 2950, new HandInProgress(50, 50, true, true, true),
                new RunStats(4, 800, 2950, null, new[] { "mammon" }, true));

            Assert.AreEqual(GamePhase.Damned, _game.Phase);
            Assert.AreEqual("THE END", _view.ActionLabel);
            Assert.IsNull(_archive.LoadRun());
            Assert.AreEqual(1, _archive.LoadRecords().Damnations);
        }

        [Test]
        public void Snapshot_WithAHand_SurvivesTheRoundTrip()
        {
            var snapshot = new RunSnapshot("belial", 1800, 7, new RunStats(1000, "belial"), new HandInProgress(250, 50, true, true, true));

            Assert.IsTrue(RunSnapshot.TryDecode(snapshot.Encode(), out RunSnapshot back));

            Assert.AreEqual(250, back.Hand.Stake);
            Assert.AreEqual(50, back.Hand.Ante);
            Assert.IsTrue(back.Hand.IsAfterDraw);
            Assert.IsTrue(back.Hand.IsSoulHand);
            Assert.IsTrue(back.Hand.IsSealed);
        }

        [TestCase("hand.stake=0\nhand.ante=0\nhand.drawn=0\nhand.soul=0\nhand.sealed=0")]
        [TestCase("hand.stake=100\nhand.ante=200\nhand.drawn=0\nhand.soul=0\nhand.sealed=0")]
        [TestCase("hand.stake=100\nhand.ante=100\nhand.drawn=yes\nhand.soul=0\nhand.sealed=0")]
        [TestCase("hand.stake=100\nhand.ante=100")]
        public void ABrokenHand_MakesTheSaveUnreadable(string handLines)
        {
            string text = "v=1\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\n" + handLines;

            Assert.IsFalse(RunSnapshot.TryDecode(text, out _));
        }

        [Test]
        public void ASaveWithoutHandLines_IsBetweenHands()
        {
            string text = "v=1\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0";

            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot snapshot));
            Assert.IsNull(snapshot.Hand);
        }
    }
}
