using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Moves the player between the title menu, the dealer choice and the table.
    /// Menu → New Game → choose a demon → table. During a run the same choice screen changes tables: the sentence goes
    /// along, and sitting with a demon whose soul line is already passed needs the player to confirm a warning.
    /// Knows nothing about poker rules; the run itself is reached through <see cref="IRunSession"/>.
    /// </summary>
    public sealed class MainMenuPresenter : IMenuCommands, IDisposable
    {
        private readonly IMainMenuView _menu;
        private readonly IDealerSelectView _dealerSelect;
        private readonly ITableView _table;
        private readonly IRunSession _session;
        private readonly IApplicationQuitter _quitter;
        private readonly Dealer[] _dealers;
        private readonly DealerCard[] _dealerCards;

        private bool _changingTables;
        private int _pendingSeat = -1;

        public MainMenuPresenter(IMainMenuView menu, IDealerSelectView dealerSelect, ITableView table, IRunSession session,
            IApplicationQuitter quitter, IReadOnlyList<Dealer> dealers)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _dealerSelect = dealerSelect ?? throw new ArgumentNullException(nameof(dealerSelect));
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _quitter = quitter ?? throw new ArgumentNullException(nameof(quitter));
            if (dealers == null || dealers.Count == 0) throw new ArgumentException("At least one dealer is needed.", nameof(dealers));
            _dealers = dealers.ToArray();
            _dealerCards = _dealers.Select(DealerCards.Describe).ToArray();

            _menu.NewGamePressed += OpenNewRunChoice;
            _menu.ContinuePressed += OpenTable;
            _menu.ChangeTablePressed += AskToChangeTables;
            _menu.QuitPressed += _quitter.Quit;
            _dealerSelect.DealerChosen += Choose;
            _dealerSelect.BackPressed += Back;
            _dealerSelect.SeatConfirmed += ConfirmSeat;
            _dealerSelect.SeatCancelled += CancelSeat;
            _table.MenuPressed += OpenMenu;
            _session.LeaveRequested += OpenTableChoice;

            OpenMenu();
        }

        /// <summary>True on any menu screen (title or dealer choice) — the table is not taking input.</summary>
        public bool IsMenuOpen => _menu.IsVisible || _dealerSelect.IsVisible;

        /// <summary>Esc: from the table or the dealer choice back to the title; from the title back into a run in progress.</summary>
        public void ToggleMenu()
        {
            if (!_menu.IsVisible)
                OpenMenu();
            else if (_session.CanContinue)
                OpenTable();
        }

        public void Dispose()
        {
            _menu.NewGamePressed -= OpenNewRunChoice;
            _menu.ContinuePressed -= OpenTable;
            _menu.ChangeTablePressed -= AskToChangeTables;
            _menu.QuitPressed -= _quitter.Quit;
            _dealerSelect.DealerChosen -= Choose;
            _dealerSelect.BackPressed -= Back;
            _dealerSelect.SeatConfirmed -= ConfirmSeat;
            _dealerSelect.SeatCancelled -= CancelSeat;
            _table.MenuPressed -= OpenMenu;
            _session.LeaveRequested -= OpenTableChoice;
        }

        private void OpenNewRunChoice()
        {
            _changingTables = false;
            ShowChoice(_dealers.Select(d => new DealerChoice(Card(d), soulAtStake: false, isCurrent: false)));
        }

        /// <summary>From the menu: go back to the table and ask there, so a soul-bound table answers in the dealer's voice.</summary>
        private void AskToChangeTables()
        {
            OpenTable();
            _session.RequestLeave();
        }

        private void OpenTableChoice()
        {
            _changingTables = true;
            ShowChoice(_dealers.Select(d => new DealerChoice(Card(d), _session.WouldStakeSoul(d), d.Id == _session.CurrentDealerId)));
        }

        private DealerCard Card(Dealer dealer) => _dealerCards[Array.IndexOf(_dealers, dealer)];

        private void ShowChoice(IEnumerable<DealerChoice> choices)
        {
            _pendingSeat = -1;
            _menu.Hide();
            _table.SetVisible(false);
            _dealerSelect.Show(choices.ToArray());
        }

        private void Choose(int index)
        {
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
            _table.SetVisible(false);
            _dealerSelect.Hide();
            _menu.Show(_session.CanContinue);
        }

        private void OpenTable()
        {
            _menu.Hide();
            _dealerSelect.Hide();
            _table.SetVisible(true);
        }
    }
}
