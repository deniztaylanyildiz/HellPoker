using HellPoker.Core.Dealers;
using HellPoker.Core.Game;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Composition root: reads Inspector settings, builds the game, the table and menu with their presenters, and connects input.
    /// It is the only Unity-side class that knows concrete implementations.
    /// </summary>
    public sealed class HellPokerBootstrap : MonoBehaviour
    {
        [Header("Sentence")]
        [SerializeField] private int _startingYears = 1000;
        [SerializeField] private int _damnationYears = 2000;

        [Tooltip("At or below this many years left, passing is forbidden and every decision must raise or fold.")]
        [SerializeField] private int _forcedRaiseYears = 250;

        [Header("Stakes")]
        [Tooltip("Ante choices. Every raise adds the ante again.")]
        [SerializeField] private int[] _stakeOptions = { 10, 25, 50, 100, 200 };

        // Discards, house reveals and payouts are each dealer's house rules: see DealerRoster.

        [Header("Randomness")]
        [Tooltip("Use a fixed seed for reproducible shuffles while debugging.")]
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed = 666;

        private TablePresenter _tablePresenter;
        private MainMenuPresenter _menuPresenter;

        private void Awake()
        {
            EnsureEventSystem();

            var table = new GameRules(_startingYears, _damnationYears, Mathf.Min(_stakeOptions), Mathf.Max(_stakeOptions),
                forcedRaiseYears: _forcedRaiseYears);
            int? seed = _useFixedSeed ? _seed : (int?)null;

            TableView tableView = TableView.Create(transform, _stakeOptions);
            _tablePresenter = new TablePresenter(dealer => HellPokerGameFactory.Create(table, dealer, seed), tableView, _stakeOptions);

            MainMenuView menu = MainMenuView.Create(transform,
                string.Format(UiText.MenuTaglineFormat, table.StartingYears),
                string.Format(UiText.RulesFormat, table.StartingYears, table.DamnationYears, table.ForcedRaiseYears));
            DealerSelectView dealerSelect = DealerSelectView.Create(transform);
            _menuPresenter = new MainMenuPresenter(menu, dealerSelect, tableView, _tablePresenter, new UnityApplicationQuitter(), DealerRoster.All);

            gameObject.AddComponent<KeyboardInput>().Bind(_tablePresenter, _menuPresenter);
        }

        private void OnDestroy()
        {
            _menuPresenter?.Dispose();
            _tablePresenter?.Dispose();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }
}
