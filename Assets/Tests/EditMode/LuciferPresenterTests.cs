using System.Collections.Generic;
using System.Linq;
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
    /// The run around Lucifer at the table: summoned at the gate between hands, unable to leave, cast down above it to the
    /// demon the player came from, summoned again, and set free only at his table (or by Wild Bill's hand). Also saved.
    /// </summary>
    public class LuciferPresenterTests
    {
        // Each new table (a game) deals the next of these decks: player five, House five, then the draws.
        private const string Win = "2C 9C JC 4C KC  2D 2H 5S 7H 9D  3S 6C JD QC 10S 2S";        // flush beats twos (×5)
        private const string LoseBig = "2C 5D 7H 9S JC  KS KH KD 4C 4H  3S 6C JD QC 10S 2S";    // full house (×8) beats nothing
        private const string LoseTwoPair = "2C 5D 7H 9S JC  KS KH 4D 4C 9H  3S 6C JD QC 10S 2S"; // two pair (×2) beats nothing
        private const string DeadMan = "AS AC 8S 8C 3H  KS KH 4D 4C 9H  3S 6C JD QC 10S 2S";

        private MemoryStore _store;
        private RunArchive _archive;
        private FakeTableView _view;
        private TablePresenter _presenter;
        private readonly Queue<string> _decks = new Queue<string>();
        private string _lastDeck;

        private IHellPokerGame Game => _presenter.Game;

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _archive = new RunArchive(_store);
            _decks.Clear();
        }

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        /// <summary>A presenter with Lucifer below the gate. Every table built deals the next queued deck (the last one repeats).</summary>
        private void CreatePresenter(params string[] decks)
        {
            foreach (string deck in decks) _decks.Enqueue(deck);
            _view = new FakeTableView();
            _presenter = new TablePresenter(d =>
            {
                _lastDeck = _decks.Count > 0 ? _decks.Dequeue() : _lastDeck;
                return new HellPokerGame(
                    d.ApplyTo(new GameRules()),
                    TestDecks.Stacked(_lastDeck),
                    HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy(d.MaxDiscards)),
                    new HouseDrawStrategy(d.MaxDiscards),
                    d.Payouts);
            }, _view, null, _archive, DealerRoster.Lucifer);
        }

        /// <summary>Starts at Mammon's table with this sentence; the first deck given is that table's.</summary>
        private void StartAt(int years, params string[] decks)
        {
            CreatePresenter(new[] { Win }.Concat(decks).ToArray());   // the new run's own first table is left at once
            _presenter.StartNewRun(DealerRoster.Mammon);
            Game.TakeOver(years, 3);
            _presenter.SwitchTable(DealerRoster.Mammon);
        }

        /// <summary>Deals and plays the hand out by passing, standing pat; then moves on to the next hand.</summary>
        private void PlayHandAndMoveOn()
        {
            _view.PressAction();                                    // deal
            if (Game.Phase == GamePhase.PlayerReveal) _presenter.CheckToDraw();
            _view.PressAction();                                    // stand pat
            for (int guard = 0; guard < 5 && Game.Phase != GamePhase.RoundOver && !Game.IsGameOver; guard++)
                _view.PressBet(BetAction.Pass);
            if (!Game.IsGameOver)
                _view.PressAction();                                // next hand: the gate is passed here
        }

        // ------------------------------------------------------------------ summoned

        [Test]
        public void WinningDownToTheGate_SummonsThePlayer_BetweenHands()
        {
            // 260 at Mammon: unit 25, a flush forgives 25 + 100 = 125 → 135.
            StartAt(260, Win, LoseBig);
            _view.PressAction();
            _presenter.CheckToDraw();
            _view.PressAction();
            while (Game.Phase != GamePhase.RoundOver) _view.PressBet(BetAction.Pass);
            Assert.AreEqual("mammon", _presenter.CurrentDealerId, "Not on the result: only between hands.");

            _view.PressAction();   // next hand

            Assert.AreEqual("lucifer", _presenter.CurrentDealerId);
            Assert.IsTrue(_presenter.IsAtFinalTable);
            Assert.AreEqual(135, Game.Years, "The sentence goes along.");
            Assert.AreEqual(GamePhase.Betting, Game.Phase);
            Assert.AreEqual(1, _presenter.Gate.Attempts);
            Assert.AreEqual("mammon", _presenter.Gate.OriginDealerId);
            Assert.AreEqual(("lucifer", SeatChange.Summoned), _view.DealerView.Seats.Last());
            Assert.AreEqual(50, _view.Ante, "His own stakes.");
            Assert.AreEqual(LeaveState.Summoned, _view.Leave);
            CollectionAssert.Contains(_view.TextLog, "Your debt is nearly paid... and someone else has noticed you. My condolences.");
            CollectionAssert.Contains(_view.TextLog, "You climbed down far. Sit. Only I can let you go.");
            Assert.AreEqual("So close. I can hear you hoping.", _view.DealerView.LastLine, "At 135 one hand could end it: his last moments.");
        }

        [Test]
        public void TheOldDemonSpeaks_BeforeTheHallGoesDark()
        {
            StartAt(200, Win, LoseBig);

            int farewell = _view.TextLog.IndexOf("Your debt is nearly paid... and someone else has noticed you. My condolences.");
            int greeting = _view.TextLog.LastIndexOf("You climbed down far. Sit. Only I can let you go.");
            Assert.GreaterOrEqual(farewell, 0);
            Assert.Greater(greeting, farewell);
            Assert.AreEqual(SeatChange.Summoned, _view.DealerView.Seats.Last().change);
        }

        [TestCase(200, false)]
        [TestCase(151, false)]
        [TestCase(150, true)]
        [TestCase(40, true)]
        public void HisLastMoments_BurnOnceOneHandCouldEndIt(int years, bool burning)
        {
            StartAt(years, Win, LoseBig);

            Assert.IsTrue(_presenter.IsAtFinalTable);
            Assert.AreEqual(burning, _view.FinalStretch);
        }

        [Test]
        public void AtHisTable_LeavingIsRefused_InHisVoice()
        {
            StartAt(200, Win, LoseBig);
            Assert.IsTrue(_presenter.IsAtFinalTable, "At 200 the player is summoned at once.");
            int asked = 0;
            _presenter.LeaveRequested += () => asked++;

            _view.PressLeave();

            Assert.AreEqual(0, asked);
            CollectionAssert.Contains(new[] { "Leave? Nothing leaves my table.", "Sit down." }, _view.DealerView.LastLine);
        }

        // ------------------------------------------------------------------ cast down

        [Test]
        public void LosingAboveTheGate_CastsThePlayerDown_ToWhereTheyCameFrom()
        {
            // Summoned at 200; a full house costs (50 + 350) × 1.25 = 500 → 700.
            StartAt(200, Win, LoseBig, Win);

            PlayHandAndMoveOn();

            Assert.AreEqual("mammon", _presenter.CurrentDealerId);
            Assert.AreEqual(700, Game.Years, "Already above 500: it stays.");
            Assert.AreEqual(("mammon", SeatChange.CastDown), _view.DealerView.Seats.Last());
            Assert.IsFalse(_presenter.Gate.IsAtLucifer);
            Assert.AreEqual(1, _presenter.Gate.CastDowns);
            CollectionAssert.Contains(_view.TextLog, "Not yet worthy. Down you go.");
            CollectionAssert.Contains(new[] { "Back already? The ledger reopens. With interest.", "Thrown down like an old coin. Sit. We have accounts to settle." },
                _view.DealerView.LastLine);
        }

        [Test]
        public void ASmallFall_StillLandsAt500()
        {
            // Summoned at 200; two pair costs (50 + 50) × 1.25 = 125 → 325 — above the gate.
            StartAt(200, Win, LoseTwoPair, Win);

            PlayHandAndMoveOn();

            Assert.AreEqual("mammon", _presenter.CurrentDealerId);
            Assert.AreEqual(500, Game.Years);
        }

        [Test]
        public void ReachingTheGateAgain_SummonsAgain_AndHeRemembers()
        {
            // Cast down to 500; at Mammon a flush forgives 50 + 200 = 250 → 250: the gate.
            StartAt(200, Win, LoseTwoPair, Win, Win);
            PlayHandAndMoveOn();
            Assert.AreEqual(500, Game.Years);

            PlayHandAndMoveOn();

            Assert.AreEqual("lucifer", _presenter.CurrentDealerId);
            Assert.AreEqual(2, _presenter.Gate.Attempts);
            Assert.AreEqual("Again. I remember your hands. Sit.", _view.DealerView.LastLine);
        }

        // ------------------------------------------------------------------ the end

        [Test]
        public void BeatingHim_EndsTheRun_AsTheMorningStarFalls()
        {
            StartAt(150, Win, Win);
            RunSummary summary = null;
            _presenter.RunEnded += s => summary = s;

            PlayHandAndMoveOn();

            Assert.AreEqual(GamePhase.Absolved, Game.Phase);
            Assert.AreEqual("THE END", _view.ActionLabel);
            StringAssert.StartsWith("His eyes go dark.", _view.Message);
            RecordBook records = _archive.LoadRecords();
            Assert.AreEqual(1, records.Absolutions);
            Assert.AreEqual(1, records.LuciferReached);
            Assert.AreEqual(1, records.LuciferDefeated);
            Assert.AreEqual(1, records.FewestLuciferAttempts);
            Assert.AreEqual(0, records.WildBillEscapes);
            Assert.AreEqual(1, records.AbsolutionsAt("mammon"), "Credited to the demon the player came from.");
            _view.PressAction();
            Assert.IsTrue(summary.Absolved);
            Assert.IsTrue(summary.BeatLucifer);
            Assert.IsFalse(summary.WildBill);
            Assert.AreEqual(1, summary.LuciferAttempts);
        }

        [Test]
        public void AtHisTable_TheCounter_MeasuresAgainstTheGate_AndCountsTheAttempt()
        {
            StartAt(200, Win, LoseTwoPair, Win, Win);
            Assert.AreEqual("LUCIFER: ATTEMPT 1", _view.SentenceView.Label);
            Assert.AreEqual("Cast down above 250", _view.SentenceView.LimitText);
            Assert.AreEqual(250, _view.SentenceView.SoulLine);

            PlayHandAndMoveOn();   // cast down to Mammon at 500
            Assert.AreEqual("YEARS LEFT IN HELL", _view.SentenceView.Label);
            Assert.AreEqual(2000, _view.SentenceView.SoulLine);

            PlayHandAndMoveOn();   // back down to 250: summoned again
            Assert.AreEqual("LUCIFER: ATTEMPT 2", _view.SentenceView.Label);
        }

        [Test]
        public void SittingDownBelowTheGate_MeansBeingSummoned_FromThatTable()
        {
            StartAt(900, Win, Win);
            Game.TakeOver(180, 3);

            _presenter.SwitchTable(DealerRoster.Belial);

            Assert.AreEqual("lucifer", _presenter.CurrentDealerId);
            Assert.AreEqual("belial", _presenter.Gate.OriginDealerId);
        }

        [Test]
        public void WildBillsHand_AboveTheGate_SetsThePlayerFree_WithoutHim()
        {
            StartAt(900, DeadMan, DeadMan);

            PlayHandAndMoveOn();

            Assert.AreEqual(GamePhase.Absolved, Game.Phase);
            Assert.AreEqual("mammon", _presenter.CurrentDealerId);
            Assert.IsFalse(_presenter.Gate.ReachedLucifer);

            RunSummary summary = null;
            _presenter.RunEnded += s => summary = s;
            _view.PressAction();
            Assert.IsTrue(summary.WildBill);
            Assert.IsFalse(summary.BeatLucifer);
            Assert.AreEqual(0, summary.LuciferAttempts);
            Assert.AreEqual(1, _archive.LoadRecords().WildBillEscapes);
            Assert.AreEqual(0, _archive.LoadRecords().LuciferReached);
        }

        [Test]
        public void ResumingAtHisTable_ShowsTheAttemptFromTheSave()
        {
            _archive.SaveRun(new RunSnapshot("lucifer", 200, 30, new RunStats(1000, "belial"), null, true, "belial", 3));
            CreatePresenter(Win);

            _presenter.Resume(DealerRoster.Lucifer, _archive.LoadRun(), DealerRoster.Belial);

            Assert.AreEqual("LUCIFER: ATTEMPT 3", _view.SentenceView.Label);
        }

        // ------------------------------------------------------------------ saved and closed

        [Test]
        public void AtHisTable_TheSaveKnowsIt()
        {
            StartAt(200, Win, LoseBig);

            RunSnapshot saved = _archive.LoadRun();

            Assert.AreEqual("lucifer", saved.DealerId);
            Assert.IsTrue(saved.AtLucifer);
            Assert.AreEqual("mammon", saved.OriginDealerId);
            Assert.AreEqual(1, saved.LuciferAttempts);
        }

        [Test]
        public void AfterAFall_TheSaveKeepsTheAttempts()
        {
            StartAt(200, Win, LoseTwoPair, Win);
            PlayHandAndMoveOn();

            RunSnapshot saved = _archive.LoadRun();

            Assert.AreEqual("mammon", saved.DealerId);
            Assert.IsFalse(saved.AtLucifer);
            Assert.AreEqual(1, saved.LuciferAttempts);
        }

        [Test]
        public void ResumingAtHisTable_SeatsThePlayerThere_WithTheOriginKnown()
        {
            _archive.SaveRun(new RunSnapshot("lucifer", 200, 30, new RunStats(1000, "belial"), null, true, "belial", 2));
            CreatePresenter(LoseTwoPair, Win);

            _presenter.Resume(DealerRoster.Lucifer, _archive.LoadRun(), DealerRoster.Belial);

            Assert.IsTrue(_presenter.IsAtFinalTable);
            Assert.AreEqual(2, _presenter.Gate.Attempts);

            PlayHandAndMoveOn();   // 200 + 125 = 325: cast down — to Belial
            Assert.AreEqual("belial", _presenter.CurrentDealerId);
            Assert.AreEqual(500, Game.Years);
        }

        [Test]
        public void ClosingMidHand_AtHisTable_IsAForfeit_AndCanCastDown()
        {
            // 220 at his table with 150 on it, sealed: lost whole — 150 × 1.25 = 188 → 408: cast down to 500.
            _archive.SaveRun(new RunSnapshot("lucifer", 220, 30, new RunStats(1000, "lilith"),
                new HandInProgress(150, 50, false, false, true), true, "lilith", 1));
            CreatePresenter(Win, Win);

            _presenter.Resume(DealerRoster.Lucifer, _archive.LoadRun(), DealerRoster.Lilith);

            Assert.AreEqual("lilith", _presenter.CurrentDealerId);
            Assert.AreEqual(500, Game.Years);
            CollectionAssert.Contains(_view.TextLog, "You closed your eyes. I did not. The wager is mine.");
            StringAssert.Contains("+188", _view.Message);
        }
    }
}
