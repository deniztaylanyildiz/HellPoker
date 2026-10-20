using System;
using System.Collections.Generic;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>The title menu and its screens (Demo 3.4.1: the game's only — the Boss Simulator, its demon choice and end screen are gone).</summary>
    public class MainMenuPresenterTests
    {
        internal sealed class FakeMenuView : IMainMenuView
        {
            public bool IsVisible { get; private set; }
            public bool ContinueShown { get; private set; }

            public event Action NewGamePressed;
            public event Action ContinuePressed;
            public event Action QuitPressed;
            public event Action SettingsPressed;
            public event Action RecordsPressed;
            public event Action Confirmed;

            /// <summary>Demo 3.3: the run that waits, as the card beside CONTINUE shows it (null: no card).</summary>
            public ContinueCard Card { get; private set; }
            public void SetContinueCard(ContinueCard card) => Card = card;

            /// <summary>The cursor's moves and presses the keys made (Demo 3.3).</summary>
            public List<(int dx, int dy)> Moves { get; } = new List<(int, int)>();
            public int Presses { get; private set; }
            public void MoveSelection(int dx, int dy) => Moves.Add((dx, dy));
            public void PressSelection() => Presses++;

            public void PressRecords() => RecordsPressed?.Invoke();

            public string Taunt { get; private set; }
            public string Warning { get; private set; }
            public bool IsConfirming { get; private set; }
            public bool IsShowingRules => RulesOpen;
            public int PagesTurned { get; private set; }
            public void TurnRulesPage(int step) => PagesTurned += step;

            public string ConfirmLabel { get; private set; }
            public string CancelLabel { get; private set; }

            public void AskToConfirm(string taunt, string warning, string confirmLabel, string cancelLabel = null)
            {
                Taunt = taunt;
                Warning = warning;
                ConfirmLabel = confirmLabel;
                CancelLabel = cancelLabel;
                IsConfirming = true;
            }

            public void Confirm()
            {
                IsConfirming = false;
                Confirmed?.Invoke();
            }

            /// <summary>The rules panel, open over the menu.</summary>
            public bool RulesOpen { get; set; }

            public void Show(bool canContinue)
            {
                IsVisible = true;
                ContinueShown = canContinue;
            }

            public bool CloseOverlay()
            {
                if (IsConfirming)
                {
                    IsConfirming = false;
                    return true;
                }
                if (!RulesOpen) return false;
                RulesOpen = false;
                return true;
            }

            public void PressSettings() => SettingsPressed?.Invoke();

            public void Hide() => IsVisible = false;

            public void PressNewGame() => NewGamePressed?.Invoke();
            public void PressContinue() => ContinuePressed?.Invoke();
            public void PressQuit() => QuitPressed?.Invoke();
        }

        internal sealed class FakeSinnerSelect : ISinnerSelectView
        {
            public event Action<int> SinnerChosen;
            public event Action BackPressed;
            public event Action<int> DifficultyStepped;
            public DifficultyChoice Difficulty { get; private set; }
            public bool IsVisible { get; private set; }
            public string DealerId { get; private set; }
            public IReadOnlyList<SinnerCard> Cards { get; private set; }
            public string Note { get; private set; }

            public void Show(IReadOnlyList<SinnerCard> sinners, string dealerId, string note = null, DifficultyChoice difficulty = null)
            {
                Difficulty = difficulty;
                IsVisible = true;
                DealerId = dealerId;
                Cards = sinners;
                Note = note;
            }

            public void Step(int delta) => DifficultyStepped?.Invoke(delta);

            public void Hide() => IsVisible = false;
            public void Choose(int index) => SinnerChosen?.Invoke(index);
            public void Back() => BackPressed?.Invoke();
        }

        internal sealed class FakeChapters : IChapterSession
        {
            public bool HasRun { get; set; }
            public bool CanContinue => HasRun;
            public ContinueCard ContinueCard => HasRun ? new ContinueCard(Peasant.ClassId, 1, 2, 90, 6500, Core.Chapters.Difficulty.Easy) : null;
            public bool IronmanUnsaved { get; set; }
            public Core.Chapters.ChapterRecords Records { get; } = new Core.Chapters.ChapterRecords();
            public bool IsVisible { get; private set; }
            public string MusicId => "mammon";
            public FeedbackContext Feedback => null;
            public SinnerClass Started { get; private set; }
            public Core.Chapters.Difficulty StartedDifficulty { get; private set; }
            public int Abandoned { get; private set; }
            public event Action MenuRequested;
            public event Action NewRunRequested;

            public void Start(SinnerClass sinnerClass) => Start(sinnerClass, Core.Chapters.Difficulty.Easy);

            public void Start(SinnerClass sinnerClass, Core.Chapters.Difficulty difficulty)
            {
                Started = sinnerClass;
                StartedDifficulty = difficulty;
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

        internal sealed class FakeQuitter : IApplicationQuitter
        {
            public bool QuitRequested { get; private set; }

            public void Quit() => QuitRequested = true;
        }

        internal sealed class FakeSettingsView : ISettingsView
        {
            public bool IsVisible { get; private set; }

            /// <summary>The last screen rendered.</summary>
            public SettingsScreen Screen { get; private set; }
            public string Speed => Screen?.Speed;
            public bool Fullscreen => Screen != null && Screen.Fullscreen;
            public bool HandGuide => Screen != null && Screen.HandGuide;
            public bool TipsLeft => Screen != null && Screen.TipsLeft;
            public string Music => Screen?.Music;
            public string SfxVolume => Screen?.Sfx;
            public string Master => Screen?.Master;
            public string Language => Screen?.Language;
            public SettingsTab Tab => Screen?.Tab ?? SettingsTab.Game;

            public event Action<SettingsTab> TabPressed;
            public event Action SpeedPressed;
            public event Action FullscreenPressed;
            public event Action WindowScalePressed;
            public event Action PixelScalePressed;
            public event Action VSyncPressed;
            public event Action HandGuidePressed;
            public event Action ResetTipsPressed;
            public event Action BackPressed;
            public event Action<int> LanguageChosen;
            public event Action MasterPressed;
            public event Action MusicPressed;
            public event Action SfxPressed;
            public void PressMusic() => MusicPressed?.Invoke();
            public void PressSfx() => SfxPressed?.Invoke();
            public void PressMaster() => MasterPressed?.Invoke();
            public void ChooseLanguage(int index) => LanguageChosen?.Invoke(index);
            public void PressTab(SettingsTab tab) => TabPressed?.Invoke(tab);
            public void PressWindowScale() => WindowScalePressed?.Invoke();
            public void PressPixelScale() => PixelScalePressed?.Invoke();
            public void PressVSync() => VSyncPressed?.Invoke();
            public event Action ScreenShakePressed;
            public void PressScreenShake() => ScreenShakePressed?.Invoke();
            public string ScreenShake => Screen?.ScreenShake;
            public event Action FourColorPressed;
            public void PressFourColor() => FourColorPressed?.Invoke();
            public bool FourColor => Screen != null && Screen.FourColor;

            public void Render(SettingsScreen screen) => Screen = screen;

            public void Show() => IsVisible = true;
            public void Hide() => IsVisible = false;

            public void PressSpeed() => SpeedPressed?.Invoke();
            public void PressFullscreen() => FullscreenPressed?.Invoke();
            public void PressHandGuide() => HandGuidePressed?.Invoke();
            public void PressResetTips() => ResetTipsPressed?.Invoke();
            public void PressBack() => BackPressed?.Invoke();
        }

        internal sealed class FakeRecords : IRecordsView
        {
            public bool IsVisible { get; private set; }
            public event Action BackPressed;

            /// <summary>The game's records, as last shown.</summary>
            public IReadOnlyList<string> Lines { get; private set; }

            public void ShowLines(IReadOnlyList<string> lines)
            {
                IsVisible = true;
                Lines = lines;
            }

            public void Hide() => IsVisible = false;
            public void PressBack() => BackPressed?.Invoke();
        }

        internal sealed class FakeTransition : IScreenTransition
        {
            public int Played { get; private set; }
            public bool IsPlaying { get; set; }
            public void Play() => Played++;
        }

        private FakeMenuView _menu;
        private FakeSettingsView _settings;
        private FakeTransition _transition;
        private FakeRecords _records;
        private FakeSinnerSelect _sinners;
        private FakeChapters _chapters;
        private FakeQuitter _quitter;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _menu = new FakeMenuView();
            _quitter = new FakeQuitter();
            _settings = new FakeSettingsView();
            _transition = new FakeTransition();
            _records = new FakeRecords();
            _sinners = new FakeSinnerSelect();
            _chapters = new FakeChapters();
            _presenter = new MainMenuPresenter(_menu, _settings, _records, _quitter, _transition, _sinners, _chapters);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        /// <summary>A run started from the menu (a Peasant), then the menu again (the chapter's MENU).</summary>
        private void RunThenMenu()
        {
            _menu.PressNewGame();
            _sinners.Choose(0);
            _chapters.AskForMenu();
        }

        [Test]
        public void GameStartsOnTheMenu_WithoutContinue()
        {
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_menu.ContinueShown);
            Assert.IsFalse(_sinners.IsVisible);
            Assert.IsFalse(_chapters.IsVisible);
        }

        [Test]
        public void TheMenuNeedsTheGamesScreens()
        {
            Assert.Throws<ArgumentNullException>(() => new MainMenuPresenter(_menu, _settings, _records, _quitter, _transition, _sinners, null));
            Assert.Throws<ArgumentNullException>(() => new MainMenuPresenter(_menu, _settings, _records, _quitter, _transition, null, _chapters));
        }

        [Test]
        public void Escape_TogglesBetweenTheRunAndTheMenu()
        {
            RunThenMenu();

            _presenter.GoBack();
            Assert.IsFalse(_presenter.IsMenuOpen);
            Assert.IsTrue(_chapters.IsVisible);

            _presenter.GoBack();
            Assert.IsTrue(_presenter.IsMenuOpen);
            Assert.IsFalse(_chapters.IsVisible);
        }

        [Test]
        public void Escape_OnFirstMenu_StaysThere()
        {
            _presenter.GoBack();

            Assert.IsTrue(_presenter.IsMenuOpen);
            Assert.IsFalse(_chapters.IsVisible);
        }

        [Test]
        public void Escape_OnTheClassChoice_ReturnsToMenu()
        {
            _menu.PressNewGame();
            Assert.IsTrue(_presenter.IsMenuOpen, "The class choice is a menu screen.");

            _presenter.GoBack();

            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_sinners.IsVisible);
            Assert.IsNull(_chapters.Started);
        }

        // ------------------------------------------------------------------ Esc and sub-screens

        [Test]
        public void Settings_OpensFromTheMenu_AndBackReturns()
        {
            _menu.PressSettings();
            Assert.IsTrue(_settings.IsVisible);
            Assert.IsFalse(_menu.IsVisible);
            Assert.IsTrue(_presenter.IsMenuOpen);

            _settings.PressBack();
            Assert.IsFalse(_settings.IsVisible);
            Assert.IsTrue(_menu.IsVisible);
        }

        [Test]
        public void Escape_OnSettings_ReturnsToMenu()
        {
            _menu.PressSettings();

            _presenter.GoBack();

            Assert.IsFalse(_settings.IsVisible);
            Assert.IsTrue(_menu.IsVisible);
        }

        [Test]
        public void Escape_OnAnySettingsTab_ReturnsToMenu_AndTheTabsWaitForTheScreen()
        {
            using var tabs = new SettingsPresenter(new GameSettings(new MemoryStore()), _settings, new SettingsTests.FakeDisplay());
            Assert.IsFalse(tabs.IsSettingsOpen, "Q / E do nothing while the settings are closed.");
            _menu.PressSettings();
            Assert.IsTrue(tabs.IsSettingsOpen);

            tabs.NextTab();
            tabs.NextTab();
            Assert.AreEqual(SettingsTab.Sound, _settings.Tab);

            _presenter.GoBack();

            Assert.IsFalse(_settings.IsVisible, "Esc leaves the settings from any tab.");
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(tabs.IsSettingsOpen);
        }

        [Test]
        public void Escape_OnTheRules_ClosesThemFirst()
        {
            RunThenMenu();
            _menu.RulesOpen = true;

            _presenter.GoBack();
            Assert.IsFalse(_menu.RulesOpen);
            Assert.IsTrue(_menu.IsVisible, "Still on the menu, not back in the run yet.");

            _presenter.GoBack();
            Assert.IsTrue(_chapters.IsVisible);
        }

        [Test]
        public void EveryScreenChange_PlaysTheTransition()
        {
            int before = _transition.Played;
            _menu.PressNewGame();
            _sinners.Choose(0);
            _chapters.AskForMenu();
            _menu.PressSettings();

            Assert.AreEqual(before + 4, _transition.Played);
        }

        [Test]
        public void Records_OpenFromTheMenu_TheGamesBook_AndGoBack()
        {
            _menu.PressRecords();
            Assert.IsTrue(_records.IsVisible);
            Assert.IsNotNull(_records.Lines, "the game's records, line by line");
            CollectionAssert.Contains(_records.Lines, RecordsLayout.ColumnBreak, "two columns");

            _records.PressBack();
            Assert.IsTrue(_menu.IsVisible);
            Assert.IsFalse(_records.IsVisible);
        }

        [Test]
        public void TheMenuRoot_IsTheTitleMenuOnly()
        {
            Assert.IsTrue(_presenter.IsAtMenuRoot, "The title menu.");

            _menu.PressNewGame();
            Assert.IsFalse(_presenter.IsAtMenuRoot, "The class choice.");
            _sinners.Choose(0);
            Assert.IsFalse(_presenter.IsAtMenuRoot, "The run.");

            _chapters.AskForMenu();
            Assert.IsTrue(_presenter.IsAtMenuRoot);
            _menu.RulesOpen = true;
            Assert.IsFalse(_presenter.IsAtMenuRoot, "Not under the rules.");
            _menu.RulesOpen = false;
            _menu.PressNewGame();
            Assert.IsTrue(_menu.IsConfirming);
            Assert.IsFalse(_presenter.IsAtMenuRoot, "Not under a warning.");
        }

        // ------------------------------------------------------------------ New Game over a run, Quit

        [Test]
        public void Escape_OnTheNewGameWarning_ClosesIt_AndKeepsTheRun()
        {
            RunThenMenu();
            _menu.PressNewGame();
            Assert.AreEqual(Presentation.Ui.UiText.AbandonButton, _menu.ConfirmLabel);

            _presenter.GoBack();

            Assert.IsFalse(_menu.IsConfirming);
            Assert.IsTrue(_menu.IsVisible, "Still on the menu.");
            Assert.AreEqual(0, _chapters.Abandoned);
            _presenter.GoBack();
            Assert.IsTrue(_chapters.IsVisible, "The run goes on.");
        }

        [Test]
        public void NewGame_WithNoRun_GoesStraightToTheClassChoice()
        {
            _menu.PressNewGame();

            Assert.IsFalse(_menu.IsConfirming);
            Assert.IsTrue(_sinners.IsVisible);
        }

        [Test]
        public void NewGameWarning_StillAbandons_NotQuits()
        {
            RunThenMenu();
            _menu.PressNewGame();

            _menu.Confirm();

            Assert.AreEqual(1, _chapters.Abandoned);
            Assert.IsFalse(_quitter.QuitRequested);
        }

        [Test]
        public void Quit_AsksTheApplicationToQuit_TheRunIsSaved()
        {
            RunThenMenu();

            _menu.PressQuit();

            Assert.IsTrue(_quitter.QuitRequested, "the run is saved at every step: nothing to ask");
            Assert.IsFalse(_menu.IsConfirming);
        }

        [Test]
        public void Quit_WithIronmanProgressSinceTheCamp_AsksFirst_AndEscKeepsPlaying()
        {
            RunThenMenu();
            _chapters.IronmanUnsaved = true;

            _menu.PressQuit();

            Assert.IsFalse(_quitter.QuitRequested);
            Assert.IsTrue(_menu.IsConfirming);
            Assert.AreEqual(Presentation.Ui.UiText.IronmanQuitWarning, _menu.Warning);
            Assert.AreEqual(Presentation.Ui.UiText.Yes, _menu.ConfirmLabel);
            Assert.AreEqual(Presentation.Ui.UiText.No, _menu.CancelLabel);
            _presenter.GoBack();
            Assert.IsFalse(_menu.IsConfirming);
            Assert.IsFalse(_quitter.QuitRequested);

            _menu.PressQuit();
            _menu.Confirm();
            Assert.IsTrue(_quitter.QuitRequested);
            Assert.AreEqual(0, _chapters.Abandoned, "quitting is not a new game");
        }
    }
}
