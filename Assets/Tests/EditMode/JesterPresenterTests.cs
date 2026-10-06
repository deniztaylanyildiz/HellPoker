using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The Jester at the table: his badge counts the jokers (K only explains), two jokers in hand bring a warning that stays, the
    /// showdown opens the joker picker on the best card (arrows step it, NAME IT / Enter settles it) and the named card shows with
    /// a fool's cap; the demon's two jokers get their laugh; the run log tells the jokers' story.
    /// </summary>
    public class JesterPresenterTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string Blanks = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S 10D 3D";

        private FakeTableView _view;
        private TablePresenter _presenter;
        private HellPokerGame _game;
        private MemoryRunLogSink _logs;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        /// <summary>A Jester's run (2 jokers, or <paramref name="jokers"/>), dealt from these cards (holding that many jokers).</summary>
        private void Run(string player, string house, string blanks = Blanks, int jokers = 2)
        {
            _view = new FakeTableView();
            _logs = new MemoryRunLogSink();
            _presenter = new TablePresenter((d, sinner) =>
            {
                var rules = d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0));
                return _game = new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {blanks}"), HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), d.Payouts, null, null, null, sinner);
            }, _view, runLog: _logs);
            _presenter.Resume(DealerRoster.Mammon,
                new RunSnapshot("mammon", 1000, 3, new RunStats(1000, "mammon"), classId: Jester.ClassId, classJokers: jokers));
        }

        private void ToShowdown()
        {
            _view.PressAction();                                    // deal
            if (_game.Phase == GamePhase.PlayerReveal) _view.PressCheckToDraw();
            _view.PressAction();                                    // stand
            for (int guard = 0; guard < 10 && (_game.Phase == GamePhase.DrawReveal || _game.Phase == GamePhase.HouseReveal); guard++)
                _view.PressBet(BetAction.Pass);
        }

        [Test]
        public void TheBadge_CountsTheJokers_AndK_OnlyExplains()
        {
            Run(Nothing, "2D 2H 5S 7H 9D", Blanks + " JK1 JK2");
            Assert.AreEqual(2, _view.Sinner.Jokers);
            Assert.IsTrue(_view.Sinner.ShowsJokers);
            _presenter.UsePower();
            Assert.AreEqual(UiText.JesterPowerInfo, _view.Message);
            Assert.IsFalse(_game.PowerArmed);
            Assert.AreEqual(0, _presenter.Sinner.Charge);
        }

        [Test]
        public void TwoJokersInHand_AWarningStays_UntilOneIsThrownBack()
        {
            Run("JK1 JK2 AS AH 9D", Nothing);
            _view.PressAction();   // deal: the two jokers are the opening cards
            Assert.AreEqual(UiText.TwoJokersWarning, _view.Power.Hint);
            Assert.IsTrue(_view.Power.Warning);
            _view.PressBet(BetAction.Pass);
            Assert.AreEqual(UiText.TwoJokersWarning, _view.Power.Hint, "It stays through the bets.");
            _view.PressCheckToDraw();
            _view.PlayerView.Click(1);
            _view.PressAction();   // the extra joker goes back
            Assert.AreNotEqual(UiText.TwoJokersWarning, _view.Power.Hint);
            StringAssert.Contains(string.Format(UiText.HandNowJokerFormat, ""), _view.PlayerView.Caption);
        }

        [Test]
        public void TheShowdown_OpensThePicker_OnTheBestCard_ArrowsStepIt_ConfirmShowsTheNamedCard()
        {
            Run("AS AH 5C 9D JK1", "KS KH KD 2C 4D", Blanks + " JK2");
            ToShowdown();
            Assert.AreEqual(GamePhase.NamingJoker, _game.Phase);
            Assert.IsNotNull(_view.JokerPicker);
            Assert.AreEqual(Rank.Ace, _view.JokerPicker.Card.Rank);
            StringAssert.Contains(UiText.CategoryNameUpper(HandCategory.ThreeOfAKind), _view.Power.Hint);
            StringAssert.Contains(UiText.JokerBestTag, _view.Power.Hint);

            _presenter.StepJoker(1, 0);   // past the ace: the two of the same suit
            Card picked = _view.JokerPicker.Card;
            Assert.AreEqual(Rank.Two, picked.Rank);
            StringAssert.DoesNotContain(UiText.JokerBestTag, _view.Power.Hint);
            _presenter.StepJoker(-1, 0);
            Assert.AreEqual(Rank.Ace, _view.JokerPicker.Card.Rank);
            _presenter.StepJoker(0, 1);
            Assert.AreEqual(Rank.Ace, _view.JokerPicker.Card.Rank, "The suits step past the aces it holds.");
            Assert.IsFalse(new[] { Suit.Spades, Suit.Hearts }.Contains(_view.JokerPicker.Card.Suit));
            Card named = _view.JokerPicker.Card;

            _view.PressAction();   // Enter / Space: NAME IT
            Assert.IsNull(_view.JokerPicker);
            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            CardSlot slot = _view.PlayerView.Slots[4];
            Assert.AreEqual(named, slot.Card);
            Assert.AreEqual(CardMark.Joker, slot.Mark);
            Assert.AreEqual(3, _presenter.Sinner.Jokers, "A won hand: one more joker.");
            Assert.AreEqual(3, _view.Sinner.Jokers);
        }

        [Test]
        public void ThePickersButtons_DoWhatTheKeysDo()
        {
            Run("AS AH 5C 9D JK1", "KS KH KD 2C 4D", Blanks + " JK2");
            ToShowdown();
            _view.PressJokerStep(1, 0);
            Assert.AreEqual(Rank.Two, _view.JokerPicker.Card.Rank);
            _view.PressJokerConfirm();
            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            Assert.AreEqual(HandCategory.OnePair, _game.LastRound.Showdown.Player.Category, "The worse card counts as it is.");
        }

        [Test]
        public void TheDemonsTwoJokers_TheFoolsLaugh_AndHisOwnLine()
        {
            Run(Nothing, "JK1 JK2 AS AH AD", "JK3 " + Blanks.Replace("3S ", ""), jokers: 3);
            ToShowdown();
            Assert.IsTrue(_game.LastRound.Showdown.HouseBust);
            Assert.IsTrue(_view.Moments.Any(m => m.moment == TableMoment.JokerLaugh && m.text == UiText.JokerLaughFlash));
            (string line, DealerMood mood) said = _view.DealerView.Said.Last();
            CollectionAssert.Contains(UiText.Dealer("mammon").JokerBust, said.line);
            Assert.AreEqual(DealerMood.Annoyed, said.mood);
            StringAssert.Contains(UiText.TwoJokersCaption, _view.HouseView.Caption);
            Assert.AreEqual(string.Format(UiText.HouseBustFormat, -_game.LastRound.YearsChange), _view.Message.Split('\n')[0]);
        }

        [Test]
        public void TwentyJokers_TheLaugh_TheDemonsSurprise_AndTheRattleApartOnTheBar()
        {
            string jokers = string.Join(" ", Enumerable.Range(1, 19).Select(n => "JK" + n));
            Run("AS AH AD KC KD", Nothing, Blanks + " " + jokers, jokers: 19);
            ToShowdown();
            Assert.IsTrue(_game.LastRound.JokerJackpot);
            Assert.IsTrue(_view.Moments.Any(m => m.moment == TableMoment.JokerLaugh && m.text == UiText.JokerJackpotFlash));
            Assert.IsTrue(_view.DealerView.Said.Any(s => UiText.Dealer("mammon").JokerJackpot.Contains(s.line) && s.mood == DealerMood.Annoyed));
            StringAssert.Contains(UiText.RelicName(HellPoker.Core.Relics.RelicIds.JestersRattle), _view.Message);
            RelicBadge rattle = _view.Relics.Single();
            Assert.AreEqual(HellPoker.Core.Relics.RelicIds.JestersRattle, rattle.Id);
            Assert.IsTrue(rattle.IsReward);
            Assert.AreEqual(2, _view.Sinner.Jokers);
            StringAssert.Contains("TWENTY JOKERS", _presenter.Log.ToText());
        }

        [Test]
        public void TheDemonsTwoJokers_NoPicker_TheJokerShowsItsBestCard_TheLaugh()
        {
            Run("AS AH 5C 9D JK1", "JK2 JK3 KS KH KD", "JK4 " + Blanks.Replace("3S ", ""), jokers: 4);
            ToShowdown();
            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            Assert.IsNull(_view.JokerPicker, "The outcome did not hang on the joker: no picker.");
            Assert.AreEqual(CardMark.Joker, _view.PlayerView.Slots[4].Mark);
            Assert.AreEqual(new Card(Rank.Ace, Suit.Diamonds), _view.PlayerView.Slots[4].Card);
            Assert.IsTrue(_view.Moments.Any(m => m.moment == TableMoment.JokerLaugh));
            StringAssert.StartsWith(string.Format(UiText.HouseBustFormat, -_game.LastRound.YearsChange), _view.Message);
            StringAssert.Contains("at once", _presenter.Log.ToText());
        }

        [Test]
        public void JokersOnBothSides_AsManyEach_APush_SaidAndLogged()
        {
            Run("JK1 JK2 AS AH AD", "JK3 JK4 2C 4D 5H", "JK5 " + Blanks.Replace("3S ", ""), jokers: 5);
            ToShowdown();
            Assert.AreEqual(ShowdownOutcome.Push, _game.LastRound.Showdown.Outcome);
            StringAssert.StartsWith(UiText.JokerDuelPush, _view.Message);
            Assert.IsFalse(_view.Moments.Any(m => m.moment == TableMoment.JokerLaugh), "Nobody's jokers sank anybody.");
            StringAssert.Contains("JOKERS ON BOTH SIDES: player 2 vs house 2: Push", _presenter.Log.ToText());
            Assert.AreEqual("Eşit joker: el berabere, yıl değişmez.", WithTurkish(() => UiText.JokerDuelPush));
            Assert.AreEqual("İkinizde de joker fazla: daha az jokeri olan sen kazandın.", WithTurkish(() => UiText.JokerDuelWin));
        }

        private static string WithTurkish(System.Func<string> words)
        {
            Lang.Set(Language.Turkish);
            try { return words(); }
            finally { Lang.Set(Language.English); }
        }

        [Test]
        public void TheRunLog_TellsTheJokersStory()
        {
            Run("AS AH 5C 9D JK1", "KS KH KD 2C 4D", Blanks + " JK2");
            ToShowdown();
            _view.PressAction();
            string log = _presenter.Log.ToText();
            StringAssert.Contains("joker dealt to the player x1", log);
            StringAssert.Contains("joker named:", log);
            StringAssert.Contains("jokers in the deck: 2 -> 3", log);
        }
    }
}
