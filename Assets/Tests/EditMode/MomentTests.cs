using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>The table's big moments: which hands earn a shake, a flare or the Dead Man's Hand scene.</summary>
    public class MomentTests
    {
        private const string Blanks = "3S 6C JD QC 10S";

        private FakeTableView _view;
        private HellPokerGame _game;
        private TablePresenter _presenter;

        private void PlayHand(string player, string house, bool raise = false)
        {
            _view = new FakeTableView();
            _presenter = new TablePresenter(d => _game = new HellPokerGame(
                d.ApplyTo(new GameRules(1000, 5000)),
                TestDecks.Stacked($"{player} {house} {Blanks}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                d.Payouts), _view);
            _presenter.StartNewRun(DealerRoster.Mammon);

            _view.PressAction();
            if (raise) _view.PressBet(BetAction.Raise);
            for (int guard = 0; guard < 10 && _game.Phase != GamePhase.Drawing; guard++) _view.PressBet(BetAction.Pass);
            _view.PressAction();
            for (int guard = 0; guard < 10 && _game.Phase != GamePhase.RoundOver && !_game.IsGameOver; guard++) _view.PressBet(BetAction.Pass);
        }

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        [Test]
        public void HeavyLoss_ShakesTheTable()
        {
            // A full house for the House: 100 + 100 × 7 = 800, eight units.
            PlayHand("2C 5D 7H 9S JC", "KS KH KD 4C 4H");

            Assert.That(_view.Moments.Select(m => m.moment), Has.Member(TableMoment.BigLoss));
        }

        [Test]
        public void SmallLoss_IsJustALoss()
        {
            // A pair of kings for the House: 100 years, one unit.
            PlayHand("2C 5D 7H 9S JC", "KS KH 4D 6C 8H");

            Assert.AreEqual(ShowdownOutcome.HouseWins, _game.LastRound.Showdown.Outcome);
            CollectionAssert.IsEmpty(_view.Moments);
        }

        [Test]
        public void WinningWithTwoPairOrBetter_FlaresTheHandsName()
        {
            PlayHand("KS KH 4D 4C 9H", "2D 2H 5S 7H 8D");

            var flare = _view.Moments.Single();
            Assert.AreEqual(TableMoment.GoodHand, flare.moment);
            Assert.AreEqual("TWO PAIR!", flare.text);
        }

        [Test]
        public void WinningWithOnePair_HasNoFlare()
        {
            PlayHand("KS KH 4D 7C 9H", "2D 2H 5S 6H 8D");

            Assert.AreEqual(ShowdownOutcome.PlayerWins, _game.LastRound.Showdown.Outcome);
            CollectionAssert.IsEmpty(_view.Moments);
        }

        [Test]
        public void DeadMansHand_GetsItsScene_WithItsFourCardsInOrder()
        {
            PlayHand("8C 3H AS 8S AC", "KS KH 4D 4C 9H");

            var scene = _view.Moments.Single();
            Assert.AreEqual(TableMoment.DeadMansHand, scene.moment);
            CollectionAssert.AreEqual(new[] { 2, 4, 3, 0 }, scene.cards, "A♠, A♣, 8♠, 8♣.");
        }
    }
}
