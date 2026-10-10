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
            public bool IsVisible { get; private set; }
            public string MusicId => "mammon";
            public SinnerClass Started { get; private set; }
            public event Action MenuRequested;
            public event Action NewRunRequested;

            public void Start(SinnerClass sinnerClass)
            {
                Started = sinnerClass;
                HasRun = true;
            }

            public void Show() => IsVisible = true;
            public void Hide() => IsVisible = false;
            public void AskForMenu() => MenuRequested?.Invoke();
            public void AskForNewRun() => NewRunRequested?.Invoke();
        }

        private MainMenuPresenterTests.FakeMenuView _menu;
        private MainMenuPresenterTests.FakeSession _session;
        private FakeSinnerSelect _sinners;
        private FakeChapters _chapters;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _menu = new MainMenuPresenterTests.FakeMenuView();
            _session = new MainMenuPresenterTests.FakeSession();
            _sinners = new FakeSinnerSelect();
            _chapters = new FakeChapters();
            _presenter = new MainMenuPresenter(_menu, new MainMenuPresenterTests.FakeDealerSelectView(), new MainMenuPresenterTests.FakeSettingsView(),
                new MainMenuPresenterTests.FakeEndScreen(), new MainMenuPresenterTests.FakeRecords(), new FakeTableView(), _session,
                new MainMenuPresenterTests.FakeQuitter(), new MainMenuPresenterTests.FakeTransition(), DealerRoster.All, null, _sinners,
                SinnerRoster.All, null, _chapters);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        [Test]
        public void ThePhaseTwoButtonShowsWhenTheChaptersAreThere()
        {
            Assert.IsTrue(_menu.ChaptersAvailable);
            Assert.IsFalse(_menu.ChaptersInProgress);
        }

        [Test]
        public void ThePhaseTwoButtonAsksForAClassThenOpensTheChapter()
        {
            _menu.PressChapters();
            Assert.IsTrue(_sinners.IsVisible);
            Assert.AreEqual(DealerRoster.MammonId, _sinners.DealerId, "Mammon's hall behind the choice: the first chapter is his");
            _sinners.Choose(2);
            Assert.AreSame(SinnerRoster.All[2], _chapters.Started);
            Assert.IsTrue(_chapters.IsVisible);
            Assert.AreEqual(0, _session.NewRuns, "the demo's run is not touched");
        }

        [Test]
        public void BackFromTheClassChoiceIsTheMenu()
        {
            _menu.PressChapters();
            _sinners.Back();
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsNull(_chapters.Started);
        }

        [Test]
        public void AChapterRunInProgressIsGoneBackTo()
        {
            _menu.PressChapters();
            _sinners.Choose(0);
            _chapters.AskForMenu();
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_chapters.IsVisible);
            Assert.IsTrue(_menu.ChaptersInProgress);
            _menu.PressChapters();
            Assert.IsTrue(_chapters.IsVisible, "straight back to the chapter, no class choice");
            Assert.IsFalse(_sinners.IsVisible);
        }

        [Test]
        public void ANewChapterRunAsksForTheClassAgain()
        {
            _menu.PressChapters();
            _sinners.Choose(0);
            _chapters.HasRun = false;
            _chapters.AskForNewRun();
            Assert.IsTrue(_sinners.IsVisible);
        }
    }
}
