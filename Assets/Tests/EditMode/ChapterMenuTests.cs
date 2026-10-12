using System;
using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>The title menu's door to Phase 2's chapters: the class first, then the chapter; the demo's run never moves.</summary>
    public class ChapterMenuTests
    {
        private sealed class FakeSinnerSelect : ISinnerSelectView
        {
            public event Action<int> SinnerChosen;
            public event Action BackPressed;
            public bool IsVisible { get; private set; }
            public string DealerId { get; private set; }

            public void Show(IReadOnlyList<SinnerCard> sinners, string dealerId)
            {
                IsVisible = true;
                DealerId = dealerId;
            }

            public void Hide() => IsVisible = false;
            public void Choose(int index) => SinnerChosen?.Invoke(index);
            public void Back() => BackPressed?.Invoke();
        }

        private sealed class FakeChapters : IChapterSession
        {
            public bool HasRun { get; set; }
            public bool CanContinue => HasRun;
            public Core.Chapters.ChapterRecords Records { get; } = new Core.Chapters.ChapterRecords();
            public bool IsVisible { get; private set; }
            public string MusicId => "mammon";
            public SinnerClass Started { get; private set; }
            public int Abandoned { get; private set; }
            public event Action MenuRequested;
            public event Action NewRunRequested;

            public void Start(SinnerClass sinnerClass)
            {
                Started = sinnerClass;
                HasRun = true;
            }

            public void Continue() => IsVisible = true;

            public void Abandon()
            {
                Abandoned++;
                HasRun = false;
            }

            public void Show() => IsVisible = true;
            public void Hide() => IsVisible = false;
            public void AskForMenu() => MenuRequested?.Invoke();
            public void AskForNewRun() => NewRunRequested?.Invoke();
        }

private MainMenuPresenterTests.FakeMenuView _menu;
        private MainMenuPresenterTests.FakeSession _session;
        private MainMenuPresenterTests.FakeRecords _records;
        private MainMenuPresenterTests.FakeDealerSelectView _dealers;
        private FakeSinnerSelect _sinners;
        private FakeChapters _chapters;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp() => Build(simulator: false);

        private void Build(bool simulator)
        {
            _presenter?.Dispose();
            _menu = new MainMenuPresenterTests.FakeMenuView();
            _session = new MainMenuPresenterTests.FakeSession();
            _records = new MainMenuPresenterTests.FakeRecords();
            _dealers = new MainMenuPresenterTests.FakeDealerSelectView();
            _sinners = new FakeSinnerSelect();
            _chapters = new FakeChapters();
            _presenter = new MainMenuPresenter(_menu, _dealers, new MainMenuPresenterTests.FakeSettingsView(),
                new MainMenuPresenterTests.FakeEndScreen(), _records, new FakeTableView(), _session,
                new MainMenuPresenterTests.FakeQuitter(), new MainMenuPresenterTests.FakeTransition(), DealerRoster.All, null, _sinners,
                SinnerRoster.All, null, _chapters, simulator);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        [Test]
        public void TheMenuLeadsToTheGame_NotTheSimulator()
        {
            Assert.IsFalse(_presenter.InSimulator);
            Assert.IsFalse(_menu.Simulator);
            Assert.IsFalse(_menu.ContinueShown, "no run yet");
        }

        [Test]
        public void NewGame_AsksForAClass_ThenOpensTheChapter()
        {
            _menu.PressNewGame();
            Assert.IsTrue(_sinners.IsVisible);
            Assert.IsFalse(_dealers.IsVisible, "no demon to choose: the game starts with Mammon's chapter");
            Assert.AreEqual(DealerRoster.MammonId, _sinners.DealerId, "Mammon's hall behind the choice: the first chapter is his");
            _sinners.Choose(2);
            Assert.AreSame(SinnerRoster.All[2], _chapters.Started);
            Assert.IsTrue(_chapters.IsVisible, "the map");
            Assert.AreEqual(0, _session.NewRuns, "the simulator's run is not touched");
        }

        [Test]
        public void BackFromTheClassChoiceIsTheMenu()
        {
            _menu.PressNewGame();
            _sinners.Back();
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsNull(_chapters.Started);
        }

        [Test]
        public void Continue_GoesBackToTheChapterRun()
        {
            _menu.PressNewGame();
            _sinners.Choose(0);
            _chapters.AskForMenu();
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsTrue(_menu.ContinueShown, "CONTINUE shows while a run waits");
            _menu.PressContinue();
            Assert.IsTrue(_chapters.IsVisible, "straight back to the chapter, no class choice");
            Assert.IsFalse(_sinners.IsVisible);
        }

        [Test]
        public void NewGameOverARunInProgress_AsksFirst_AndAbandonsIt()
        {
            _menu.PressNewGame();
            _sinners.Choose(0);
            _chapters.AskForMenu();
            _sinners.Hide();
            _menu.PressNewGame();
            Assert.IsTrue(_menu.IsConfirming, "a run waits: the player is asked first");
            Assert.IsFalse(_sinners.IsVisible);
            Assert.AreEqual(0, _chapters.Abandoned);
            _menu.Confirm();
            Assert.AreEqual(1, _chapters.Abandoned, "the old run is given up (damned)");
            Assert.IsTrue(_sinners.IsVisible, "then the class choice");
        }

        [Test]
        public void ANewChapterRunAsksForTheClassAgain()
        {
            _menu.PressNewGame();
            _sinners.Choose(0);
            _chapters.HasRun = false;
            _chapters.AskForNewRun();
            Assert.IsTrue(_sinners.IsVisible);
        }

        [Test]
        public void F9_OpensTheBossSimulator_TheOldDemo_AndBack()
        {
            _presenter.ToggleSimulator();
            Assert.IsTrue(_presenter.InSimulator);
            Assert.IsTrue(_menu.Simulator, "the menu says so");
            _menu.PressNewGame();
            Assert.IsTrue(_dealers.IsVisible, "the simulator's new run: a demon first");
            Assert.IsFalse(_sinners.IsVisible);
            _dealers.PressBack();
            _presenter.ToggleSimulator();
            Assert.IsFalse(_presenter.InSimulator);
            _menu.PressNewGame();
            Assert.IsTrue(_sinners.IsVisible, "the game again");
        }

        [Test]
        public void HELLPOKER_DEV_OrABatchRun_OpensTheSimulator_ThePlayersGameOtherwise()
        {
            Assert.IsFalse(HellPokerBootstrap.StartsInSimulator(null, false, null), "a player's game: the game");
            Assert.IsFalse(HellPokerBootstrap.StartsInSimulator("0", false, null));
            Assert.IsTrue(HellPokerBootstrap.StartsInSimulator("1", false, null), "HELLPOKER_DEV=1");
            Assert.IsTrue(HellPokerBootstrap.StartsInSimulator(null, true, null), "the old demo's tests");
            Assert.IsFalse(HellPokerBootstrap.StartsInSimulator("1", true, false), "a test that asks for the game");
        }

        [Test]
        public void TheDevFlag_StartsInTheSimulator()
        {
            Build(simulator: true);
            Assert.IsTrue(_presenter.InSimulator);
            Assert.IsTrue(_menu.Simulator);
        }

        [Test]
        public void Records_ShowTheGamesBook_TheSimulatorsOwnInTheSimulator()
        {
            _menu.PressRecords();
            Assert.IsNotNull(_records.Lines, "the game's records");
            Assert.IsNull(_records.Phase2);
            _records.PressBack();
            _presenter.ToggleSimulator();
            _menu.PressRecords();
            Assert.AreEqual(DealerRoster.All.Count, _records.DealersShown, "the simulator's records: the demo's book");
        }

        [Test]
        public void TheSimulatorsRun_IsNeverTouchedByTheGame()
        {
            _menu.PressNewGame();
            _sinners.Choose(1);
            _chapters.AskForMenu();
            _menu.PressNewGame();
            _menu.Confirm();
            _sinners.Choose(0);
            Assert.AreEqual(0, _session.NewRuns);
            Assert.AreEqual(0, _session.Abandoned, "the simulator's save stays");
        }
    }
}