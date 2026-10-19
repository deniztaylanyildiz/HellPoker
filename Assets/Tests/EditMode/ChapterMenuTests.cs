using System;
using System.Collections.Generic;
using System.Linq;
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
            public event Action<int, int> CircleStepped;
            public bool IsVisible { get; private set; }
            public string DealerId { get; private set; }
            public IReadOnlyList<SinnerCard> Cards { get; private set; }
            public string Note { get; private set; }

            public void Show(IReadOnlyList<SinnerCard> sinners, string dealerId, string note = null)
            {
                IsVisible = true;
                DealerId = dealerId;
                Cards = sinners;
                Note = note;
            }

            public void Step(int index, int delta) => CircleStepped?.Invoke(index, delta);

            public void Hide() => IsVisible = false;
            public void Choose(int index) => SinnerChosen?.Invoke(index);
            public void Back() => BackPressed?.Invoke();
        }

        private sealed class FakeChapters : IChapterSession
        {
            public bool HasRun { get; set; }
            public bool CanContinue => HasRun;
            public ContinueCard ContinueCard => HasRun ? new ContinueCard(Core.Sinners.Peasant.ClassId, 1, 2, 90, 6500, 0) : null;
            public Core.Chapters.ChapterRecords Records { get; } = new Core.Chapters.ChapterRecords();
            public bool IsVisible { get; private set; }
            public string MusicId => "mammon";
            public FeedbackContext Feedback => null;
            public SinnerClass Started { get; private set; }
            public int StartedCircle { get; private set; }
            public int Abandoned { get; private set; }
            public event Action MenuRequested;
            public event Action NewRunRequested;

            public void Start(SinnerClass sinnerClass) => Start(sinnerClass, 0);

            public void Start(SinnerClass sinnerClass, int circle)
            {
                Started = sinnerClass;
                StartedCircle = circle;
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

        private void Build(bool simulator, bool simulatorAllowed = true, bool simulatorSaved = false, bool gameSaved = false)
        {
            _presenter?.Dispose();
            _menu = new MainMenuPresenterTests.FakeMenuView();
            _session = new MainMenuPresenterTests.FakeSession { CanContinue = simulatorSaved };
            _records = new MainMenuPresenterTests.FakeRecords();
            _dealers = new MainMenuPresenterTests.FakeDealerSelectView();
            _sinners = new FakeSinnerSelect();
            _chapters = new FakeChapters { HasRun = gameSaved };
            _presenter = new MainMenuPresenter(_menu, _dealers, new MainMenuPresenterTests.FakeSettingsView(),
                new MainMenuPresenterTests.FakeEndScreen(), _records, new FakeTableView(), _session,
                new MainMenuPresenterTests.FakeQuitter(), new MainMenuPresenterTests.FakeTransition(), DealerRoster.All, null, _sinners,
                SinnerRoster.All, null, _chapters, simulator, simulatorAllowed);
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

        // ------------------------------------------------------------------ the Summoner's lock and the circles of Hell

        [Test]
        public void TheGamesClassChoice_HasSixClasses_AllOpenFromTheStart()
        {
            _menu.PressNewGame();
            Assert.AreEqual(6, _sinners.Cards.Count, "the four, the Summoner and the Thief");
            Assert.IsTrue(_sinners.Cards.All(c => !c.Locked), "Demo 2.2: every class open, to try them all");
            Assert.AreEqual(Summoner.ClassId, _sinners.Cards[4].Id);
            Assert.AreEqual(Thief.ClassId, _sinners.Cards[5].Id);
            _sinners.Choose(5);
            Assert.AreEqual(Thief.ClassId, _chapters.Started.Id);
        }

        [Test]
        public void ACircleOpensForAClass_WithEachFreedom_AndIsChosenOnItsCard()
        {
            _menu.PressNewGame();
            Assert.AreEqual(0, _sinners.Cards[0].CirclesOpen, "no circle before a freedom");
            _sinners.Step(0, 1);
            Assert.AreEqual(0, _sinners.Cards[0].Circle);

            _chapters.Records.RunEnded(Core.Chapters.JourneyEnd.Freed, 4, Peasant.ClassId, 100, circle: 0);
            _chapters.Records.RunEnded(Core.Chapters.JourneyEnd.Freed, 4, Peasant.ClassId, 100, circle: 1);
            _sinners.Back();
            _menu.PressNewGame();
            Assert.AreEqual(2, _sinners.Cards[0].CirclesOpen, "free in the first circle: the second opens");
            Assert.AreEqual(0, _sinners.Cards[1].CirclesOpen, "for that class only");
            _sinners.Step(0, 1);
            _sinners.Step(0, 1);
            _sinners.Step(0, 1);
            Assert.AreEqual(2, _sinners.Cards[0].Circle, "never past what is open");
            Assert.AreEqual(Presentation.Ui.UiText.CircleNote(2), _sinners.Note, "its rules under the title");
            _sinners.Choose(0);
            Assert.AreEqual(2, _chapters.StartedCircle);
        }

        [Test]
        public void APlayersBuild_HasNoSimulator_F9DoesNothing_AndASimulatorSaveNeverBecomesContinue()
        {
            // Masa birliği: a player's build (no editor, no development build, no HELLPOKER_DEV, no batch run, no tour).
            Assert.IsFalse(HellPokerBootstrap.SimulatorAllowed(false, false, null, false, false));
            Assert.IsTrue(HellPokerBootstrap.SimulatorAllowed(true, false, null, false, false), "a development build");
            Assert.IsTrue(HellPokerBootstrap.SimulatorAllowed(false, true, null, false, false), "the editor");
            Assert.IsTrue(HellPokerBootstrap.SimulatorAllowed(false, false, "1", false, false), "HELLPOKER_DEV=1");
            Assert.IsTrue(HellPokerBootstrap.SimulatorAllowed(false, false, null, true, false), "the tests");
            Assert.IsTrue(HellPokerBootstrap.SimulatorAllowed(false, false, null, false, true), "the FPS tour");

            // An old simulator run in run.save: the game's menu has nothing to continue, and F9 does not open the simulator.
            Build(simulator: true, simulatorAllowed: false, simulatorSaved: true);
            Assert.IsFalse(_presenter.InSimulator, "never the simulator in a player's build");
            Assert.IsFalse(_menu.Simulator, "no trace of it on the menu");
            Assert.IsFalse(_menu.ContinueShown, "the simulator's save is not the game's");
            _presenter.ToggleSimulator();
            Assert.IsFalse(_presenter.InSimulator, "F9 does nothing");
            Assert.IsFalse(_menu.Simulator);

            // The game's own save: CONTINUE leads to it, the simulator's save untouched.
            Build(simulator: false, simulatorAllowed: false, simulatorSaved: true, gameSaved: true);
            Assert.IsTrue(_menu.ContinueShown);
            _menu.PressContinue();
            Assert.IsTrue(_chapters.IsVisible, "the game's run");
            Assert.AreEqual(0, _session.NewRuns);
        }

        [Test]
        public void TheMenusCard_IsTheRunThatWaits_AndNoneWithoutOne()
        {
            Assert.IsNull(_menu.Card, "no run, no card");
            _menu.PressNewGame();
            _sinners.Choose(0);
            _chapters.AskForMenu();
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsNotNull(_menu.Card, "the card beside CONTINUE");
            Assert.AreEqual(Peasant.ClassId, _menu.Card.ClassId);
            Assert.AreEqual(90, _menu.Card.Coins);
        }

        [Test]
        public void TheArrowsAndEnter_ReachTheMenusCursor_OnlyOnItsRoot()
        {
            _presenter.MoveMenuSelection(1, 0);
            _presenter.PressMenuSelection();
            CollectionAssert.AreEqual(new[] { (1, 0) }, _menu.Moves);
            Assert.AreEqual(1, _menu.Presses);

            _menu.PressNewGame();   // the class choice is open: the menu's cursor is not there
            _presenter.MoveMenuSelection(0, 1);
            _presenter.PressMenuSelection();
            Assert.AreEqual(1, _menu.Moves.Count);
            Assert.AreEqual(1, _menu.Presses);
        }
    }
}