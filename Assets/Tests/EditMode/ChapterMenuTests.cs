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
    /// <summary>The title menu's door to the chapters: the class first (the difficulty under it), then the chapter.</summary>
    public class ChapterMenuTests
    {
        private MainMenuPresenterTests.FakeMenuView _menu;
        private MainMenuPresenterTests.FakeRecords _records;
        private MainMenuPresenterTests.FakeSinnerSelect _sinners;
        private MainMenuPresenterTests.FakeChapters _chapters;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _menu = new MainMenuPresenterTests.FakeMenuView();
            _records = new MainMenuPresenterTests.FakeRecords();
            _sinners = new MainMenuPresenterTests.FakeSinnerSelect();
            _chapters = new MainMenuPresenterTests.FakeChapters();
            _presenter = new MainMenuPresenter(_menu, new MainMenuPresenterTests.FakeSettingsView(), _records, new MainMenuPresenterTests.FakeQuitter(),
                new MainMenuPresenterTests.FakeTransition(), _sinners, _chapters);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        [Test]
        public void NewGame_AsksForAClass_ThenOpensTheChapter()
        {
            _menu.PressNewGame();
            Assert.IsTrue(_sinners.IsVisible, "no demon to choose: the game starts with Mammon's chapter");
            Assert.AreEqual(DealerRoster.MammonId, _sinners.DealerId, "Mammon's hall behind the choice: the first chapter is his");
            _sinners.Choose(2);
            Assert.AreSame(SinnerRoster.PhaseTwo[2], _chapters.Started);
            Assert.IsTrue(_chapters.IsVisible, "the map");
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

        // ------------------------------------------------------------------ the classes and the difficulty

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
        public void TheDifficultyRow_StartsAtMedium_StepsAndLocks_AndTheRunGetsIt()
        {
            Core.Chapters.ChapterUnlocks.AllClassesOpen = false;
            try
            {
                _menu.PressNewGame();
                Assert.IsNotNull(_sinners.Difficulty, "the row under the cards");
                Assert.AreEqual((int)Core.Chapters.Difficulty.Medium, _sinners.Difficulty.Level, "Medium at first");
                Assert.IsTrue(_sinners.Difficulty.Open);
                _sinners.Step(1);
                Assert.AreEqual((int)Core.Chapters.Difficulty.Hard, _sinners.Difficulty.Level);
                Assert.IsFalse(_sinners.Difficulty.Open, "Hard is locked before a Medium freedom");
                StringAssert.Contains(Presentation.Ui.UiText.DifficultyUnlockText(Core.Chapters.Difficulty.Hard), _sinners.Difficulty.Hover, "its hover says what opens it");
                _sinners.Choose(0);
                Assert.IsNull(_chapters.Started, "a locked level starts nothing");
                Assert.AreEqual(Presentation.Ui.UiText.DifficultyLockedHover(Core.Chapters.Difficulty.Hard), _sinners.Note);

                _chapters.Records.RunEnded(Core.Chapters.JourneyEnd.Freed, 4, King.ClassId, 100, Core.Chapters.Difficulty.Medium);
                _sinners.Step(1);
                _sinners.Step(-1);
                Assert.IsTrue(_sinners.Difficulty.Open, "a Medium freedom by any class opens Hard");
                _sinners.Step(-1);
                _sinners.Step(-1);
                _sinners.Step(-1);
                Assert.AreEqual((int)Core.Chapters.Difficulty.Easy, _sinners.Difficulty.Level, "never below Easy");
                Assert.IsFalse(_sinners.Difficulty.CanStepDown);
                _sinners.Step(1);
                _sinners.Step(1);
                _sinners.Choose(0);
                Assert.AreEqual(Core.Chapters.Difficulty.Hard, _chapters.StartedDifficulty);
            }
            finally
            {
                Core.Chapters.ChapterUnlocks.AllClassesOpen = true;
            }
        }

        [Test]
        public void IronmanProgressSinceTheLastCamp_QuitAsksFirst()
        {
            _chapters.HasRun = true;
            _chapters.IronmanUnsaved = true;
            _menu.PressQuit();
            Assert.IsTrue(_menu.IsConfirming, "a warning first");
            Assert.AreEqual(Presentation.Ui.UiText.IronmanQuitWarning, _menu.Warning);
            _chapters.IronmanUnsaved = false;
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