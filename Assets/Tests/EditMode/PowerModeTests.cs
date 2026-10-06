using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// A power switched on stays on — through bets, turned cards and the draw prompt — until it is used, switched off (K again),
    /// or its moment goes (the King's protection past the draw: it closes unspent, and the player hears it). The Peasant's power
    /// waits for his fold (FREE FOLD), across hands. The Bone Die's pick stays open the same way. And the screen shows it all.
    /// </summary>
    public class PowerModeTests
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

        private FakeTableView _view;
        private TablePresenter _presenter;
        private HellPokerGame _game;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        /// <summary>A run of this class picked up with a full gauge (or <paramref name="charge"/>).</summary>
        private void Run(SinnerClass sinnerClass, string player, string house, ICheat cheat = null, int years = 1000, int charge = 5)
        {
            _view = new FakeTableView();
            _presenter = new TablePresenter((d, sinner) =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(cheat == null ? null : new OnlyCheat(cheat), 1, random, sinner), random, sinner);
            }, _view);
            _presenter.Resume(DealerRoster.Mammon,
                new RunSnapshot("mammon", years, 3, new RunStats(years, "mammon"), classId: sinnerClass.Id, classCharge: charge));
        }

        private Sinner Sinner => _presenter.Sinner;

        private void PlayToTheEnd()
        {
            for (int guard = 0; guard < 20 && _game.Phase != GamePhase.RoundOver && !_game.IsGameOver; guard++)
            {
                if (_game.Phase == GamePhase.Drawing) _view.PressAction();
                else if (_game.Phase == GamePhase.HouseReRaise) _view.PressBet(BetAction.Call);
                else _view.PressBet(BetAction.Pass);
            }
        }

        // ------------------------------------------------------------------ the King

        [Test]
        public void TheKing_K_GuardsTheHandAtOnce_TheCrownsStayThroughBetsAndTheDraw()
        {
            Run(SinnerRoster.King, KingsPair, HouseTwos, new BurningCardCheat(), years: 1250);
            _view.PressAction();   // deal: the cheat is announced
            _presenter.UsePower();
            Assert.IsFalse(_game.PowerArmed, "No switched-on mode for the King any more.");
            Assert.AreEqual(0, Sinner.Charge, "Spent at once.");
            Assert.IsTrue(Sinner.HandProtected);
            Assert.IsNull(_view.PlayerView.Picking, "No card to pick.");

            _view.PressBet(BetAction.Pass);   // a card turns: it is guarded too
            for (int i = 0; i < _game.PlayerCardsRevealed; i++)
                Assert.AreEqual(CardMark.Protected, _view.PlayerView.Slots[i].Mark, "card " + i);
            Assert.IsTrue(_view.Sinner.PowerOn);
        }

        [Test]
        public void TheKing_TheCrownEndsWithTheHand()
        {
            Run(SinnerRoster.King, KingsPair, HouseTwos, new BurningCardCheat(), years: 1250);
            _view.PressAction();
            _presenter.UsePower();
            PlayToTheEnd();
            Assert.IsFalse(Sinner.HandProtected);
            Assert.IsFalse(_view.Sinner.PowerOn);
        }

        [Test]
        public void TheKing_K_WithoutACheat_SaysWhy_AndSpendsNothing()
        {
            Run(SinnerRoster.King, KingsPair, HouseTwos, years: 1250);
            _view.PressAction();
            _presenter.UsePower();
            Assert.AreEqual(UiText.PowerNoCheatToGuard, _view.Message);
            Assert.AreEqual(5, Sinner.Charge);
            Assert.IsFalse(Sinner.HandProtected);
        }
        // ------------------------------------------------------------------ the Peasant

        [Test]
        public void ThePeasant_K_SwitchesOnUnspent_FreeFold_ThenTheFoldCostsNothing()
        {
            Run(SinnerRoster.Peasant, Nothing, HouseFullHouse);
            _view.PressAction();
            _presenter.UsePower();

            Assert.IsTrue(_game.PowerArmed);
            Assert.AreEqual(5, Sinner.Charge, "Not spent yet.");
            Assert.AreEqual("FREE\nFOLD", _view.BetControls.FoldLabel);
            Assert.AreEqual(UiText.FreeFoldHint, _view.Power.Hint);

            _view.PressBet(BetAction.Fold);

            Assert.AreEqual(1000, _game.Years, "Nothing added.");
            Assert.IsTrue(_game.LastRound.FreeFold);
            Assert.AreEqual(0, Sinner.Charge, "Spent at the fold (that fold charges nothing).");
            Assert.IsFalse(_game.PowerArmed);
            StringAssert.Contains("honest heart", _view.Message);
        }

        [Test]
        public void ThePeasantsPower_WaitsThroughAPlayedHand_IntoTheNext()
        {
            Run(SinnerRoster.Peasant, Nothing, HouseFullHouse);
            _view.PressAction();
            _presenter.UsePower();
            PlayToTheEnd();   // played out (and lost), not folded

            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            Assert.IsTrue(_game.PowerArmed, "Still on after the hand.");
            _view.PressAction();   // next hand
            _view.PressAction();   // its deal
            Assert.IsTrue(_game.PowerArmed);
            Assert.AreEqual("FREE\nFOLD", _view.BetControls.FoldLabel);

            _view.PressBet(BetAction.Fold);
            Assert.IsTrue(_game.LastRound.FreeFold);
        }

        [Test]
        public void ThePeasant_KTwice_SwitchesItOff_AndAFoldCostsAgain()
        {
            Run(SinnerRoster.Peasant, Nothing, HouseFullHouse);
            _view.PressAction();
            _presenter.UsePower();
            _presenter.UsePower();
            Assert.IsFalse(_game.PowerArmed);
            Assert.IsNull(_view.BetControls.FoldLabel, "FOLD again.");

            _view.PressBet(BetAction.Fold);

            Assert.AreEqual(1050, _game.Years, "The usual fold.");
            Assert.IsFalse(_game.LastRound.FreeFold);
            Assert.AreEqual(5, Sinner.Charge);
        }

        [Test]
        public void ThePeasantsFreeFold_AnswersAReRaise_Too()
        {
            var sinner = new Sinner(new Peasant(), charge: 5);
            var random = new FirstChoice();
            var game = new HellPokerGame(new GameRules(1000, 5000, luciferGateYears: 0), TestDecks.Stacked($"{Nothing} {HouseFullHouse} {Blanks}"),
                HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()), new HouseDrawStrategy(), PayoutTable.CreateDefault(),
                new HellPokerGameTests.FixedHouseBetting(true), new CheatSession(null, 0, random), random, sinner);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            game.Bet(BetAction.Raise);
            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase);
            Assert.IsTrue(game.ArmPower());

            game.Bet(BetAction.Fold);

            Assert.AreEqual(1000, game.Years);
            Assert.IsTrue(game.LastRound.FreeFold);
        }

        // ------------------------------------------------------------------ the Warlock

        [Test]
        public void TheWarlocksWard_StaysUpThroughMoves_UntilTheCheatIsWarded()
        {
            Run(SinnerRoster.Warlock, Nothing, HouseFullHouse, new CollateralCheat());
            _view.PressAction();   // the cheat is announced
            _presenter.UsePower();
            Assert.IsTrue(_view.Power.WardUp, "The shield by the intent sign.");
            Assert.IsTrue(_view.Sinner.PowerOn);

            _view.PressBet(BetAction.Pass);
            Assert.IsTrue(_view.Power.WardUp, "Still up after a bet.");
            _presenter.CheckToDraw();   // the chain comes as the draw opens

            Assert.AreEqual(CheatOutcome.Blocked, _game.CheatsThisHand.Single().Outcome);
            Assert.IsFalse(_view.Power.WardUp, "Warded: the shield goes.");
            Assert.IsFalse(_view.Sinner.PowerOn);
        }

        [Test]
        public void AWardWhoseCheatNeverCame_StaysUp_ItsChargeSpent()
        {
            // CLAUDE.md: the gauge is spent when the ward goes up; a cheat that never strikes leaves the ward waiting for the next.
            Run(SinnerRoster.Warlock, Nothing, HouseFullHouse, new CollateralCheat());
            _view.PressAction();
            _presenter.UsePower();
            _view.PressBet(BetAction.Fold);   // before the draw: the chain never came

            Assert.IsTrue(Sinner.WardRaised, "Still up for the next minor cheat.");
            Assert.AreEqual(1, Sinner.Charge, "Spent when raised (no refund); the fold pays its one.");
            Assert.IsTrue(_view.Power.WardUp);

            _view.PressAction();   // next hand
            _view.PressAction();   // the deal: the chain is announced again
            _presenter.CheckToDraw();
            Assert.AreEqual(CheatOutcome.Blocked, _game.CheatsThisHand.Single().Outcome, "The waiting ward took it.");
        }

        // ------------------------------------------------------------------ the Bone Die

        [Test]
        public void TheBoneDie_PressThePass_StillPicking_ThenACard_Redrawn()
        {
            Run(SinnerRoster.Peasant, Nothing, HouseFullHouse, charge: 0);
            _game.Effects.AddRelic(RelicIds.BoneDie);
            _view.PressAction();
            _view.PressRelic(RelicIds.BoneDie);
            Assert.IsTrue(_view.Relics[0].Selecting);

            _view.PressBet(BetAction.Pass);

            Assert.IsTrue(_view.Relics[0].Selecting, "A bet does not put it away.");
            Assert.AreEqual(UiText.RedrawPrompt, _view.Power.Hint);
            CollectionAssert.Contains(_view.PlayerView.Picking, 0);

            _view.PlayerView.Click(0);

            Assert.AreEqual(TestCards.Card("3S"), _game.PlayerHand[0], "Redrawn.");
            Assert.IsFalse(_view.Relics[0].Selecting);
            Assert.IsNull(_view.PlayerView.Picking);
        }

        [Test]
        public void TheBoneDiesPick_ClosesWhenTheDrawPasses()
        {
            Run(SinnerRoster.Peasant, Nothing, HouseFullHouse, charge: 0);
            _game.Effects.AddRelic(RelicIds.BoneDie);
            _view.PressAction();
            _view.PressRelic(RelicIds.BoneDie);
            _presenter.CheckToDraw();
            Assert.IsTrue(_view.Relics[0].Selecting, "At the draw too.");

            _view.PressAction();   // stand pat

            Assert.IsFalse(_view.Relics[0].Selecting);
            Assert.AreEqual(1, _game.RedrawsLeft, "Not rolled.");
        }
    }
}
