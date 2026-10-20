using System;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Sinners;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Moves the player between the screens: the title menu, its sub-screens (settings, records), the class choice and the chapters.
    /// Menu → NEW GAME → the class (and the difficulty under it) → the chapter's map. Esc always goes one screen up (<see cref="GoBack"/>),
    /// and every screen change plays the transition curtain. Knows nothing about poker rules; the run itself is reached through
    /// <see cref="IChapterSession"/>. Demo 3.4.1: the Boss Simulator (the old demo's menu, its demon choice and end screen) is gone.
    /// </summary>
    public sealed class MainMenuPresenter : IMenuCommands, IDisposable
    {
        private readonly IMainMenuView _menu;
        private readonly ISettingsView _settings;
        private readonly IRecordsView _records;
        private readonly IApplicationQuitter _quitter;
        private readonly IScreenTransition _transition;
        private readonly ISinnerSelectView _sinnerSelect;
        private readonly IChapterSession _chapters;

        /// <summary>The music of the screens (the menu's theme, the chapter's own) and the curtain's whoosh.</summary>
        private readonly IAudio _audio;

        public MainMenuPresenter(IMainMenuView menu, ISettingsView settings, IRecordsView records, IApplicationQuitter quitter,
            IScreenTransition transition, ISinnerSelectView sinnerSelect, IChapterSession chapters, IAudio audio = null)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _records = records ?? throw new ArgumentNullException(nameof(records));
            _quitter = quitter ?? throw new ArgumentNullException(nameof(quitter));
            _transition = transition ?? throw new ArgumentNullException(nameof(transition));
            _sinnerSelect = sinnerSelect ?? throw new ArgumentNullException(nameof(sinnerSelect));
            _chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
            _audio = audio ?? NullAudio.Instance;

            _menu.NewGamePressed += OpenChaptersOrChoose;
            _menu.Confirmed += GoAhead;
            _menu.ContinuePressed += ContinueChapters;
            _menu.SettingsPressed += OpenSettings;
            _menu.RecordsPressed += OpenRecords;
            _menu.QuitPressed += AskToQuit;
            _settings.BackPressed += OpenMenu;
            _records.BackPressed += OpenMenu;
            _sinnerSelect.SinnerChosen += ChooseSinner;
            _sinnerSelect.BackPressed += OpenMenu;
            _sinnerSelect.DifficultyStepped += StepDifficulty;
            _chapters.MenuRequested += OpenMenu;
            _chapters.NewRunRequested += OpenSinnerChoice;
            Lang.Changed += OnLanguageChanged;

            OpenMenu();
        }

        public bool IsMenuOpen => _menu.IsVisible || _sinnerSelect.IsVisible || _settings.IsVisible || _records.IsVisible;

        public bool IsTransitioning => _transition.IsPlaying;

        public bool IsAtMenuRoot => _menu.IsVisible && !_menu.IsConfirming && !_menu.IsShowingRules;

        public bool IsShowingRules => _menu.IsVisible && _menu.IsShowingRules;

        public void TurnRulesPage(int step) => _menu.TurnRulesPage(step);

        public void GoBack()
        {
            if (_sinnerSelect.IsVisible || _settings.IsVisible || _records.IsVisible)
            {
                OpenMenu();
            }
            else if (_menu.IsVisible)
            {
                if (!_menu.CloseOverlay() && _chapters.CanContinue)
                    ContinueChapters();
            }
            else
            {
                OpenMenu();
            }
        }

        public void Dispose()
        {
            _menu.NewGamePressed -= OpenChaptersOrChoose;
            _menu.Confirmed -= GoAhead;
            _menu.ContinuePressed -= ContinueChapters;
            _menu.SettingsPressed -= OpenSettings;
            _menu.RecordsPressed -= OpenRecords;
            _menu.QuitPressed -= AskToQuit;
            _settings.BackPressed -= OpenMenu;
            _records.BackPressed -= OpenMenu;
            _sinnerSelect.SinnerChosen -= ChooseSinner;
            _sinnerSelect.BackPressed -= OpenMenu;
            _sinnerSelect.DifficultyStepped -= StepDifficulty;
            _chapters.MenuRequested -= OpenMenu;
            _chapters.NewRunRequested -= OpenSinnerChoice;
            Lang.Changed -= OnLanguageChanged;
        }

        // ------------------------------------------------------------------ the run

        /// <summary>NEW GAME: a new run (its class first) — over a run that waits only once the player agrees to lose it.</summary>
        private void OpenChaptersOrChoose()
        {
            if (!_chapters.CanContinue)
            {
                OpenSinnerChoice();
                return;
            }
            DealerText mammon = UiText.Dealer(DealerRoster.MammonId);
            string taunt = mammon?.Scorn == null ? null : UiText.Pick(mammon.Scorn, Environment.TickCount & int.MaxValue);
            _asking = Asking.NewRun;
            _menu.AskToConfirm(taunt, UiText.ChaptersAbandonWarning, UiText.AbandonButton);
        }

        /// <summary>CONTINUE: the run that waits (the saved one is loaded).</summary>
        private void ContinueChapters()
        {
            if (!_chapters.CanContinue) return;
            HideAll();
            _chapters.Continue();
            Curtain();
        }

        private void OpenChapters()
        {
            HideAll();
            _chapters.Show();
            Curtain();
        }

        /// <summary>What the open warning is about.</summary>
        private enum Asking { Nothing, Quit, NewRun }

        private Asking _asking;

        private void GoAhead()
        {
            Asking asking = _asking;
            _asking = Asking.Nothing;
            if (asking == Asking.Quit)
            {
                _quitter.Quit();
            }
            else if (asking == Asking.NewRun)
            {
                _chapters.Abandon();
                OpenSinnerChoice();
            }
        }

        /// <summary>
        /// Quit. The run is saved at every step (a hand left in the middle is lost on the next launch, as its rules say), so the game just
        /// goes — but an Ironman run is saved only at the camps (Demo 3.4): leaving now loses what was done since the last one, so it asks.
        /// </summary>
        private void AskToQuit()
        {
            if (_chapters.IronmanUnsaved)
            {
                _asking = Asking.Quit;
                _menu.AskToConfirm(null, UiText.IronmanQuitWarning, UiText.Yes, UiText.No);
                return;
            }
            _quitter.Quit();
        }

        // ------------------------------------------------------------------ the class choice and the difficulty

        /// <summary>Demo 3.4: the difficulty chosen under the class cards (Medium at first).</summary>
        private Difficulty _difficulty = DifficultyRules.Default;
        private string _difficultyNote;

        private bool DifficultyOpen(Difficulty d) => _chapters.Records?.IsDifficultyOpen(d) ?? d <= Difficulty.Medium;

        /// <summary>The difficulty row: its name, open or locked, its words (what it adds, or what opens it).</summary>
        private DifficultyChoice DifficultyRow()
        {
            Difficulty d = _difficulty;
            bool open = DifficultyOpen(d);
            return new DifficultyChoice((int)d, UiText.DifficultyName(d), open, open ? UiText.DifficultyHover(d) : UiText.DifficultyLockedHover(d),
                d > Difficulty.Easy, d < DifficultyRules.Hardest);
        }

        /// <summary>The chosen difficulty (tests).</summary>
        public Difficulty ChosenDifficulty => _difficulty;

        private static SinnerClass[] Classes => SinnerRoster.PhaseTwo.ToArray();

        /// <summary>A class card's hover box (Demo 2.3.1): the whole text the short card leaves out, and its price.</summary>
        private static string ClassCardHover(SinnerClass c)
        {
            string text = UiText.ChapterSinnerFull(c.Id);
            if (c.StartingJokers > 0 && c.JokerJackpot > 0)
                text += "\n" + string.Format(UiText.JokerJackpotPhaseTwoFormat, c.JokerJackpot, Jester.PhaseTwoJackpotFallsTo);
            return text + "\n" + UiText.ChapterSinnerDetailFull(c.Id);
        }

        private void OpenSinnerChoice() => OpenSinnerChoice(curtain: true);

        /// <summary>Who was the player, up there? The class cards with their Phase 2 powers and locks, over the first chapter's demon's hall;
        /// the difficulty under them.</summary>
        private void OpenSinnerChoice(bool curtain)
        {
            HideAll();
            ChapterRecords records = _chapters.Records;
            _sinnerSelect.Show(Classes.Select(c => new SinnerCard(c.Id, UiText.SinnerName(c.Id), UiText.SinnerTitle(c.Id),
                    UiText.ChapterSinnerAbility(c.Id), UiText.ChapterSinnerDetail(c.Id),
                    string.Format(UiText.ChapterSinnerStartFormat, BossShares.Total(c.Id), ChapterRules.StartingCoinsFor(c.Id)),
                    !ChapterUnlocks.IsClassOpen(c.Id, records), UiText.MilestoneText(ChapterUnlocks.SummonerMilestone), ClassCardHover(c))).ToArray(),
                DealerRoster.MammonId, _difficultyNote, DifficultyRow());
            if (curtain) Curtain();
        }

        /// <summary>The difficulty row's arrows: the level steps (a locked one shows what opens it); its rules show under the title.</summary>
        private void StepDifficulty(int delta)
        {
            if (!_sinnerSelect.IsVisible || delta == 0) return;
            _difficulty = DifficultyRules.Clamp((int)_difficulty + Math.Sign(delta));
            _difficultyNote = DifficultyOpen(_difficulty) ? UiText.DifficultyHover(_difficulty) : UiText.DifficultyLockedHover(_difficulty);
            OpenSinnerChoice(curtain: false);
        }

        private void ChooseSinner(int index)
        {
            SinnerClass[] classes = Classes;
            if (index < 0 || index >= classes.Length) return;
            if (!ChapterUnlocks.IsClassOpen(classes[index].Id, _chapters.Records)) return;   // locked: the card says why
            if (!DifficultyOpen(_difficulty))
            {
                // A locked level: the line under the title says what opens it.
                _difficultyNote = UiText.DifficultyLockedHover(_difficulty);
                OpenSinnerChoice(curtain: false);
                return;
            }
            _difficultyNote = null;
            _chapters.Start(classes[index], _difficulty);
            OpenChapters();
        }

        // ------------------------------------------------------------------ the menu and its screens

        /// <summary>
        /// The language changed: the screen that is open says the same in the new words (without a curtain). Fixed labels follow by
        /// themselves (<see cref="LocalizedText"/>).
        /// </summary>
        private void OnLanguageChanged()
        {
            if (_sinnerSelect.IsVisible)
            {
                OpenSinnerChoice(curtain: false);
            }
            else if (_records.IsVisible)
            {
                ShowRecords();
            }
            else if (_menu.IsVisible && _menu.IsConfirming)
            {
                if (_asking == Asking.Quit) AskToQuit();
                else if (_asking == Asking.NewRun) OpenChaptersOrChoose();
            }
        }

        /// <summary>The curtain over a screen change, with its whoosh; the music follows the screen: the chapter's, else the menu's theme.</summary>
        private void Curtain()
        {
            _transition.Play();
            _audio.PlaySfx(SfxIds.Transition);
            _audio.PlayMusic(_chapters.IsVisible ? _chapters.MusicId : SfxIds.MenuMusic);
            _audio.SetSoulLayer(false);
        }

        /// <summary>Every screen goes away; the caller shows the one wanted.</summary>
        private void HideAll()
        {
            _menu.Hide();
            _sinnerSelect.Hide();
            _settings.Hide();
            _records.Hide();
            _chapters.Hide();
        }

        private void OpenRecords()
        {
            HideAll();
            ShowRecords();
            Curtain();
        }

        /// <summary>The game's records (the chapters' book): the runs left, the progress and the locks right.</summary>
        private void ShowRecords() =>
            _records.ShowLines(UiText.ChapterRecordLines(_chapters.Records).Concat(new[] { RecordsLayout.ColumnBreak })
                .Concat(UiText.ProgressRecordLines(_chapters.Records)).ToList());

        private void OpenMenu()
        {
            HideAll();
            bool canContinue = _chapters.CanContinue;
            _menu.Show(canContinue);
            // Demo 3.3: the run that waits, on a card beside CONTINUE.
            _menu.SetContinueCard(canContinue ? _chapters.ContinueCard : null);
            Curtain();
        }

        public void MoveMenuSelection(int dx, int dy)
        {
            if (IsAtMenuRoot) _menu.MoveSelection(dx, dy);
        }

        public void PressMenuSelection()
        {
            if (IsAtMenuRoot) _menu.PressSelection();
        }

        private void OpenSettings()
        {
            HideAll();
            _settings.Show();
            Curtain();
        }
    }
}
