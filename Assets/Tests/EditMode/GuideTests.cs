using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Guidance for players new to poker: the hand's name as it stands, keep hints, the ranking panel, first-game tips.</summary>
    public class GuideTests
    {
        private sealed class FakeGuide : IGuideSettings
        {
            public bool HandGuide { get; set; } = true;
            public readonly HashSet<string> Seen = new HashSet<string>();
            public bool HasSeenTip(string tip) => Seen.Contains(tip);
            public void MarkTipSeen(string tip) => Seen.Add(tip);
        }

        // Player: kings and fours with a nine; house: a pair of twos that draws three blanks.
        private const string PlayerTwoPair = "KS KH 4D 4C 9H";
        private const string HousePair = "2D 2H 5S 7H 8D";
        private const string Blanks = "3S 6C JD QC 10S";

        private const string FirstDecisionTip = "tip.decision";   // UiText.TipFirstDecision (internal)

        private FakeTableView _view;
        private HellPokerGame _game;
        private TablePresenter _presenter;
        private FakeGuide _guide;

        private void Setup(string player = PlayerTwoPair, string house = HousePair, int startingYears = 1000,
            IHouseBettingStrategy houseBetting = null, int tableCapPercent = 30)
        {
            _view = new FakeTableView();
            _guide = new FakeGuide();
            _presenter = new TablePresenter(d => _game = new HellPokerGame(
                d.ApplyTo(new GameRules(startingYears, 5000, stakes: new StakeScale(tableCapPercent: tableCapPercent))),
                TestDecks.Stacked($"{player} {house} {Blanks}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                d.Payouts,
                houseBetting), _view, _guide);
            _presenter.StartNewRun(DealerRoster.Mammon);
        }

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private void PassUntil(GamePhase phase)
        {
            for (int guard = 0; guard < 20 && _game.Phase != phase; guard++)
                _view.PressBet(BetAction.Pass);
        }

        // ------------------------------------------------------------------ reading the visible hand (Core)

        [TestCase("", null)]
        [TestCase("2C 5D", HandCategory.HighCard)]
        [TestCase("KS KH", HandCategory.OnePair)]
        [TestCase("KS KH 4D 4C", HandCategory.TwoPair)]
        [TestCase("7S 7H 9C 7D", HandCategory.ThreeOfAKind)]
        [TestCase("KS KH KD KC", HandCategory.FourOfAKind)]
        [TestCase("2H 5H 9H JH KH", HandCategory.Flush)]
        [TestCase("AS AC 8S 8C 2D", HandCategory.DeadMansHand)]
        public void VisibleHand_IsNamedFromTheFaceUpCards(string cards, HandCategory? expected)
        {
            IReadOnlyList<Card> faceUp = cards.Length == 0 ? new List<Card>() : TestCards.Cards(cards).ToList();

            Assert.AreEqual(expected, VisibleHandReader.Read(faceUp, HandEvaluator.CreateDefault()));
        }

        [Test]
        public void Game_NamesOnlyTheCardsThatAreUp()
        {
            Setup();
            Assert.IsNull(_game.PlayerHandNow, "No cards yet.");

            _game.PlaceBet();   // three cards turn: KS KH 4D

            Assert.AreEqual(HandCategory.OnePair, _game.PlayerHandNow);
        }

        [Test]
        public void Game_SuggestsTheHousesOwnDiscards_OnlyWhileDrawing()
        {
            Setup(player: "KS KH 4D 7C 9H");
            _game.PlaceBet();
            CollectionAssert.IsEmpty(_game.SuggestedDiscards());

            PassUntil(GamePhase.Drawing);

            CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, _game.SuggestedDiscards(), "Keep the kings, throw the rest.");
        }

        // ------------------------------------------------------------------ the hand guide at the table

        [Test]
        public void Caption_NamesTheHandAsItStands()
        {
            Setup();

            _view.PressAction();
            Assert.AreEqual("NOW: ONE PAIR", _view.PlayerView.Caption);

            _view.PressBet(BetAction.Pass);   // the fourth card: another four
            Assert.AreEqual("NOW: TWO PAIR", _view.PlayerView.Caption);
        }

        [Test]
        public void Caption_IsPlain_WithTheGuideOff()
        {
            Setup();
            _guide.HandGuide = false;

            _view.PressAction();

            Assert.AreEqual("YOUR HAND", _view.PlayerView.Caption);
        }

        [Test]
        public void Drawing_HintsTheCardsToKeep_AndClearsAfter()
        {
            Setup(player: "KS KH 4D 7C 9H");
            _view.PressAction();
            PassUntil(GamePhase.Drawing);

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, _view.PlayerView.Hints);

            _view.PressAction();   // stand pat
            CollectionAssert.IsEmpty(_view.PlayerView.Hints);
        }

        [Test]
        public void Drawing_HasNoHints_WithTheGuideOff()
        {
            Setup(player: "KS KH 4D 7C 9H");
            _guide.HandGuide = false;
            _view.PressAction();
            PassUntil(GamePhase.Drawing);

            CollectionAssert.IsEmpty(_view.PlayerView.Hints);
        }

        [Test]
        public void Showdown_NamesBothHands_AndMarksTheWinner()
        {
            Setup();
            _view.PressAction();
            PassUntil(GamePhase.Drawing);
            _view.PressAction();
            PassUntil(GamePhase.RoundOver);

            Assert.AreEqual("Two Pair  WINS", _view.PlayerView.Caption);
            StringAssert.StartsWith("One Pair", _view.HouseView.Caption);
            Assert.AreEqual(Tone.Triumph, _view.PlayerView.CaptionTone);
        }

        // ------------------------------------------------------------------ hand ranks

        [Test]
        public void HandRanks_OpenWithTheDealersPayouts_AndCloseAsAnOverlay()
        {
            Setup();

            _view.PressHandRanks();
            Assert.IsTrue(_view.HandRanksOpen);
            Assert.AreEqual(10, _view.HandRanksPayouts.GetMultiplier(HandCategory.FourOfAKind), "Mammon's own multipliers.");

            Assert.IsTrue(_presenter.CloseOverlay(), "Esc closes the panel first.");
            Assert.IsFalse(_view.HandRanksOpen);
            Assert.IsFalse(_presenter.CloseOverlay(), "Nothing left to close: Esc may leave the table.");
        }

        // ------------------------------------------------------------------ first-game tips

        [Test]
        public void FirstDecision_GetsATip_OnlyOnce()
        {
            Setup();

            _view.PressAction();
            StringAssert.Contains("RAISE", _view.DealerView.LastLine);
            int lines = _view.DealerView.LinesSaid;

            _view.PressBet(BetAction.Pass);

            Assert.AreEqual(lines, _view.DealerView.LinesSaid, "The second decision has no tip.");
            Assert.IsTrue(_guide.HasSeenTip(FirstDecisionTip));
        }

        [Test]
        public void FirstDraw_GetsATip()
        {
            Setup();
            _view.PressAction();
            PassUntil(GamePhase.Drawing);

            StringAssert.Contains("DRAW", _view.DealerView.LastLine);
        }

        [Test]
        public void SeenTips_AreNotRepeated_UntilReset()
        {
            Setup();
            _guide.Seen.Add(FirstDecisionTip);
            int lines = _view.DealerView.LinesSaid;

            _view.PressAction();

            Assert.AreEqual(lines, _view.DealerView.LinesSaid);
        }

        [Test]
        public void FirstReRaise_IsExplained_InsteadOfTheUsualLine()
        {
            Setup(houseBetting: new HellPokerGameTests.FixedHouseBetting(true), tableCapPercent: 50);
            _view.PressAction();
            PassUntil(GamePhase.Drawing);
            _view.PressAction();

            _view.PressBet(BetAction.Raise);

            Assert.AreEqual(GamePhase.HouseReRaise, _game.Phase);
            StringAssert.Contains("CALL", _view.DealerView.LastLine);
            Assert.AreEqual(DealerMood.Neutral, _view.DealerView.LastMood);
        }

        [Test]
        public void FinalStretch_IsExplained()
        {
            Setup(startingYears: 200);

            StringAssert.Contains("no passing", _view.DealerView.LastLine);
        }

        [Test]
        public void Soul_IsExplained_WithoutANumber()
        {
            Setup();
            _game.TakeOver(2100, 1);

            _presenter.SwitchTable(DealerRoster.Mammon);

            StringAssert.Contains("soul", _view.DealerView.LastLine);
            StringAssert.DoesNotMatch(@"\d{2,}", _view.DealerView.LastLine);
        }
    }
}
