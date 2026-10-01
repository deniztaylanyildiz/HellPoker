using System;
using HellPoker.Presentation.Abstractions;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Moves the player between the title menu and the table. Knows nothing about poker rules;
    /// the run itself is reached through <see cref="IRunSession"/>.
    /// </summary>
    public sealed class MainMenuPresenter : IMenuCommands, IDisposable
    {
        private readonly IMainMenuView _menu;
        private readonly ITableView _table;
        private readonly IRunSession _session;
        private readonly IApplicationQuitter _quitter;

        public MainMenuPresenter(IMainMenuView menu, ITableView table, IRunSession session, IApplicationQuitter quitter)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _quitter = quitter ?? throw new ArgumentNullException(nameof(quitter));

            _menu.NewGamePressed += StartNewRun;
            _menu.ContinuePressed += OpenTable;
            _menu.QuitPressed += _quitter.Quit;
            _table.MenuPressed += OpenMenu;

            OpenMenu();
        }

        public bool IsMenuOpen => _menu.IsVisible;

        public void ToggleMenu()
        {
            if (!IsMenuOpen)
                OpenMenu();
            else if (_session.CanContinue)
                OpenTable();
        }

        public void Dispose()
        {
            _menu.NewGamePressed -= StartNewRun;
            _menu.ContinuePressed -= OpenTable;
            _menu.QuitPressed -= _quitter.Quit;
            _table.MenuPressed -= OpenMenu;
        }

        private void StartNewRun()
        {
            _session.StartNewRun();
            OpenTable();
        }

        private void OpenMenu()
        {
            _table.SetVisible(false);
            _menu.Show(_session.CanContinue);
        }

        private void OpenTable()
        {
            _menu.Hide();
            _table.SetVisible(true);
        }
    }
}
