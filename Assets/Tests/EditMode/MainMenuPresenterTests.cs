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
            public event Action ChangeTablePressed;

            public void Show(bool canContinue)
            {
                IsVisible = true;
                ContinueShown = canContinue;
            }

            public void Hide() => IsVisible = false;

            public void PressNewGame() => NewGamePressed?.Invoke();
            public void PressContinue() => ContinuePressed?.Invoke();
            public void PressQuit() => QuitPressed?.Invoke();
            public void PressChangeTable() => ChangeTablePressed?.Invoke();
        }

        private sealed class FakeDealerSelectView : IDealerSelectView
        {
            public bool IsVisible { get; private set; }
            public IReadOnlyList<DealerChoice> Shown { get; private set; }
            public string Warning { get; private set; }

            public event Action<int> DealerChosen;
            public event Action BackPressed;
            public event Action SeatConfirmed;
            public event Action SeatCancelled;

            public void Show(IReadOnlyList<DealerChoice> dealers)
            {
                IsVisible = true;
                Shown = dealers;
                Warning = null;
            }

            public void AskToConfirm(string warning) => Warning = warning;

            public void Hide() => IsVisible = false;

            public void Choose(int index) => DealerChosen?.Invoke(index);
            public void PressBack() => BackPressed?.Invoke();
            public void Confirm() => SeatConfirmed?.Invoke();
            public void Cancel() => SeatCancelled?.Invoke();
        }

        private sealed class FakeSession : IRunSession
        {
            public bool CanContinue { get; set; }
            public int NewRuns { get; private set; }
            public int Switches { get; private set; }
            public Dealer Dealer { get; private set; }
            public string CurrentDealerId => Dealer?.Id;

            /// <summary>The sentence the fake player carries; compared with each dealer's soul line.</summary>
            public int Years { get; set; } = 1000;

            /// <summary>When true, the dealer refuses to let the player leave (soul bound).</summary>
            public bool SoulBound { get; set; }

            public event Action LeaveRequested;

            public void StartNewRun(Dealer dealer)
            {
                NewRuns++;
                Dealer = dealer;
                CanContinue = true;
            }

            public void RequestLeave()
            {
                if (!SoulBound) LeaveRequested?.Invoke();
            }

            public bool WouldStakeSoul(Dealer dealer) => dealer.TakesSoulAt(Years);

            public void SwitchTable(Dealer dealer)
            {
                Switches++;
                Dealer = dealer;
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
            CollectionAssert.AreEqual(new[] { "MAMMON", "BELIAL", "LILITH" }, new[] { _dealerSelect.Shown[0].Card.Name, _dealerSelect.Shown[1].Card.Name, _dealerSelect.Shown[2].Card.Name });
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

        // ------------------------------------------------------------------ changing tables

        [Test]
        public void NewRunChoice_FlagsNothing()
        {
            _menu.PressNewGame();

            foreach (DealerChoice choice in _dealerSelect.Shown)
                Assert.IsFalse(choice.IsCurrent || choice.SoulAtStake);
        }

        [Test]
        public void LeaveRequest_OpensTheChoice_WithTheCurrentDealerAndSoulFlags()
        {
            StartRunWith(0);
            _session.Years = 1600;

            _session.RequestLeave();

            Assert.IsTrue(_dealerSelect.IsVisible);
            Assert.IsFalse(_table.Visible);
            Assert.IsTrue(_dealerSelect.Shown[0].IsCurrent);
            Assert.IsFalse(_dealerSelect.Shown[0].SoulAtStake, "Mammon takes the soul at 2000.");
            Assert.IsFalse(_dealerSelect.Shown[1].SoulAtStake, "Belial at 1750.");
            Assert.IsTrue(_dealerSelect.Shown[2].SoulAtStake, "Lilith at 1500.");
        }

        [Test]
        public void ChangingToASafeTable_SwitchesAndKeepsTheRun()
        {
            StartRunWith(0);
            _session.RequestLeave();

            _dealerSelect.Choose(1);

            Assert.AreEqual(1, _session.NewRuns, "No new run: the sentence goes along.");
            Assert.AreEqual(1, _session.Switches);
            Assert.AreEqual(DealerRoster.BelialId, _session.CurrentDealerId);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void ChoosingTheCurrentDealer_ReturnsWithoutSwitching()
        {
            StartRunWith(0);
            _session.RequestLeave();

            _dealerSelect.Choose(0);

            Assert.AreEqual(0, _session.Switches);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void SittingPastASoulLine_NeedsConfirmation()
        {
            StartRunWith(0);
            _session.Years = 1600;
            _session.RequestLeave();

            _dealerSelect.Choose(2);

            Assert.AreEqual(0, _session.Switches, "Not before the warning is accepted.");
            Assert.IsNotEmpty(_dealerSelect.Warning);
            Assert.IsTrue(_dealerSelect.IsVisible);

            _dealerSelect.Confirm();

            Assert.AreEqual(1, _session.Switches);
            Assert.AreEqual(DealerRoster.LilithId, _session.CurrentDealerId);
            Assert.IsTrue(_table.Visible);
        }

        [Test]
        public void CancellingTheWarning_StaysOnTheChoice()
        {
            StartRunWith(0);
            _session.Years = 1600;
            _session.RequestLeave();
            _dealerSelect.Choose(2);

            _dealerSelect.Cancel();
            _dealerSelect.Confirm();

            Assert.AreEqual(0, _session.Switches, "A confirm after cancelling does nothing.");
            Assert.IsTrue(_dealerSelect.IsVisible);
        }

        [Test]
        public void BackWhileChangingTables_ReturnsToTheTable()
        {
            StartRunWith(0);
            _session.RequestLeave();

            _dealerSelect.PressBack();

            Assert.IsTrue(_table.Visible);
            Assert.IsFalse(_menu.IsVisible);
        }

        [Test]
        public void MenuChangeTable_OpensTheChoice()
        {
            StartRunWith(0);
            _table.PressMenu();

            _menu.PressChangeTable();

            Assert.IsTrue(_dealerSelect.IsVisible);
        }

        [Test]
        public void MenuChangeTable_WhenSoulBound_StaysAtTheTable()
        {
            StartRunWith(0);
            _session.SoulBound = true;
            _table.PressMenu();

            _menu.PressChangeTable();

            Assert.IsFalse(_dealerSelect.IsVisible);
            Assert.IsTrue(_table.Visible, "The dealer answers at the table.");
        }

        [Test]
        public void Quit_AsksTheApplicationToQuit()
        {
            _menu.PressQuit();

            Assert.IsTrue(_quitter.QuitRequested);
        }
    }
}
