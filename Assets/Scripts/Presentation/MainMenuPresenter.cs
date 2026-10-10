using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Sinners;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Moves the player between the screens: the title menu, its sub-screens (settings), the dealer choice and the table.
    /// Menu → New Game → choose a demon → table. During a run the same choice screen changes tables: the sentence goes
    /// along, and sitting with a demon whose soul line is already passed needs the player to confirm a warning.
    /// Esc always goes one screen up (<see cref="GoBack"/>), and every screen change plays the transition curtain.
    /// Knows nothing about poker rules; the run itself is reached through <see cref="IRunSession"/>.
    /// </summary>
    public sealed class MainMenuPresenter : IMenuCommands, IDisposable
    {
        private readonly IMainMenuView _menu;
        private readonly IDealerSelectView _dealerSelect;
        private readonly ISettingsView _settings;
        private readonly IEndScreenView _endScreen;
        private readonly IRecordsView _records;
        private readonly ITableView _table;
        private readonly IRunSession _session;
        private readonly IApplicationQuitter _quitter;
        private readonly IScreenTransition _transition;
        private readonly Dealer[] _dealers;
        private readonly Dealer _finalDealer;

        /// <summary>The demons in words (names, titles, house rules): built again when the language changes.</summary>
        private DealerCard[] _dealerCards;

        /// <summary>Lucifer's card: last on the choice screen, locked; null when there is no Lucifer.</summary>
        private DealerCard _finalCard;

        /// <summary>The choice screen as last shown (new run or changing tables), to show again in another language.</summary>
        private bool _choiceForTables;
        private RunSummary _lastSummary;

        private bool _changingTables;

        /// <summary>The class choice of a new run (after the demon); null: every new run is a Peasant's.</summary>
        private readonly ISinnerSelectView _sinnerSelect;

        /// <summary>The music of the screens (the menu's theme, the demon's at the table) and the curtain's whoosh.</summary>
        private readonly IAudio _audio;
        private readonly SinnerClass[] _classes;

        /// <summary>The demon chosen for the new run, while the class is being chosen.</summary>
        private Dealer _pendingDealer;
        private int _pendingSeat = -1;

        /// <summary>Phase 2's chapters (a test build's), apart from the demo's run; null: none.</summary>
        private readonly IChapterSession _chapters;

        /// <summary>The class choice open now is for a chapter run (not the demo's).</summary>
        private bool _chapterChoice;

        /// <param name="dealers">The demons the player may choose.</param>
        /// <param name="finalDealer">Lucifer, shown locked after them; he is never chosen, only met below the gate.</param>
        public MainMenuPresenter(IMainMenuView menu, IDealerSelectView dealerSelect, ISettingsView settings, IEndScreenView endScreen,
            IRecordsView records, ITableView table, IRunSession session, IApplicationQuitter quitter, IScreenTransition transition,
            IReadOnlyList<Dealer> dealers, Dealer finalDealer = null, ISinnerSelectView sinnerSelect = null,
            IReadOnlyList<SinnerClass> classes = null, IAudio audio = null, IChapterSession chapters = null)
        {
            _chapters = chapters;
            _audio = audio ?? NullAudio.Instance;
            _sinnerSelect = sinnerSelect;
            _classes = (classes ?? SinnerRoster.All).ToArray();
            _finalDealer = finalDealer;
            _finalCard = finalDealer == null ? null : DealerCards.Describe(finalDealer);
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _dealerSelect = dealerSelect ?? throw new ArgumentNullException(nameof(dealerSelect));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _endScreen = endScreen ?? throw new ArgumentNullException(nameof(endScreen));
            _records = records ?? throw new ArgumentNullException(nameof(records));
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _quitter = quitter ?? throw new ArgumentNullException(nameof(quitter));
            _transition = transition ?? throw new ArgumentNullException(nameof(transition));
            if (dealers == null || dealers.Count == 0) throw new ArgumentException("At least one dealer is needed.", nameof(dealers));
            _dealers = dealers.ToArray();
            _dealerCards = _dealers.Select(DealerCards.Describe).ToArray();

            _menu.NewGamePressed += AskForNewGame;
            _menu.Confirmed += GoAhead;
            _menu.ContinuePressed += OpenTable;
            _menu.ChangeTablePressed += AskToChangeTables;
            _menu.SettingsPressed += OpenSettings;
            _menu.RecordsPressed += OpenRecords;
            _menu.QuitPressed += AskToQuit;
            _dealerSelect.DealerChosen += Choose;
            _dealerSelect.BackPressed += Back;
            _dealerSelect.SeatConfirmed += ConfirmSeat;
            _dealerSelect.SeatCancelled += CancelSeat;
            _settings.BackPressed += OpenMenu;
            _records.BackPressed += OpenMenu;
            _endScreen.NewGamePressed += OpenNewRunChoice;
            _endScreen.MenuPressed += OpenMenu;
            _table.MenuPressed += OpenMenu;
            _session.LeaveRequested += OpenTableChoice;
            _session.RunEnded += ShowEnd;
            Lang.Changed += OnLanguageChanged;
            if (_sinnerSelect != null)
            {
                _sinnerSelect.SinnerChosen += ChooseSinner;
                _sinnerSelect.BackPressed += BackFromSinners;
            }
            _menu.ChaptersPressed += OpenChaptersOrChoose;
            _menu.ChaptersContinuePressed += ContinueChapters;
            if (_chapters != null)
            {
                _chapters.MenuRequested += OpenMenu;
                _chapters.NewRunRequested += OpenChapterSinnerChoice;
            }

            OpenMenu();
        }

        public bool IsMenuOpen => _menu.IsVisible || _dealerSelect.IsVisible || (_sinnerSelect?.IsVisible ?? false) || _settings.IsVisible || _endScreen.IsVisible || _records.IsVisible;

        public bool IsTransitioning => _transition.IsPlaying;

        public bool IsAtMenuRoot => _menu.IsVisible && !_menu.IsConfirming && !_menu.IsShowingRules;

        public void GoBack()
        {
            if (_sinnerSelect != null && _sinnerSelect.IsVisible)
            {
                BackFromSinners();
            }
            else if (_dealerSelect.IsVisible)
            {
                if (_dealerSelect.IsConfirming)
                {
                    _dealerSelect.CloseConfirm();
                    CancelSeat();
                }
                else
                {
                    Back();
                }
            }
            else if (_settings.IsVisible || _records.IsVisible || _endScreen.IsVisible)
            {
                OpenMenu();
            }
            else if (_menu.IsVisible)
            {
                if (!_menu.CloseOverlay() && _session.CanContinue)
                    OpenTable();
            }
            else
            {
                OpenMenu();
            }
        }

        public void Dispose()
        {
            _menu.NewGamePressed -= AskForNewGame;
            _menu.Confirmed -= GoAhead;
            _menu.ContinuePressed -= OpenTable;
            _menu.ChangeTablePressed -= AskToChangeTables;
            _menu.SettingsPressed -= OpenSettings;
            _menu.RecordsPressed -= OpenRecords;
            _menu.QuitPressed -= AskToQuit;
            _dealerSelect.DealerChosen -= Choose;
            _dealerSelect.BackPressed -= Back;
            _dealerSelect.SeatConfirmed -= ConfirmSeat;
            _dealerSelect.SeatCancelled -= CancelSeat;
            _settings.BackPressed -= OpenMenu;
            _records.BackPressed -= OpenMenu;
            _endScreen.NewGamePressed -= OpenNewRunChoice;
            _endScreen.MenuPressed -= OpenMenu;
            _table.MenuPressed -= OpenMenu;
            _session.LeaveRequested -= OpenTableChoice;
            _session.RunEnded -= ShowEnd;
            Lang.Changed -= OnLanguageChanged;
            if (_sinnerSelect != null)
            {
                _sinnerSelect.SinnerChosen -= ChooseSinner;
                _sinnerSelect.BackPressed -= BackFromSinners;
            }
            _menu.ChaptersPressed -= OpenChaptersOrChoose;
            _menu.ChaptersContinuePressed -= ContinueChapters;
            if (_chapters != null)
            {
                _chapters.MenuRequested -= OpenMenu;
                _chapters.NewRunRequested -= OpenChapterSinnerChoice;
            }
        }

        // ------------------------------------------------------------------ Phase 2's chapters

        /// <summary>The Phase 2 button: a new run (its class first) — over a run that waits only once the player agrees to lose it.</summary>
        private void OpenChaptersOrChoose()
        {
            if (_chapters == null) return;
            if (!_chapters.CanContinue)
            {
                OpenChapterSinnerChoice();
                return;
            }
            DealerText mammon = UiText.Dealer(DealerRoster.MammonId);
            string taunt = mammon?.Scorn == null ? null : UiText.Pick(mammon.Scorn, Environment.TickCount & int.MaxValue);
            _asking = Asking.NewChapterRun;
            _menu.AskToConfirm(taunt, UiText.ChaptersAbandonWarning, UiText.AbandonButton);
        }

        /// <summary>Phase 2's records in a line for the records screen; null without the chapters or before any run.</summary>
        private string Phase2Records()
        {
            Core.Chapters.ChapterRecords r = _chapters?.Records;
            if (r == null || r.Runs == 0) return null;
            return string.Format(UiText.RecordsPhase2Format, r.Runs, r.Freed, r.Damned, r.LuciferReached,
                r.FastestFreedom.HasValue ? r.FastestFreedom.Value.ToString() : "—");
        }

        /// <summary>Phase 2's CONTINUE: the run that waits (the saved one is loaded).</summary>
        private void ContinueChapters()
        {
            if (_chapters == null || !_chapters.CanContinue) return;
            HideAll();
            _chapters.Continue();
            Curtain();
        }

        /// <summary>The class choice for a chapter run, over the first chapter's demon's hall.</summary>
        private void OpenChapterSinnerChoice()
        {
            if (_chapters == null || _sinnerSelect == null) return;
            _chapterChoice = true;
            _pendingDealer = _dealers.FirstOrDefault(d => d.Id == DealerRoster.MammonId) ?? _dealers[0];
            OpenSinnerChoice(curtain: true);
        }

        private void OpenChapters()
        {
            HideAll();
            _chapters.Show();
            Curtain();
        }

        /// <summary>BACK on the class choice: the demon choice for a demo run, the menu for a chapter run.</summary>
        private void BackFromSinners()
        {
            if (_chapterChoice)
            {
                _chapterChoice = false;
                _pendingDealer = null;
                OpenMenu();
                return;
            }
            OpenNewRunChoice();
        }

        /// <summary>
        /// The language changed: the demons are described again, and the screen that is open says the same in the new words
        /// (without a curtain). Fixed labels follow by themselves (<see cref="LocalizedText"/>).
        /// </summary>
        private void OnLanguageChanged()
        {
            _dealerCards = _dealers.Select(DealerCards.Describe).ToArray();
            _finalCard = _finalDealer == null ? null : DealerCards.Describe(_finalDealer);

            if (_sinnerSelect != null && _sinnerSelect.IsVisible)
            {
                OpenSinnerChoice(curtain: false);
            }
            else if (_dealerSelect.IsVisible)
            {
                if (_choiceForTables)
                    OpenTableChoice(curtain: false);
                else
                    OpenNewRunChoice(curtain: false);
            }
            else if (_records.IsVisible)
            {
                _records.Show(_session.Records, _dealerCards, Phase2Records());
            }
            else if (_endScreen.IsVisible && _lastSummary != null)
            {
                _endScreen.Show(_lastSummary);
            }
            else if (_menu.IsVisible && _menu.IsConfirming)
            {
                if (_asking == Asking.Quit) AskToQuit();
                else AskForNewGame();
            }
        }

        /// <summary>
        /// New Game from the menu. With a run in progress the player is never held — but asked first, told what walking
        /// away costs, and mocked for it in the demon's own voice.
        /// </summary>
        private void AskForNewGame()
        {
            AbandonRisk risk = _session.AbandonRisk;
            if (risk == AbandonRisk.None)
            {
                OpenNewRunChoice();
                return;
            }

            string id = _session.CurrentDealerId;
            DealerText dealer = id == null ? null : UiText.Dealer(id);
            string taunt = dealer?.Scorn == null ? null : UiText.Pick(dealer.Scorn, Environment.TickCount & int.MaxValue);
            string warning = risk == AbandonRisk.Soul ? UiText.AbandonSoulWarning
                : risk == AbandonRisk.Hand ? UiText.AbandonHandWarning
                : UiText.AbandonRunWarning;
            _asking = Asking.NewGame;
            _menu.AskToConfirm(taunt, warning, UiText.AbandonButton);
        }

        /// <summary>What the open warning is about.</summary>
        private enum Asking { Nothing, NewGame, Quit, NewChapterRun }

        private Asking _asking;

        private void GoAhead()
        {
            Asking asking = _asking;
            _asking = Asking.Nothing;
            if (asking == Asking.Quit)
                _quitter.Quit();
            else if (asking == Asking.NewGame)
                AbandonAndChoose();
            else if (asking == Asking.NewChapterRun)
            {
                _chapters?.Abandon();
                OpenChapterSinnerChoice();
            }
        }

        /// <summary>
        /// Quit. Between hands it just goes (the run is saved); mid-hand — or with the soul on the table — the player is told
        /// first that the hand left behind is lost (it is forfeited on the next launch), in the demon's own voice.
        /// </summary>
        private void AskToQuit()
        {
            AbandonRisk risk = _session.AbandonRisk;
            if (risk != AbandonRisk.Hand && risk != AbandonRisk.Soul)
            {
                _quitter.Quit();
                return;
            }

            string id = _session.CurrentDealerId;
            DealerText dealer = id == null ? null : UiText.Dealer(id);
            string taunt = dealer?.Fled == null ? null : UiText.Pick(dealer.Fled, Environment.TickCount & int.MaxValue);
            _asking = Asking.Quit;
            _menu.AskToConfirm(taunt, UiText.QuitHandWarning, UiText.Quit);
        }

        private void AbandonAndChoose()
        {
            _session.AbandonRun();
            OpenNewRunChoice();
        }

        private void OpenNewRunChoice() => OpenNewRunChoice(curtain: true);

        private void OpenNewRunChoice(bool curtain)
        {
            _chapterChoice = false;
            _changingTables = false;
            _choiceForTables = false;
            ShowChoice(_dealers.Select(d => new DealerChoice(Card(d), soulAtStake: false, isCurrent: false)), curtain);
        }

        /// <summary>From the menu: go back to the table and ask there, so a soul-bound table answers in the dealer's voice.</summary>
        private void AskToChangeTables()
        {
            OpenTable();
            _session.RequestLeave();
        }

        private void OpenTableChoice() => OpenTableChoice(curtain: true);

        private void OpenTableChoice(bool curtain)
        {
            _changingTables = true;
            _choiceForTables = true;
            ShowChoice(_dealers.Select(d => new DealerChoice(Card(d), _session.WouldStakeSoul(d), d.Id == _session.CurrentDealerId)), curtain);
        }

        private DealerCard Card(Dealer dealer) => _dealerCards[Array.IndexOf(_dealers, dealer)];

        private void ShowChoice(IEnumerable<DealerChoice> choices, bool curtain = true)
        {
            _pendingSeat = -1;
            HideAll();
            if (_finalCard != null)
                choices = choices.Concat(new[] { new DealerChoice(_finalCard, soulAtStake: false, isCurrent: false, isLocked: true) });
            _dealerSelect.Show(choices.ToArray());
            if (curtain) Curtain();
        }

        /// <summary>The curtain over a screen change, with its whoosh; the music follows the screen: the demon's at the table,
        /// the menu's theme everywhere else.</summary>
        private void Curtain()
        {
            _transition.Play();
            _audio.PlaySfx(SfxIds.Transition);
            bool chapters = _chapters != null && _chapters.IsVisible;
            bool atTable = !IsMenuOpen && !chapters;
            _audio.PlayMusic(chapters ? _chapters.MusicId : atTable ? _session.CurrentDealerId : SfxIds.MenuMusic);
            _audio.SetSoulLayer(atTable && _session.AbandonRisk == AbandonRisk.Soul);
        }

        /// <summary>Every screen goes away; the caller shows the one wanted.</summary>
        private void HideAll()
        {
            _menu.Hide();
            _dealerSelect.Hide();
            _sinnerSelect?.Hide();
            _settings.Hide();
            _endScreen.Hide();
            _records.Hide();
            _table.SetVisible(false);
            _chapters?.Hide();
        }

        private void ShowEnd(RunSummary summary)
        {
            _lastSummary = summary;
            HideAll();
            _endScreen.Show(summary);
            Curtain();
        }

        private void OpenRecords()
        {
            HideAll();
            _records.Show(_session.Records, _dealerCards, Phase2Records());
            Curtain();
        }

        private void Choose(int index)
        {
            if (_finalCard != null && index == _dealers.Length)
            {
                // Nobody sits with him by choice: he answers from the dark.
                _dealerSelect.ShowLockedLine(index, UiText.Dealer(_finalCard.Id).NotYet);
                return;
            }

            if (index < 0 || index >= _dealers.Length) return;
            Dealer dealer = _dealers[index];

            if (!_changingTables)
            {
                if (_sinnerSelect == null)
                {
                    _session.StartNewRun(dealer);
                    OpenTable();
                    return;
                }
                _pendingDealer = dealer;
                OpenSinnerChoice(curtain: true);
                return;
            }

            if (dealer.Id == _session.CurrentDealerId)
            {
                OpenTable();
                return;
            }

            if (_session.WouldStakeSoul(dealer))
            {
                // A trap, left in on purpose: the player may still sit down, but only after a warning.
                _pendingSeat = index;
                _dealerSelect.AskToConfirm(UiText.Dealer(dealer.Id).SoulWarning);
                return;
            }

            _session.SwitchTable(dealer);
            OpenTable();
        }

        /// <summary>Who was the player, up there? The class cards, over the chosen demon's hall.</summary>
        private void OpenSinnerChoice(bool curtain)
        {
            HideAll();
            _sinnerSelect.Show(_classes.Select(c => new SinnerCard(c.Id, UiText.SinnerName(c.Id), UiText.SinnerTitle(c.Id),
                UiText.SinnerAbility(c.Id), UiText.SinnerDetail(c.Id), _chapterChoice
                    ? string.Format(UiText.ChapterSinnerStartFormat, Core.Chapters.BossShares.Total(c.Id), Core.Chapters.ChapterRules.StartingCoinsFor(c.Id))
                    : string.Format(UiText.SinnerStartFormat, c.StartingYears))).ToArray(),
                _pendingDealer?.Id);
            if (curtain) Curtain();
        }

        private void ChooseSinner(int index)
        {
            if (_pendingDealer == null || index < 0 || index >= _classes.Length) return;
            Dealer dealer = _pendingDealer;
            _pendingDealer = null;
            if (_chapterChoice)
            {
                _chapterChoice = false;
                _chapters.Start(_classes[index]);
                OpenChapters();
                return;
            }
            _session.StartNewRun(dealer, _classes[index]);
            OpenTable();
        }

        private void ConfirmSeat()
        {
            if (_pendingSeat < 0) return;

            Dealer dealer = _dealers[_pendingSeat];
            _pendingSeat = -1;
            _session.SwitchTable(dealer);
            OpenTable();
        }

        private void CancelSeat()
        {
            _pendingSeat = -1;
        }

        private void Back()
        {
            if (_changingTables)
                OpenTable();
            else
                OpenMenu();
        }

        private void OpenMenu()
        {
            HideAll();
            _chapterChoice = false;
            _menu.SetChapters(_chapters != null, _chapters != null && _chapters.CanContinue);
            _menu.Show(_session.CanContinue);
            Curtain();
        }

        private void OpenSettings()
        {
            HideAll();
            _settings.Show();
            Curtain();
        }

        private void OpenTable()
        {
            HideAll();
            _table.SetVisible(true);
            Curtain();
        }
    }
}
