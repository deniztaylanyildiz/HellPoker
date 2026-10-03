using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Dealers;
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
        private int _pendingSeat = -1;

        /// <param name="dealers">The demons the player may choose.</param>
        /// <param name="finalDealer">Lucifer, shown locked after them; he is never chosen, only met below the gate.</param>
        public MainMenuPresenter(IMainMenuView menu, IDealerSelectView dealerSelect, ISettingsView settings, IEndScreenView endScreen,
            IRecordsView records, ITableView table, IRunSession session, IApplicationQuitter quitter, IScreenTransition transition,
            IReadOnlyList<Dealer> dealers, Dealer finalDealer = null)
        {
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
            _menu.NewGameConfirmed += AbandonAndChoose;
            _menu.ContinuePressed += OpenTable;
            _menu.ChangeTablePressed += AskToChangeTables;
            _menu.SettingsPressed += OpenSettings;
            _menu.RecordsPressed += OpenRecords;
            _menu.QuitPressed += _quitter.Quit;
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

            OpenMenu();
        }

        public bool IsMenuOpen => _menu.IsVisible || _dealerSelect.IsVisible || _settings.IsVisible || _endScreen.IsVisible || _records.IsVisible;

        public bool IsTransitioning => _transition.IsPlaying;

        public void GoBack()
        {
            if (_dealerSelect.IsVisible)
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
            _menu.NewGameConfirmed -= AbandonAndChoose;
            _menu.ContinuePressed -= OpenTable;
            _menu.ChangeTablePressed -= AskToChangeTables;
            _menu.SettingsPressed -= OpenSettings;
            _menu.RecordsPressed -= OpenRecords;
            _menu.QuitPressed -= _quitter.Quit;
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
        }

        /// <summary>
        /// The language changed: the demons are described again, and the screen that is open says the same in the new words
        /// (without a curtain). Fixed labels follow by themselves (<see cref="LocalizedText"/>).
        /// </summary>
        private void OnLanguageChanged()
        {
            _dealerCards = _dealers.Select(DealerCards.Describe).ToArray();
            _finalCard = _finalDealer == null ? null : DealerCards.Describe(_finalDealer);

            if (_dealerSelect.IsVisible)
            {
                if (_choiceForTables)
                    OpenTableChoice(curtain: false);
                else
                    OpenNewRunChoice(curtain: false);
            }
            else if (_records.IsVisible)
            {
                _records.Show(_session.Records, _dealerCards);
            }
            else if (_endScreen.IsVisible && _lastSummary != null)
            {
                _endScreen.Show(_lastSummary);
            }
            else if (_menu.IsVisible && _menu.IsConfirming)
            {
                AskForNewGame();
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
            _menu.AskToConfirmNewGame(taunt, warning);
        }

        private void AbandonAndChoose()
        {
            _session.AbandonRun();
            OpenNewRunChoice();
        }

        private void OpenNewRunChoice() => OpenNewRunChoice(curtain: true);

        private void OpenNewRunChoice(bool curtain)
        {
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
            if (curtain) _transition.Play();
        }

        /// <summary>Every screen goes away; the caller shows the one wanted.</summary>
        private void HideAll()
        {
            _menu.Hide();
            _dealerSelect.Hide();
            _settings.Hide();
            _endScreen.Hide();
            _records.Hide();
            _table.SetVisible(false);
        }

        private void ShowEnd(RunSummary summary)
        {
            _lastSummary = summary;
            HideAll();
            _endScreen.Show(summary);
            _transition.Play();
        }

        private void OpenRecords()
        {
            HideAll();
            _records.Show(_session.Records, _dealerCards);
            _transition.Play();
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
                _session.StartNewRun(dealer);
                OpenTable();
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
            _menu.Show(_session.CanContinue);
            _transition.Play();
        }

        private void OpenSettings()
        {
            HideAll();
            _settings.Show();
            _transition.Play();
        }

        private void OpenTable()
        {
            HideAll();
            _table.SetVisible(true);
            _transition.Play();
        }
    }
}
