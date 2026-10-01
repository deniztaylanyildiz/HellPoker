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
        private static readonly int[] Stakes = { 10, 25, 50, 100, 200 };

        private FakeTableView _view;
        private HellPokerGame _game;
        private TablePresenter _presenter;

        /// <summary>Default: player gets a flush, the house a pair of twos that draws three blanks.</summary>
        private void Setup(string player = "2C 9C JC 4C KC", string house = "2D 2H 5S 7H 9D", string rest = "3S 4S 6D AH",
            int startingYears = 1000)
        {
            _view = new FakeTableView();
            _game = new HellPokerGame(
                new GameRules(startingYears, 2000, 10, 200),
                TestDecks.Stacked($"{player} {house} {rest}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                PayoutTable.CreateDefault());
            _presenter = new TablePresenter(_game, _view, Stakes);
        }

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private void PassUntil(GamePhase phase)
        {
            while (_game.Phase != phase)
                _view.PressBet(BetAction.Pass);
        }

        [Test]
        public void Start_ShowsBettingTable()
        {
            Setup();

            Assert.AreEqual(50, _view.StakesView.Selected);
            Assert.IsTrue(_view.StakesView.Visible);
            Assert.IsFalse(_view.BetControls.Visible);
            Assert.IsTrue(_view.PlayerView.IsEmpty);
            Assert.AreEqual("DEAL", _view.ActionLabel);
            Assert.AreEqual(1000, _view.SentenceView.Years);
            Assert.IsFalse(_view.FinalStretch);
        }

        [Test]
        public void Deal_TurnsFirstCard_AndOffersBetDecision()
        {
            Setup();
            _view.StakesView.Choose(100);

            _view.PressAction();

            Assert.AreEqual(GamePhase.PlayerReveal, _game.Phase);
            Assert.AreEqual(1, _view.PlayerView.FaceUpCount);
            Assert.AreEqual(0, _view.HouseView.FaceUpCount);
            Assert.IsFalse(_view.HouseView.IsEmpty);
            Assert.IsTrue(_view.BetControls.Visible);
            Assert.IsTrue(_view.BetControls.CanPass);
            Assert.AreEqual("RAISE +100", _view.BetControls.RaiseLabel);
            Assert.IsNull(_view.ActionLabel);
            Assert.IsFalse(_view.StakesView.Visible);
            Assert.AreEqual(100, _view.Pot);
        }

        [Test]
        public void EachDecision_TurnsAnotherCard_AndRaiseGrowsPot()
        {
            Setup();
            _view.PressAction();

            _view.PressBet(BetAction.Raise);
            Assert.AreEqual(2, _view.PlayerView.FaceUpCount);
            Assert.AreEqual(100, _view.Pot);

            _view.PressBet(BetAction.Pass);
            Assert.AreEqual(3, _view.PlayerView.FaceUpCount);
            Assert.AreEqual(100, _view.Pot);
        }

        [Test]
        public void StakeOnTable_ComesOffTheSentenceCounter()
        {
            Setup();
            _view.StakesView.Choose(200);
            _view.PressAction();
            Assert.AreEqual(800, _view.SentenceView.Years);

            _view.PressBet(BetAction.Raise);

            Assert.AreEqual(400, _view.Pot);
            Assert.AreEqual(600, _view.SentenceView.Years);
        }

        [Test]
        public void RaiseButton_BecomesAllIn_ThenLocks()
        {
            Setup(startingYears: 130);
            _view.StakesView.Choose(50);
            _view.PressAction();
            _view.PressBet(BetAction.Raise);

            Assert.AreEqual("ALL IN +30", _view.BetControls.RaiseLabel);
            _view.PressBet(BetAction.Raise);

            Assert.AreEqual("ALL IN", _view.BetControls.RaiseLabel);
            Assert.IsFalse(_view.BetControls.CanRaise);
            Assert.AreEqual(0, _view.SentenceView.Years);
        }

        [Test]
        public void StakesAboveTheSentence_AreNotOffered()
        {
            Setup(startingYears: 80);

            CollectionAssert.AreEquivalent(new[] { 10, 25, 50 }, _view.StakesView.Available);
            _view.StakesView.Choose(100);
            Assert.AreEqual(50, _presenter.SelectedStake);

            _presenter.StepStake(+1);
            Assert.AreEqual(50, _presenter.SelectedStake);
        }

        [Test]
        public void StartNewRun_ResetsTheTable()
        {
            Setup();
            _view.PressAction();
            _view.PressBet(BetAction.Fold);
            Assert.IsTrue(_presenter.CanContinue);

            _presenter.StartNewRun();

            Assert.AreEqual(GamePhase.Betting, _game.Phase);
            Assert.AreEqual(1000, _view.SentenceView.Years);
            Assert.IsFalse(_presenter.CanContinue);
        }

        [Test]
        public void SpaceKeyPasses_DuringDecision()
        {
            Setup();
            _view.PressAction();

            _presenter.PerformAction();

            Assert.AreEqual(2, _game.PlayerCardsRevealed);
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
        public void Draw_StartsHouseReveal_WithBetDecision()
        {
            Setup();
            _view.PressAction();
            PassUntil(GamePhase.Drawing);

            _view.PressAction();

            Assert.AreEqual(GamePhase.HouseReveal, _game.Phase);
            Assert.AreEqual(1, _view.HouseView.FaceUpCount);
            Assert.IsTrue(_view.BetControls.Visible);
            Assert.IsFalse(_view.PlayerView.Interactable);
        }

        [Test]
        public void WinningShowdown_RevealsEverything_AndReportsForgiveness()
        {
            Setup();
            _view.StakesView.Choose(100);
            _view.PressAction();
            PassUntil(GamePhase.Drawing);
            _view.PressAction();

            PassUntil(GamePhase.RoundOver);

            Assert.AreEqual(5, _view.HouseView.FaceUpCount);
            Assert.AreEqual(Tone.Good, _view.MessageTone);
            Assert.AreEqual(HandCategory.Flush, _view.PayoutsView.Highlighted);
            Assert.AreEqual(500, _view.SentenceView.Years);
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
            Assert.AreEqual(1025, _view.SentenceView.Years);
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
            Assert.AreEqual(1, _game.PlayerCardsRevealed);
            Assert.AreEqual(Tone.Warning, _view.MessageTone);

            _presenter.PerformAction();
            Assert.AreEqual(1, _game.PlayerCardsRevealed);

            _view.PressBet(BetAction.Raise);
            Assert.AreEqual(2, _game.PlayerCardsRevealed);
        }

        [Test]
        public void InputIsIgnored_WhileViewIsAnimating()
        {
            Setup();
            _view.IsBusy = true;

            _view.PressAction();
            _view.StakesView.Choose(200);

            Assert.AreEqual(GamePhase.Betting, _game.Phase);
            Assert.AreEqual(50, _presenter.SelectedStake);
        }

        [Test]
        public void DeadMansHand_ShowsTriumph()
        {
            Setup(player: "AS AC 8S 8C 3H", house: "KS KH 4D 4C 2H", rest: "QD");
            _view.PressAction();
            PassUntil(GamePhase.Drawing);
            _view.PressAction();

            PassUntil(GamePhase.Absolved);

            Assert.AreEqual(Tone.Triumph, _view.MessageTone);
            Assert.AreEqual(0, _view.SentenceView.Years);
            Assert.AreEqual("PLAY AGAIN", _view.ActionLabel);
        }
    }
}
