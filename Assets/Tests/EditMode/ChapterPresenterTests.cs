using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    internal sealed class FakeChapterMapView : IChapterMapView
    {
        public event Action<int, int> NodePressed;
        public event Action MenuPressed;
        public bool IsVisible { get; private set; }
        public ChapterMapState State { get; private set; }
        public int Shows { get; private set; }

        public void Show(ChapterMapState state)
        {
            State = state;
            IsVisible = true;
            Shows++;
        }

        public void Hide() => IsVisible = false;
        public void Press(MapNode node) => NodePressed?.Invoke(node.Floor, node.Lane);
        public void PressMenu() => MenuPressed?.Invoke();
    }

    internal sealed class FakeChapterPanelView : IChapterPanelView
    {
        public event Action<int> OptionPressed;
        public bool IsOpen => Card != null;
        public PanelCard Card { get; private set; }
        public List<string> Titles { get; } = new List<string>();

        public void Show(PanelCard card)
        {
            Card = card;
            Titles.Add(card.Title);
        }

        public void Hide() => Card = null;
        public void Press(int index) => OptionPressed?.Invoke(index);
    }

    /// <summary>Phase 2's chapter presenter, with fake screens: the map, the panels, the floors' tables and the demon's.</summary>
    public class ChapterPresenterTests
    {
        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private ChapterPresenter _chapters;

        [SetUp]
        public void SetUp()
        {
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _tableView = new FakeTableView();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, null, GameRules.Default, () => 4242);
        }

        [TearDown]
        public void TearDown()
        {
            _chapters.Dispose();   // the chapter's table goes with it
        }

        /// <summary>One action at a chapter's table; a floor's imp deals every hand with a single coin (a player who only passes
        /// and never draws would otherwise feed its purse for a long while).</summary>
        private void Act()
        {
            FloorTable floor = _table.Floor;
            if (floor?.HousePurse != null && _table.Game.Phase == GamePhase.Betting && floor.HousePurse.Coins > 1)
                floor.HousePurse.Add(1 - floor.HousePurse.Coins);
            _table.PerformAction();
        }

        private void Begin(SinnerClass sinner = null)
        {
            _chapters.Start(sinner ?? SinnerRoster.Peasant);
            Assert.IsTrue(_panel.IsOpen, "the chapter opens with its words");
            _panel.Press(0);   // DESCEND
        }

        [Test]
        public void AChapterStartsOnTheMapWithItsFirstFloorLit()
        {
            _chapters.Start(SinnerRoster.Peasant);
            Assert.IsTrue(_map.IsVisible);
            Assert.AreEqual(UiText.PanelDescend, _panel.Card.Options[0].Label);
            Assert.IsEmpty(_map.State.Choices, "nothing is picked under a panel");
            _panel.Press(0);
            Assert.IsFalse(_panel.IsOpen);
            Assert.AreEqual(6, _map.State.Choices.Count);
            Assert.AreEqual(30, _map.State.Coins, "the Peasant's purse");
            Assert.IsTrue(_chapters.HasRun);
            Assert.IsTrue(_chapters.IsMapOpen);
        }

        [Test]
        public void AFirstFloorTableIsPlayedForCoinsAtItsOwnTable()
        {
            Begin();
            _map.Press(_map.State.Choices.First());
            Assert.IsTrue(_chapters.IsAtTable);
            Assert.IsFalse(_map.IsVisible);
            Assert.IsTrue(_table.AtChapterTable);
            Assert.AreEqual(Currency.Coins, _tableView.Currency);
            Assert.AreEqual(ChapterCast.ImpId, _tableView.DealerView.Dealer.Id);
            Assert.AreEqual(UiText.CoinsLabel, _tableView.SentenceView.Label);
            Assert.AreEqual(30, _tableView.SentenceView.Years, "the counter is the purse");
            Assert.AreEqual(LeaveState.Hidden, _tableView.Leave, "no leaving a chapter's table");
            ChapterRules rules = _chapters.Run.Rules;
            Assert.AreEqual(string.Format(UiText.FloorSeatTitleFormat, rules.ImpCoins, rules.Ante, rules.AnteStepHands),
                _tableView.DealerView.Dealer.Title, "the imp's purse and the ante under its name");
        }

        [Test]
        public void AFloorMatchEndsWhenTheImpsPurseIsEmptyAndGoesBackToTheMap()
        {
            Begin();
            _chapters.Run.Purse.Add(100000);   // a purse no imp can empty: the match ends with the imp's
            _map.Press(_map.State.Choices.First());
            int guard = 0;
            while (_chapters.IsAtTable && guard++ < 2000)
            {
                Act();
                if (_table.Floor != null && _table.Game.Phase == GamePhase.RoundOver)
                    Assert.AreEqual(_chapters.Run.Purse.Coins, _tableView.SentenceView.Years, "the counter shows the purse after every hand");
            }
            Assert.IsFalse(_chapters.IsAtTable, "the imp's purse ran dry, then the map");
            Assert.IsTrue(_map.IsVisible);
            Assert.IsTrue(_panel.IsOpen);
            Assert.AreEqual(UiText.MatchWonTitle, _panel.Card.Title);
            StringAssert.Contains(string.Format(UiText.MatchPurseFormat, _chapters.Run.Purse.Coins), _panel.Card.Text);
        }

        [Test]
        public void AnEmptyPurseEndsTheRunAtTheTable()
        {
            Begin();
            int guard = 0;
            while (guard++ < 5000 && !(_panel.IsOpen && _panel.Card.Title == UiText.PurseEmptyTitle))
            {
                if (_panel.IsOpen)
                {
                    Assert.AreNotEqual(UiText.GateTitle, _panel.Card.Title, "a purse of one coin never reaches the gate");
                    _panel.Press(_panel.Card.Options.Count - 1);
                }
                else if (_chapters.IsAtTable) _table.PerformAction();
                else
                {
                    _chapters.Run.Purse.Add(1 - _chapters.Run.Purse.Coins);   // one coin to the next table: all in, every hand
                    _map.Press(_map.State.Choices.First());
                }
            }
            Assert.AreEqual(UiText.PurseEmptyTitle, _panel.Card.Title);
            Assert.AreEqual(0, _chapters.Run.Purse.Coins);
            Assert.IsTrue(_chapters.Run.PurseEmptied);
            Assert.IsFalse(_chapters.HasRun, "the run is over");
            CollectionAssert.AreEqual(new[] { UiText.ChapterNewRun, UiText.Menu }, _panel.Card.Options.Select(o => o.Label));
        }

        [Test]
        public void TheWholeChapterPlaysThroughTheGateToTheDemonsTable()
        {
            Begin();
            _chapters.Run.Purse.Add(100000);   // the floors cannot end this run
            int guard = 0;
            bool sawBoss = false, sawGate = false;
            while (guard++ < 5000)
            {
                if (_panel.IsOpen)
                {
                    PanelCard card = _panel.Card;
                    if (card.Title == UiText.ChapterDoneTitle || card.Title == UiText.ChapterDamnedTitle) break;
                    if (card.Title == UiText.GateTitle) sawGate = true;
                    _panel.Press(card.Options.Count - 1);   // the way out: leave, pass, on (the fire's shuffle)
                }
                else if (_chapters.IsAtTable)
                {
                    sawBoss |= _table.AtChapterTable && _table.Floor == null;
                    Act();
                }
                else
                {
                    _map.Press(_map.State.Choices.First());
                }
            }
            Assert.IsTrue(sawGate, "the last floor leads to the gate");
            Assert.IsTrue(sawBoss, "the gate leads to Mammon's table");
            Assert.That(new[] { UiText.ChapterDoneTitle, UiText.ChapterDamnedTitle }, Does.Contain(_panel.Card.Title));
            Assert.IsFalse(_chapters.HasRun, "the chapter is over");
        }

        [Test]
        public void TheGateWritesTheMissingCoinsOnTheSentence()
        {
            Begin();
            ChapterRun run = _chapters.Run;
            run.Purse.Add(5000);
            int guard = 0;
            while (!(_panel.IsOpen && _panel.Card.Title == UiText.GateTitle) && guard++ < 3000)
            {
                if (_panel.IsOpen) _panel.Press(_panel.Card.Options.Count - 1);
                else if (_chapters.IsAtTable) Act();
                else _map.Press(_map.State.Choices.First());
            }
            run.Purse.Add(40 - run.Purse.Coins);   // short of the Peasant's 90
            int coins = run.Purse.Coins, years = run.Years, owed = run.YearsOwed;
            _panel.Press(0);   // PAY AND SIT
            Assert.AreEqual(years + run.TributeYears(coins) + owed, run.Years);
            Assert.AreEqual(250, run.TributeYears(coins));
            Assert.IsTrue(_chapters.IsAtTable);
            Assert.AreEqual(Currency.Bar, _tableView.Currency, "the demon's bar: no number on the counter");
            Assert.AreEqual("MAMMON'S BAR", _tableView.SentenceView.Label);
            Assert.AreEqual(run.Years * 2, _table.Game.Rules.SoulThreshold, "Mammon's soul line: twice his bar");
            Assert.AreEqual(0, _table.Game.Rules.ForcedRaiseYears, "no final stretch on a bar");
            Assert.AreEqual(DealerRoster.MammonId, _tableView.DealerView.Dealer.Id);
            Assert.AreEqual(run.Years, _table.Game.Years);
        }

        [Test]
        public void MenuOnTheMapAsksForTheTitleMenu()
        {
            Begin();
            int asked = 0;
            _chapters.MenuRequested += () => asked++;
            _map.PressMenu();
            Assert.AreEqual(1, asked);
            Assert.IsFalse(_chapters.Back(), "with no panel open, Esc is the menu's");
        }

        [Test]
        public void TheArrowsPickANodeAndEnterGoesThere()
        {
            Begin();
            _chapters.Step(1, 0);
            MapNode picked = _map.State.Selected;
            Assert.IsNotNull(picked);
            _chapters.Confirm();
            Assert.AreEqual(picked, _chapters.Run.Current);
        }

        [Test]
        public void EscOnAPanelTakesItsWayOut()
        {
            _chapters.Start(SinnerRoster.Peasant);
            Assert.IsFalse(_chapters.Back(), "the opening words have no way out but on: Esc is the menu's");
            Assert.IsTrue(_panel.IsOpen);
        }
    }
}
