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
            IGuideSettings guide = null, RunArchive archive = null, ICheatPolicy policy = null, int maliceMax = 1, Dealer dealer = null,
            string rest = Blanks)
        {
            dealer = dealer ?? DealerRoster.Mammon;
            _hidden.Clear();   // the fixture instance is shared by its tests
            _view = new FakeTableView();
            _presenter = new TablePresenter(d =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{player} {house} {rest}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
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
            CollectionAssert.AreEqual(new[] { 2 }, impact.PlayerCards, "The 7♥, a card to be thrown.");
            CollectionAssert.Contains(_view.TextLog, "Collateral. This one stays with you — on my terms.");
            Assert.IsNull(_view.Intent, "Played out: the sign comes down.");
            Assert.AreEqual(0, _view.Malice.Value);
            Assert.AreEqual(CardMark.Chained, _view.PlayerView.Slots[2].Mark);
        }

        [Test]
        public void AChainedCard_CannotBePickedForTheDraw_AndSaysWhy()
        {
            Start(new CollateralCheat());
            ToTheDraw();

            _view.PlayerView.Click(2);

            CollectionAssert.DoesNotContain(_presenter.SelectedDiscards, 2);
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
        public void AVeiledCard_TurnsFaceDown_UnderItsVeil_FromItsFirstTurn()
        {
            Start(new NightVeilCheat());

            _view.PressAction();   // the deal: the third card turns — in the dark

            Assert.AreEqual(CardSlot.SlotKind.Back, _view.PlayerView.Slots[2].Kind);
            Assert.AreEqual(CardMark.Veiled, _view.PlayerView.Slots[2].Mark);
            Assert.AreEqual(CheatIds.NightVeil, _view.Impacts.Single().CheatId);
            CollectionAssert.Contains(_view.TextLog, "NIGHT VEIL", "The sign went up before the blow, even at the deal.");

            _presenter.CheckToDraw();
            Assert.AreEqual(CardSlot.SlotKind.Back, _view.PlayerView.Slots[2].Kind, "Still dark at the draw.");
            Assert.AreEqual(CardMark.Veiled, _view.PlayerView.Slots[2].Mark);
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

            StringAssert.Contains("Mammon chained your 7♥ as collateral.", _view.Message);
        }

        [Test]
        public void TheResultLine_NamesATitledDemon_WordByWord()
        {
            Start(new CollateralCheat(), years: 150, dealer: DealerRoster.Lucifer);

            PlayOut();

            StringAssert.Contains("The Morning Star chained your 7♥ as collateral.", _view.Message);
        }

        // ------------------------------------------------------------------ backfire

        [Test]
        public void ABackfire_TurnsTheNewCard_FlashesBackfire_AngersTheDemon_AndIsRecorded()
        {
            // The K♥ burns into the 2♦: a pair of twos for the player.
            Start(new BurningCardCheat(), player: "2C 5D KH 9S JC", house: "3D 4D 6C 7S 8D", rest: "2D 6D 8S 2S 3H QD QC 8H 7C 6H");

            ToTheDraw();

            var moment = _view.Moments.Single(m => m.moment == TableMoment.Backfire);
            Assert.AreEqual("BACKFIRE", moment.text);
            CollectionAssert.AreEqual(new[] { 2 }, moment.cards);
            Assert.AreEqual(new Card(Rank.Two, Suit.Diamonds), _view.PlayerView.Slots[2].Card, "The new card is already on the table.");
            Assert.IsTrue(_view.DealerView.Said.Any(s => s.mood == DealerMood.Annoyed), "The demon is angry.");

            PlayOut();

            StringAssert.Contains("It backfired!", _view.Message);
            Assert.AreEqual(1, _presenter.Records.BackfiresSeen);
        }

        [Test]
        public void AnAimedCheat_ShowsNoBackfire()
        {
            Start(new CollateralCheat());

            PlayOut();

            Assert.IsFalse(_view.Moments.Any(m => m.moment == TableMoment.Backfire));
            Assert.AreEqual(0, _presenter.Records.BackfiresSeen);
        }

        // ------------------------------------------------------------------ what the player may not see, they never see

        private readonly HashSet<Card> _hidden = new HashSet<Card>();

        /// <summary>Notes every card of the player's that is (or was, this hand) kept from them.</summary>
        private void NoteHidden()
        {
            if (_game.PlayerHand == null) return;
            for (int i = 0; i < Hand.Size; i++)
                if (_game.WasPlayerCardHidden(i))
                    _hidden.Add(_game.PlayerHand[i]);
        }

        /// <summary>
        /// No row of the player's cards shows a hidden card's face until the House has shown all five (the showdown), and no
        /// word on the table names it at all.
        /// </summary>
        private void AssertTheDarkHeld()
        {
            Assert.IsNotEmpty(_hidden, "The test needs a hidden card.");
            bool showdown = false;
            foreach (var (house, slots) in _view.ShowLog)
            {
                if (house && slots.All(s => s.Kind == CardSlot.SlotKind.Face)) showdown = true;
                if (house || showdown) continue;
                foreach (CardSlot slot in slots)
                    Assert.IsFalse(slot.Kind == CardSlot.SlotKind.Face && _hidden.Contains(slot.Card),
                        $"{slot.Card} showed its face before the showdown.");
            }
            Assert.IsTrue(showdown, "The hand reached its showdown.");
            foreach (string text in _view.TextLog.Where(t => t != null))
                foreach (Card card in _hidden)
                    StringAssert.DoesNotContain(card.ToString(), text, $"\"{text}\" names a hidden card.");
        }

        [Test]
        public void NightVeil_TheCardNeverShowsItsFace_BeforeTheShowdown()
        {
            Start(new NightVeilCheat(), player: "KS KH 2C 5D 9C", house: "3D 4D 6C 7S 8D");
            _view.PressAction();
            NoteHidden();
            _presenter.CheckToDraw();
            NoteHidden();
            _view.PressAction();   // stand pat (the dark card stays)
            NoteHidden();
            while (_game.Phase == GamePhase.DrawReveal || _game.Phase == GamePhase.HouseReveal)
            {
                _view.PressBet(BetAction.Pass);
                NoteHidden();
            }

            AssertTheDarkHeld();
            Assert.IsFalse(_view.PlayerView.Hints.Count > 0 && _view.PlayerView.Hints.Count < Hand.Size,
                "No keep frames with a card in the dark.");
        }

        [Test]
        public void Moonless_TheDrawnCardsNeverShowTheirFaces_BeforeTheShowdown()
        {
            Start(new MoonlessCheat());
            ToTheDraw();
            _view.PlayerView.Click(0);
            _view.PlayerView.Click(2);
            _view.PressAction();   // the draw: the new cards stay dark
            NoteHidden();
            while (_game.Phase == GamePhase.DrawReveal || _game.Phase == GamePhase.HouseReveal)
            {
                _view.PressBet(BetAction.Pass);
                NoteHidden();
            }

            AssertTheDarkHeld();
        }

        [Test]
        public void Moonless_InASealedHand_TheCardsStayDarkWhileTheHouseTurns()
        {
            Start(new MoonlessCheat());
            _view.PressAction();
            _view.PressBet(BetAction.Raise);
            _view.PressBet(BetAction.Raise);   // the table is full: sealed, on to the draw
            Assert.AreEqual(GamePhase.Drawing, _game.Phase);
            _view.PlayerView.Click(0);
            _view.PlayerView.Click(2);

            _view.PressAction();   // the draw — and the sealed hand plays out on its own

            NoteHidden();
            Assert.AreEqual(GamePhase.RoundOver, _game.Phase);
            AssertTheDarkHeld();
        }

        [Test]
        public void SerpentSwap_WhatComesBackNeverShowsItsFace_BeforeTheShowdown()
        {
            Start(new SerpentSwapCheat(), player: "QS QH 2C 5D 9C", house: "3D 4D 6C 7S 8D");
            ToTheDraw();
            _view.PressAction();   // stand pat: the serpent strikes after the draw
            NoteHidden();
            while (_game.Phase == GamePhase.DrawReveal || _game.Phase == GamePhase.HouseReveal)
            {
                _view.PressBet(BetAction.Pass);
                NoteHidden();
            }

            AssertTheDarkHeld();
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

        /// <summary>A run resumed from <paramref name="snapshot"/>, every demon with their own cheats.</summary>
        private RunArchive ResumeWithDemonsCheats(RunSnapshot snapshot, Dealer dealer)
        {
            var archive = new RunArchive(new MemoryStore());
            archive.SaveRun(snapshot);
            _view = new FakeTableView();
            _presenter = new TablePresenter(d =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{Nothing} {HouseFullHouse} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(d.Cheats, d.MaliceMax, random), random);
            }, _view, null, archive);
            _presenter.Resume(dealer, archive.LoadRun());
            return archive;
        }

        [Test]
        public void ChangingTables_KeepsTheGauge_AndTheGrudge()
        {
            RunArchive archive = ResumeWithDemonsCheats(new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon"), malice: 3, grudge: 2),
                DealerRoster.Mammon);

            _presenter.SwitchTable(DealerRoster.Lilith);

            Assert.AreEqual(3, _game.Malice, "No escaping a full gauge by changing tables.");
            Assert.AreEqual(2, _game.Grudge);
            Assert.AreEqual(3, _view.Malice.Value);
            Assert.AreEqual(3, archive.LoadRun().Malice);
            Assert.AreEqual(2, archive.LoadRun().Grudge);

            _presenter.SwitchTable(DealerRoster.Belial);

            Assert.AreEqual(2, _game.Malice, "Never fuller than this demon's gauge (Belial's holds 2).");
        }

        [Test]
        public void WalkingOutOnACheat_TheDemonMocks_AndHoldsAGrudge()
        {
            var hand = new HandInProgress(200, 100, false, false, false, CheatIds.Collateral, cheatResolved: false);
            ResumeWithDemonsCheats(new RunSnapshot("mammon", 900, 5, new RunStats(4, 800, 900, null, new[] { "mammon" }, false), hand),
                DealerRoster.Mammon);

            CollectionAssert.Contains(new[]
            {
                "You ran from my collateral? Where to? This is Hell. I charge interest on running.",
                "Skipped out before I could collect? Then I collect twice. Soon, and often."
            }, _view.DealerView.LastLine);
            StringAssert.Contains("grudge", _view.Message);
            Assert.AreEqual(4, _game.Malice, "Full: the cheat comes with the next deal.");
            Assert.AreEqual(3, _game.Grudge);
        }

        [Test]
        public void WalkingOutWithNoCheatComing_IsTheUsualForfeit()
        {
            var hand = new HandInProgress(200, 100, false, false, false);
            ResumeWithDemonsCheats(new RunSnapshot("mammon", 900, 5, new RunStats(4, 800, 900, null, new[] { "mammon" }, false), hand),
                DealerRoster.Mammon);

            StringAssert.DoesNotContain("grudge", _view.Message);
            Assert.AreEqual(0, _game.Grudge);
        }

        [Test]
        public void GoingToASmallGaugeAndBack_DoesNotDrainIt()
        {
            ResumeWithDemonsCheats(new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon"), malice: 3), DealerRoster.Mammon);

            _presenter.SwitchTable(DealerRoster.Belial);
            Assert.AreEqual(2, _game.Malice, "3/4 is 2/2, rounded up.");
            _presenter.SwitchTable(DealerRoster.Mammon);

            Assert.AreEqual(4, _game.Malice, "2/2 is full: full at Mammon's too.");
        }

        [Test]
        public void AFullGauge_StaysFull_AtEveryTable()
        {
            ResumeWithDemonsCheats(new RunSnapshot("lilith", 900, 4, new RunStats(1000, "lilith"), malice: 4), DealerRoster.Lilith);

            _presenter.SwitchTable(DealerRoster.Belial);
            Assert.AreEqual(2, _game.Malice);
            _presenter.SwitchTable(DealerRoster.Lilith);

            Assert.AreEqual(4, _game.Malice, "The cheat still comes with the next deal.");
        }

        [Test]
        public void AnEmptyGauge_StaysEmpty()
        {
            ResumeWithDemonsCheats(new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon")), DealerRoster.Mammon);

            _presenter.SwitchTable(DealerRoster.Belial);

            Assert.AreEqual(0, _game.Malice);
        }

        [Test]
        public void AFallFromLucifer_GivesBackTheGaugeTheyHadBelow()
        {
            const string win = "2C 9C JC 4C KC 2D 2H 5S 7H 9D 3S 6C JD QC 10S 2S 4H 5C 6D 7S";
            const string loseBig = "2C 5D 7H 9S JC KS KH KD 4C 4H 3S 6C JD QC 10S 2S 4H 5C 6D 7S";
            var decks = new Queue<string>(new[] { win, win, loseBig, win });
            string last = win;
            _view = new FakeTableView();
            _presenter = new TablePresenter(d =>
            {
                last = decks.Count > 0 ? decks.Dequeue() : last;
                var random = new FirstChoice();
                // Each table its own stacked deck: the run's deck is not carried (a fresh deal every hand).
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(continuousDeck: false)), TestDecks.Stacked(last), HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy(d.MaxDiscards)), new HouseDrawStrategy(d.MaxDiscards), d.Payouts, null,
                    new CheatSession(d.Cheats, d.MaliceMax, random), random);
            }, _view, null, null, DealerRoster.Lucifer);
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.TakeOver(200, 3);
            _game.RestoreMalice(3, false);

            _presenter.SwitchTable(DealerRoster.Mammon);   // 200: summoned at once
            Assert.IsTrue(_presenter.IsAtFinalTable);
            Assert.AreEqual(1, _game.Malice, "3/4 is his whole gauge of 1.");

            _view.PressAction();
            if (_game.Phase == GamePhase.PlayerReveal) _presenter.CheckToDraw();
            _view.PressAction();
            for (int guard = 0; guard < 5 && _game.Phase != GamePhase.RoundOver; guard++) _view.PressBet(BetAction.Pass);
            _view.PressAction();   // lost above the gate: cast down

            Assert.AreEqual("mammon", _presenter.CurrentDealerId);
            Assert.AreEqual(3, _game.Malice, "Mammon's 3/4 comes back, not Lucifer's spent gauge.");
        }
        // ------------------------------------------------------------------ the thorn's price, before the draw

        private int ThornedCard() => Enumerable.Range(0, 5).Single(_game.IsPlayerCardThorned);

        [Test]
        public void PickingTheThornedCard_SaysThePrice_OnTheButtonToo()
        {
            Start(new ThornCheat());
            ToTheDraw();
            int thorned = ThornedCard();
            int other = Enumerable.Range(0, 5).First(i => i != thorned);

            _presenter.ToggleDiscard(other);
            Assert.AreEqual("DRAW 1", _view.ActionLabel);

            _presenter.ToggleDiscard(other);
            _presenter.ToggleDiscard(thorned);

            Assert.AreEqual("DRAW 1\n(+100 YEARS)", _view.ActionLabel);
            StringAssert.Contains("+100 YEARS", _view.Message);
            Assert.AreEqual(Tone.Warning, _view.MessageTone);
        }

        [Test]
        public void ThePriceOfAThorn_OnTheSoul_HasNoNumber()
        {
            Start(new ThornCheat(), years: 2100);
            ToTheDraw();

            _presenter.ToggleDiscard(ThornedCard());

            Assert.AreEqual("DRAW 1\n(THORN BITES)", _view.ActionLabel);
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(_view.Message, @"\d{2,}"), _view.Message);
            StringAssert.Contains("soul", _view.Message);
        }
    }
}
