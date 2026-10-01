using HellPoker.Core.Betting;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class TablePresenterTests
    {
        private FakeTableView _view;
        private HellPokerGame _game;
        private TablePresenter _presenter;
        private Dealer _lastDealer;

        /// <summary>
        /// Default: player gets a flush, the house a pair of twos that draws three blanks.
        /// Every run gets a fresh game on the same stacked deck, whatever the dealer.
        /// </summary>
        private void Setup(string player = "2C 9C JC 4C KC", string house = "2D 2H 5S 7H 9D", string rest = "3S 4S 6D AH",
            int startingYears = 1000, Dealer dealer = null, IHouseBettingStrategy houseBetting = null)
        {
            _view = new FakeTableView();
            _presenter = new TablePresenter(d =>
            {
                _lastDealer = d;
                return _game = new HellPokerGame(
                    d.ApplyTo(new GameRules(startingYears, 5000)),
                    TestDecks.Stacked($"{player} {house} {rest}"),
                    HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(),
                    d.Payouts,
                    houseBetting);
            }, _view);
            _presenter.StartNewRun(dealer ?? DealerRoster.Mammon);
        }

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private void PassUntil(GamePhase phase)
        {
            while (_game.Phase != phase)
                _view.PressBet(BetAction.Pass);
        }

        private void DealAndDraw()
        {
            _view.PressAction();
            PassUntil(GamePhase.Drawing);
            _view.PressAction();
        }

        // ------------------------------------------------------------------ betting

        [Test]
        public void Start_ShowsBettingTable_WithTheAnte()
        {
            Setup();

            Assert.AreEqual(100, _view.Ante);
            StringAssert.Contains("100", _view.Message);
            Assert.AreEqual("Win: at least −100 years   ·   Lose: at least +100 years", _view.StakeInfo);
            Assert.IsFalse(_view.BetControls.Visible);
            Assert.IsTrue(_view.PlayerView.IsEmpty);
            Assert.AreEqual("DEAL", _view.ActionLabel);
            Assert.AreEqual(1000, _view.SentenceView.Years);
            Assert.IsFalse(_view.FinalStretch);
        }

        [Test]
        public void Ante_FollowsTheSentence()
        {
            Setup(startingYears: 650);

            Assert.AreEqual(50, _view.Ante);
        }

        [Test]
        public void Deal_TurnsThreeCards_AndOffersBetDecision()
        {
            Setup();

            _view.PressAction();

            Assert.AreEqual(GamePhase.PlayerReveal, _game.Phase);
            Assert.AreEqual(3, _view.PlayerView.FaceUpCount);
            Assert.AreEqual(0, _view.HouseView.FaceUpCount);
            Assert.IsFalse(_view.HouseView.IsEmpty);
            Assert.IsTrue(_view.BetControls.Visible);
            Assert.IsFalse(_view.BetControls.IsAnswer);
            Assert.IsTrue(_view.BetControls.CanPass);
            Assert.AreEqual("RAISE +100", _view.BetControls.RaiseLabel);
            Assert.IsNull(_view.ActionLabel);
            Assert.AreEqual(0, _view.Ante, "The ante label goes once the cards are out.");
            Assert.AreEqual(100, _view.Pot);
        }

        [Test]
        public void EachDecision_TurnsAnotherCard_AndRaiseGrowsPotAndOutlook()
        {
            Setup();
            _view.PressAction();

            _view.PressBet(BetAction.Raise);
            Assert.AreEqual(4, _view.PlayerView.FaceUpCount);
            Assert.AreEqual(200, _view.Pot);
            StringAssert.Contains("−200", _view.StakeInfo);

            _view.PressBet(BetAction.Pass);
            Assert.AreEqual(5, _view.PlayerView.FaceUpCount);
            Assert.AreEqual(200, _view.Pot);
        }

        [Test]
        public void StakeOnTable_ComesOffTheSentenceCounter()
        {
            Setup();
            _view.PressAction();
            Assert.AreEqual(900, _view.SentenceView.Years);

            _view.PressBet(BetAction.Raise);

            Assert.AreEqual(200, _view.Pot);
            Assert.AreEqual(800, _view.SentenceView.Years);
        }

        [Test]
        public void RaiseButton_ShowsTableFull_AtTheCap()
        {
            Setup();
            _view.PressAction();
            for (int i = 0; i < 3; i++) _view.PressBet(BetAction.Raise);
            _view.PressAction();
            Assert.AreEqual("RAISE +100", _view.BetControls.RaiseLabel, "The double raise is cut to the cap.");

            _view.PressBet(BetAction.Raise);

            Assert.AreEqual(GamePhase.HouseReveal, _game.Phase);
            Assert.AreEqual(500, _view.Pot);
            Assert.AreEqual("TABLE FULL", _view.BetControls.RaiseLabel);
            Assert.IsFalse(_view.BetControls.CanRaise);
        }

        [Test]
        public void TinySentence_IsAllIn()
        {
            Setup(startingYears: 6);
            _view.PressAction();

            Assert.AreEqual("ALL IN", _view.BetControls.RaiseLabel);
            Assert.IsFalse(_view.BetControls.CanRaise);
            Assert.AreEqual(0, _view.SentenceView.Years);
        }

        // ------------------------------------------------------------------ after the draw

        [Test]
        public void Draw_LeadsToOneDecision_WithDoubleRaise()
        {
            Setup();
            DealAndDraw();

            Assert.AreEqual(GamePhase.DrawReveal, _game.Phase);
            Assert.AreEqual(0, _view.HouseView.FaceUpCount);
            Assert.IsTrue(_view.BetControls.Visible);
            Assert.AreEqual("RAISE +200", _view.BetControls.RaiseLabel);
            Assert.IsFalse(_view.PlayerView.Interactable);
        }

        [Test]
        public void HouseShowsItsCards_ForTheLastDecision()
        {
            Setup(dealer: DealerRoster.Belial);
            DealAndDraw();

            _view.PressBet(BetAction.Pass);

            Assert.AreEqual(GamePhase.HouseReveal, _game.Phase);
            Assert.AreEqual(1, _view.HouseView.FaceUpCount, "Belial shows only one card.");
            Assert.IsTrue(_view.BetControls.Visible);
        }

        [Test]
        public void HouseReRaise_OffersCallOrFold_AndTheDealerSpeaks()
        {
            Setup(houseBetting: new HellPokerGameTests.FixedHouseBetting(true));
            DealAndDraw();
            int lines = _view.DealerView.LinesSaid;

            _view.PressBet(BetAction.Raise);

            Assert.AreEqual(GamePhase.HouseReRaise, _game.Phase);
            Assert.IsTrue(_view.BetControls.IsAnswer);
            Assert.AreEqual("CALL +100", _view.BetControls.CallLabel);
            Assert.AreEqual(Tone.Warning, _view.MessageTone);
            Assert.AreEqual(lines + 1, _view.DealerView.LinesSaid);
            Assert.AreEqual(Tone.Bad, _view.DealerView.LastTone);
        }

        [Test]
        public void SpaceKey_CallsTheReRaise()
        {
            Setup(houseBetting: new HellPokerGameTests.FixedHouseBetting(true));
            DealAndDraw();
            _view.PressBet(BetAction.Raise);

            _presenter.PerformAction();

            Assert.AreEqual(GamePhase.HouseReveal, _game.Phase);
            Assert.AreEqual(400, _view.Pot);
            Assert.IsFalse(_view.BetControls.IsAnswer);
        }

        [Test]
        public void PassIsRefused_AgainstAReRaise()
        {
            Setup(houseBetting: new HellPokerGameTests.FixedHouseBetting(true));
            DealAndDraw();
            _view.PressBet(BetAction.Raise);

            _view.PressBet(BetAction.Pass);

            Assert.AreEqual(GamePhase.HouseReRaise, _game.Phase);
            Assert.AreEqual(Tone.Warning, _view.MessageTone);
        }

        // ------------------------------------------------------------------ runs and dealers

        [Test]
        public void StartNewRun_ResetsTheTable()
        {
            Setup();
            _view.PressAction();
            _view.PressBet(BetAction.Fold);
            Assert.IsTrue(_presenter.CanContinue);

            _presenter.StartNewRun(DealerRoster.Belial);

            Assert.AreSame(_game, _presenter.Game, "A new run plays a new game.");
            Assert.AreEqual(DealerRoster.BelialId, _lastDealer.Id);
            Assert.AreEqual(GamePhase.Betting, _game.Phase);
            Assert.AreEqual(1000, _view.SentenceView.Years);
            Assert.IsFalse(_presenter.CanContinue);
        }

        [Test]
        public void BeforeAnyRun_InputIsIgnored()
        {
            _view = new FakeTableView();
            _presenter = new TablePresenter(d => throw new AssertionException("No game should be built yet."), _view);

            _presenter.PerformAction();
            _presenter.Bet(BetAction.Raise);
            _presenter.ToggleDiscard(0);
            _view.PressAction();

            Assert.IsNull(_presenter.Game);
            Assert.IsFalse(_presenter.CanContinue);
        }

        [Test]
        public void StartNewRun_IntroducesTheDealer_AndTheirPayouts()
        {
            Setup(dealer: DealerRoster.Belial);

            Assert.AreEqual(DealerRoster.BelialId, _view.DealerView.Dealer.Id);
            Assert.AreEqual("BELIAL", _view.DealerView.Dealer.Name);
            Assert.IsNotEmpty(_view.DealerView.LastLine, "The dealer greets the player.");
            Assert.AreEqual(30, _view.PayoutsView.Table.GetMultiplier(HandCategory.RoyalFlush));
        }

        [Test]
        public void DealerGloats_WhenTheHouseWins()
        {
            // Player: nothing. House: a pair of kings that draws into two pair.
            Setup(player: "2C 5D 9H JS 3C", house: "KD KH 4S 7H 8D", rest: "6S 4C 6D AH 2H 3D");
            string greeting = _view.DealerView.LastLine;
            DealAndDraw();
            PassUntil(GamePhase.RoundOver);

            Assert.AreEqual(ShowdownOutcome.HouseWins, _game.LastRound.Showdown.Outcome);
            Assert.AreEqual(1000 + 100 * 2, _game.Years, "Two pair ×2.");
            Assert.AreNotEqual(greeting, _view.DealerView.LastLine);
            Assert.AreEqual(Tone.Bad, _view.DealerView.LastTone);
        }

        [Test]
        public void DealerIsAnnoyed_WhenThePlayerWins()
        {
            Setup();
            DealAndDraw();
            PassUntil(GamePhase.RoundOver);

            Assert.AreEqual(ShowdownOutcome.PlayerWins, _game.LastRound.Showdown.Outcome);
            Assert.AreEqual(Tone.Good, _view.DealerView.LastTone);
        }

        [Test]
        public void DealerRemarks_OnceTheGatesAreInSight()
        {
            Setup(startingYears: 200);

            Assert.IsTrue(_view.FinalStretch);
            Assert.AreEqual(Tone.Warning, _view.DealerView.LastTone);
            int lines = _view.DealerView.LinesSaid;

            _view.PressAction();
            _view.PressBet(BetAction.Raise);

            Assert.AreEqual(lines, _view.DealerView.LinesSaid, "The remark is made only once.");
        }

        // ------------------------------------------------------------------ the rest of a hand

        [Test]
        public void SpaceKeyPasses_DuringDecision()
        {
            Setup();
            _view.PressAction();

            _presenter.PerformAction();

            Assert.AreEqual(4, _game.PlayerCardsRevealed);
        }

        [Test]
        public void AfterFifthCard_PlayerPicksDiscards()
        {
            Setup();
            _view.PressAction();
            PassUntil(GamePhase.Drawing);

            Assert.IsFalse(_view.BetControls.Visible);
            Assert.IsTrue(_view.PlayerView.Interactable);
            Assert.AreEqual("STAND PAT", _view.ActionLabel);

            _view.PlayerView.Click(1);
            _view.PlayerView.Click(3);
            _view.PlayerView.Click(1);
            CollectionAssert.AreEquivalent(new[] { 3 }, _view.PlayerView.Selection);
            Assert.AreEqual("DRAW 1", _view.ActionLabel);
        }

        [Test]
        public void DiscardingOverLimit_IsRefusedWithWarning()
        {
            Setup();
            _view.PressAction();
            PassUntil(GamePhase.Drawing);

            for (int i = 0; i < 4; i++) _view.PlayerView.Click(i);

            Assert.AreEqual(3, _presenter.SelectedDiscards.Count);
            Assert.AreEqual(Tone.Warning, _view.MessageTone);
        }

        [Test]
        public void WinningShowdown_RevealsEverything_AndReportsForgiveness()
        {
            Setup();
            DealAndDraw();

            PassUntil(GamePhase.RoundOver);

            Assert.AreEqual(5, _view.HouseView.FaceUpCount);
            Assert.AreEqual(Tone.Good, _view.MessageTone);
            Assert.AreEqual(HandCategory.Flush, _view.PayoutsView.Highlighted);
            Assert.AreEqual(500, _view.SentenceView.Years, "Flush ×5 on 100.");
            Assert.IsNull(_view.StakeInfo);
            Assert.AreEqual("NEXT HAND", _view.ActionLabel);
        }

        [Test]
        public void Fold_EndsHand_WithPenalty()
        {
            Setup();
            _view.PressAction();

            _view.PressBet(BetAction.Fold);

            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            Assert.AreEqual(Tone.Bad, _view.MessageTone);
            Assert.AreEqual("FOLDED", _view.PlayerView.Caption);
            Assert.AreEqual(5, _view.HouseView.FaceUpCount);
            Assert.AreEqual(1050, _view.SentenceView.Years);
        }

        [Test]
        public void NextHand_ReturnsToBetting()
        {
            Setup();
            _view.PressAction();
            _view.PressBet(BetAction.Fold);

            _view.PressAction();

            Assert.AreEqual(GamePhase.Betting, _game.Phase);
            Assert.IsTrue(_view.PlayerView.IsEmpty);
            Assert.AreEqual(0, _view.Pot);
            Assert.AreEqual(100, _view.Ante);
            Assert.IsNull(_view.PayoutsView.Highlighted);
        }

        [Test]
        public void FinalStretch_ForbidsPass_AndTurnsOnHellfire()
        {
            Setup(startingYears: 200);
            Assert.IsTrue(_view.FinalStretch);

            _view.PressAction();
            Assert.IsFalse(_view.BetControls.CanPass);

            _view.PressBet(BetAction.Pass);
            Assert.AreEqual(3, _game.PlayerCardsRevealed);
            Assert.AreEqual(Tone.Warning, _view.MessageTone);

            _presenter.PerformAction();
            Assert.AreEqual(3, _game.PlayerCardsRevealed);

            _view.PressBet(BetAction.Raise);
            Assert.AreEqual(4, _game.PlayerCardsRevealed);
        }

        [Test]
        public void InputIsIgnored_WhileViewIsAnimating()
        {
            Setup();
            _view.IsBusy = true;

            _view.PressAction();

            Assert.AreEqual(GamePhase.Betting, _game.Phase);
        }

        [Test]
        public void DeadMansHand_ShowsTriumph()
        {
            Setup(player: "AS AC 8S 8C 3H", house: "KS KH 4D 4C 2H", rest: "QD");
            DealAndDraw();

            PassUntil(GamePhase.Absolved);

            Assert.AreEqual(Tone.Triumph, _view.MessageTone);
            Assert.AreEqual(0, _view.SentenceView.Years);
            Assert.AreEqual("PLAY AGAIN", _view.ActionLabel);
        }
    }
}
