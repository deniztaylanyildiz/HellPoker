using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The demons' cheats at the table: the gauge and the announced intent, the blow on the cards (its meaning, the card as
    /// it was, the demon's line), the marks the cards keep, Belial's lie coming out, the result line, the tips, the H panel —
    /// and never a number while the soul is on the table.
    /// </summary>
    public class CheatPresenterTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseTwos = "2D 2H 5S 7H 9D";
        private const string Blanks = "3S 6D 8S 2S 3H QD QC 8H 7C 6H";

        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            private readonly ICheat _shown;
            public OnlyCheat(ICheat cheat, ICheat shown = null) { _cheat = cheat; _shown = shown; }
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, _shown);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        private sealed class FakeGuide : IGuideSettings
        {
            public bool HandGuide { get; set; } = true;
            public readonly HashSet<string> Seen = new HashSet<string>();
            public bool HasSeenTip(string tip) => Seen.Contains(tip);
            public void MarkTipSeen(string tip) => Seen.Add(tip);
        }

        private FakeTableView _view;
        private TablePresenter _presenter;
        private HellPokerGame _game;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        /// <summary>Mammon's table, the demon playing <paramref name="cheat"/> every hand (or his own cheats with <paramref name="policy"/>).</summary>
        private void Start(ICheat cheat, string player = Nothing, string house = HouseFullHouse, ICheat shown = null, int years = 1000,
            IGuideSettings guide = null, RunArchive archive = null, ICheatPolicy policy = null, int maliceMax = 1, Dealer dealer = null)
        {
            dealer = dealer ?? DealerRoster.Mammon;
            _view = new FakeTableView();
            _presenter = new TablePresenter(d =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null,
                    new CheatSession(policy ?? new OnlyCheat(cheat, shown), maliceMax, random), random);
            }, _view, guide, archive);
            _presenter.StartNewRun(dealer);
            if (years != 1000)
            {
                _game.TakeOver(years, 3);
                _presenter.SwitchTable(dealer);
            }
        }

        private void ToTheDraw()
        {
            _view.PressAction();
            if (_game.Phase == GamePhase.PlayerReveal) _presenter.CheckToDraw();
        }

        private void PlayOut()
        {
            ToTheDraw();
            _view.PressAction();   // stand pat
            while (_game.Phase == GamePhase.DrawReveal || _game.Phase == GamePhase.HouseReveal)
                _view.PressBet(BetAction.Pass);
        }

        // ------------------------------------------------------------------ gauge and intent

        [Test]
        public void TheGauge_AndTheIntent_ShowAtTheDeal()
        {
            Start(new CollateralCheat());
            Assert.IsNull(_view.Intent, "Nothing announced between hands.");

            _view.PressAction();

            Assert.AreEqual("mammon", _view.Malice.DealerId);
            Assert.AreEqual(1, _view.Malice.Value);
            Assert.AreEqual(1, _view.Malice.Max);
            Assert.AreEqual("COLLATERAL", _view.Intent.Name);
            Assert.AreEqual(CheatIds.Collateral, _view.Intent.Id);
            StringAssert.Contains("chained", _view.Intent.Description);
        }

        [Test]
        public void TheBlow_LandsOnTheCard_TheDemonSpeaks_AndTheMarkStays()
        {
            Start(new CollateralCheat());

            ToTheDraw();

            CheatImpact impact = _view.Impacts.Single();
            Assert.AreEqual(CheatIds.Collateral, impact.CheatId);
            CollectionAssert.AreEqual(new[] { 4 }, impact.PlayerCards);
            CollectionAssert.Contains(_view.TextLog, "Collateral. This one stays with you — on my terms.");
            Assert.IsNull(_view.Intent, "Played out: the sign comes down.");
            Assert.AreEqual(0, _view.Malice.Value);
            Assert.AreEqual(CardMark.Chained, _view.PlayerView.Slots[4].Mark);
        }

        [Test]
        public void AChainedCard_CannotBePickedForTheDraw_AndSaysWhy()
        {
            Start(new CollateralCheat());
            ToTheDraw();

            _view.PlayerView.Click(4);

            CollectionAssert.DoesNotContain(_presenter.SelectedDiscards, 4);
            StringAssert.Contains("collateral", _view.Message);
        }

        [Test]
        public void AChangedCard_ShowsAsItWas_ThenTurns()
        {
            Start(new BurningCardCheat(), player: "2C 5D KH 9S JC", house: "3D 4D 6C 7S 8D");

            ToTheDraw();

            var king = new Card(Rank.King, Suit.Hearts);
            Assert.IsTrue(_view.PlayerView.History.Any(row => row[2].Kind == CardSlot.SlotKind.Face && row[2].Card == king),
                "The K♥ is seen before it burns.");
            Assert.AreNotEqual(king, _view.PlayerView.Slots[2].Card, "Then it is another card.");
        }

        [Test]
        public void AVeiledCard_ShowsFaceDown_UnderItsVeil()
        {
            Start(new NightVeilCheat());

            ToTheDraw();

            Assert.AreEqual(CardSlot.SlotKind.Back, _view.PlayerView.Slots[0].Kind);
            Assert.AreEqual(CardMark.Veiled, _view.PlayerView.Slots[0].Mark);
        }

        [Test]
        public void AFalseFace_ShowsTheFakeCard_WithItsSheen_UntilTheShowdown()
        {
            Start(new FalseFaceCheat());
            ToTheDraw();
            _view.PressAction();
            _view.PressBet(BetAction.Pass);   // the House shows two cards

            CardSlot shown = _view.HouseView.Slots[0];
            Assert.AreEqual(CardMark.FalseFace, shown.Mark);
            Assert.AreNotEqual(_game.HouseHand[0], shown.Card);

            _view.PressBet(BetAction.Pass);
            Assert.AreEqual(_game.HouseHand[0], _view.HouseView.Slots[0].Card, "The truth at the showdown.");
            Assert.AreEqual(CardMark.None, _view.HouseView.Slots[0].Mark);
        }

        [Test]
        public void BelialsLie_ComesOut_WhenTheRealCheatStrikes()
        {
            Start(new CollateralCheat(), shown: new ThornCheat());
            _view.PressAction();
            Assert.AreEqual("THORN", _view.Intent.Name, "What he said he would do.");

            _presenter.CheckToDraw();

            Assert.AreEqual(CheatIds.Collateral, _view.Lies.Single().Id, "The sign shatters into the truth.");
            CollectionAssert.Contains(_view.TextLog, "Did you believe me? How sweet.");
        }

        [Test]
        public void TheResult_TellsTheHandsCheat()
        {
            Start(new CollateralCheat());

            PlayOut();

            StringAssert.Contains("Mammon chained your J♣ as collateral.", _view.Message);
        }

        [Test]
        public void TheResultLine_NamesATitledDemon_WordByWord()
        {
            Start(new CollateralCheat(), years: 150, dealer: DealerRoster.Lucifer);

            PlayOut();

            StringAssert.Contains("The Morning Star chained your J♣ as collateral.", _view.Message);
        }

        [Test]
        public void TheTithe_IsTold_WithItsYears()
        {
            Start(new TitheCheat(), Flush, HouseTwos);

            PlayOut();

            StringAssert.Contains("Mammon kept a tithe: 100 years of your win.", _view.Message);
            Assert.AreEqual(CheatIds.Tithe, _view.Impacts.Single().CheatId);
        }

        [Test]
        public void OnTheSoul_TheThornIsTold_WithoutNumbers()
        {
            Start(new ThornCheat(), house: HouseTwos, years: 2100);   // a small loss: the soul burns but survives
            _view.TextLog.Clear();
            ToTheDraw();
            _view.PlayerView.Click(0);   // the thorned card

            _view.PressAction();
            while (_game.Phase == GamePhase.DrawReveal || _game.Phase == GamePhase.HouseReveal)
                _view.PressBet(BetAction.Pass);

            StringAssert.Contains("Mammon's thorn drew blood from your soul.", _view.Message);
            foreach (string text in _view.TextLog.Where(t => t != null))
                Assert.IsFalse(Regex.IsMatch(text, @"\d{2,} years"), $"A number reached the table: \"{text}\"");
        }

        [Test]
        public void TheFirstCheat_OfADemon_IsExplained_Once()
        {
            var guide = new FakeGuide();
            Start(new CollateralCheat(), guide: guide);

            _view.PressAction();

            Assert.IsTrue(guide.Seen.Contains("tip.cheat.mammon"));
            CollectionAssert.Contains(_view.TextLog, "When my purse of malice is full, I collect. The sign above me says how.");
        }

        [Test]
        public void TheHandsPanel_TellsTheAnnouncedCheat()
        {
            Start(new CollateralCheat());
            _view.PressAction();

            _view.PressHandRanks();

            StringAssert.StartsWith("COLLATERAL: ", _view.HandRanksFootnote);
        }

        [Test]
        public void AResumedRun_RemembersTheGauge()
        {
            var store = new MemoryStore();
            var archive = new RunArchive(store);
            archive.SaveRun(new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon"), malice: 3));
            _view = new FakeTableView();
            _presenter = new TablePresenter(d =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{Nothing} {HouseFullHouse} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(d.Cheats, d.MaliceMax, random), random);
            }, _view, null, archive);

            _presenter.Resume(DealerRoster.Mammon, archive.LoadRun());

            Assert.AreEqual(3, _game.Malice);
            Assert.AreEqual(3, _view.Malice.Value);
            Assert.AreEqual(4, _view.Malice.Max);
            Assert.AreEqual(3, archive.LoadRun().Malice, "Saved again as it is.");
        }
    }
}
