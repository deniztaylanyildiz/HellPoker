using System;
using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class MainMenuPresenterTests
    {
        private sealed class FakeMenuView : IMainMenuView
        {
            public bool IsVisible { get; private set; }
            public bool ContinueShown { get; private set; }

            public event Action NewGamePressed;
            public event Action ContinuePressed;
            public event Action QuitPressed;

            public void Show(bool canContinue)
            {
                IsVisible = true;
                ContinueShown = canContinue;
            }

            public void Hide() => IsVisible = false;

            public void PressNewGame() => NewGamePressed?.Invoke();
            public void PressContinue() => ContinuePressed?.Invoke();
            public void PressQuit() => QuitPressed?.Invoke();
        }

        private sealed class FakeDealerSelectView : IDealerSelectView
        {
            public bool IsVisible { get; private set; }
            public IReadOnlyList<DealerCard> Shown { get; private set; }

            public event Action<int> DealerChosen;
            public event Action BackPressed;

            public void Show(IReadOnlyList<DealerCard> dealers)
            {
                IsVisible = true;
                Shown = dealers;
            }

            public void Hide() => IsVisible = false;

            public void Choose(int index) => DealerChosen?.Invoke(index);
            public void PressBack() => BackPressed?.Invoke();
        }

        private sealed class FakeSession : IRunSession
        {
            public bool CanContinue { get; set; }
            public int NewRuns { get; private set; }
            public Dealer Dealer { get; private set; }

            public void StartNewRun(Dealer dealer)
            {
                NewRuns++;
                Dealer = dealer;
                CanContinue = true;
            }
        }

        private sealed class FakeQuitter : IApplicationQuitter
        {
            public bool QuitRequested { get; private set; }

            public void Quit() => QuitRequested = true;
        }

        private FakeMenuView _menu;
        private FakeDealerSelectView _dealerSelect;
        private FakeTableView _table;
        private FakeSession _session;
        private FakeQuitter _quitter;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _menu = new FakeMenuView();
            _dealerSelect = new FakeDealerSelectView();
            _table = new FakeTableView();
            _session = new FakeSession();
            _quitter = new FakeQuitter();
            _presenter = new MainMenuPresenter(_menu, _dealerSelect, _table, _session, _quitter, DealerRoster.All);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        private void StartRunWith(int dealerIndex = 0)
        {
            _menu.PressNewGame();
            _dealerSelect.Choose(dealerIndex);
        }

        [Test]
        public void GameStartsOnTheMenu_WithoutContinue()
        {
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_menu.ContinueShown);
            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.IsFalse(_table.Visible);
        }

        [Test]
        public void NewGame_OpensDealerChoice_WithEveryDealer()
        {
            _menu.PressNewGame();

            Assert.IsTrue(_dealerSelect.IsVisible);
            Assert.IsFalse(_menu.IsVisible);
            Assert.IsFalse(_table.Visible);
            Assert.AreEqual(0, _session.NewRuns, "Nothing starts until a dealer is chosen.");
            CollectionAssert.AreEqual(new[] { "MAMMON", "BELIAL", "LILITH" }, new[] { _dealerSelect.Shown[0].Name, _dealerSelect.Shown[1].Name, _dealerSelect.Shown[2].Name });
        }

        [Test]
        public void ChoosingADealer_StartsRunAtTheirTable()
        {
            StartRunWith(2);

            Assert.AreEqual(1, _session.NewRuns);
            Assert.AreEqual(DealerRoster.LilithId, _session.Dealer.Id);
            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void DealerChoice_Back_ReturnsToMenu()
        {
            _menu.PressNewGame();

            _dealerSelect.PressBack();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.AreEqual(0, _session.NewRuns);
        }

        [Test]
        public void Escape_OnDealerChoice_ReturnsToMenu()
        {
            _menu.PressNewGame();
            Assert.IsTrue(_presenter.IsMenuOpen, "The table does not take input while choosing a dealer.");

            _presenter.ToggleMenu();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_dealerSelect.IsVisible);
        }

        [Test]
        public void MenuButton_OpensMenu_WithContinue_DuringARun()
        {
            StartRunWith();

            _table.PressMenu();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsTrue(_menu.ContinueShown);
            Assert.IsFalse(_table.Visible);
        }

        [Test]
        public void Continue_ReturnsToTable_WithoutRestarting()
        {
            StartRunWith();
            _table.PressMenu();

            _menu.PressContinue();

            Assert.AreEqual(1, _session.NewRuns);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void Escape_TogglesBetweenTableAndMenu()
        {
            StartRunWith();

            _presenter.ToggleMenu();
            Assert.IsTrue(_presenter.IsMenuOpen);

            _presenter.ToggleMenu();
            Assert.IsFalse(_presenter.IsMenuOpen);
        }

        [Test]
        public void Escape_OnFirstMenu_StaysThere()
        {
            _presenter.ToggleMenu();

            Assert.IsTrue(_presenter.IsMenuOpen);
            Assert.IsFalse(_table.Visible);
        }

        [Test]
        public void Quit_AsksTheApplicationToQuit()
        {
            _menu.PressQuit();

            Assert.IsTrue(_quitter.QuitRequested);
        }
    }
}
