using System;
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

        private sealed class FakeSession : IRunSession
        {
            public bool CanContinue { get; set; }
            public int NewRuns { get; private set; }

            public void StartNewRun()
            {
                NewRuns++;
                CanContinue = true;
            }
        }

        private sealed class FakeQuitter : IApplicationQuitter
        {
            public bool QuitRequested { get; private set; }

            public void Quit() => QuitRequested = true;
        }

        private FakeMenuView _menu;
        private FakeTableView _table;
        private FakeSession _session;
        private FakeQuitter _quitter;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _menu = new FakeMenuView();
            _table = new FakeTableView();
            _session = new FakeSession();
            _quitter = new FakeQuitter();
            _presenter = new MainMenuPresenter(_menu, _table, _session, _quitter);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        [Test]
        public void GameStartsOnTheMenu_WithoutContinue()
        {
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_menu.ContinueShown);
            Assert.IsFalse(_table.Visible);
        }

        [Test]
        public void NewGame_StartsRun_AndShowsTable()
        {
            _menu.PressNewGame();

            Assert.AreEqual(1, _session.NewRuns);
            Assert.IsFalse(_menu.IsVisible);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void MenuButton_OpensMenu_WithContinue_DuringARun()
        {
            _menu.PressNewGame();

            _table.PressMenu();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsTrue(_menu.ContinueShown);
            Assert.IsFalse(_table.Visible);
        }

        [Test]
        public void Continue_ReturnsToTable_WithoutRestarting()
        {
            _menu.PressNewGame();
            _table.PressMenu();

            _menu.PressContinue();

            Assert.AreEqual(1, _session.NewRuns);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void Escape_TogglesBetweenTableAndMenu()
        {
            _menu.PressNewGame();

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
