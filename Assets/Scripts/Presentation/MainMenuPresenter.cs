using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Presentation.Abstractions;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Moves the player between the title menu, the dealer choice and the table.
    /// Menu → New Game → choose a demon → table. Knows nothing about poker rules;
    /// the run itself is reached through <see cref="IRunSession"/>.
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

            _menu.NewGamePressed += OpenDealerSelect;
            _menu.ContinuePressed += OpenTable;
            _menu.QuitPressed += _quitter.Quit;
            _dealerSelect.DealerChosen += StartRun;
            _dealerSelect.BackPressed += OpenMenu;
            _table.MenuPressed += OpenMenu;

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
            _menu.NewGamePressed -= OpenDealerSelect;
            _menu.ContinuePressed -= OpenTable;
            _menu.QuitPressed -= _quitter.Quit;
            _dealerSelect.DealerChosen -= StartRun;
            _dealerSelect.BackPressed -= OpenMenu;
            _table.MenuPressed -= OpenMenu;
        }

        private void OpenDealerSelect()
        {
            _menu.Hide();
            _table.SetVisible(false);
            _dealerSelect.Show(_dealerCards);
        }

        private void StartRun(int index)
        {
            if (index < 0 || index >= _dealers.Length) return;

            _session.StartNewRun(_dealers[index]);
            OpenTable();
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
