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
        [Tooltip("The betting unit (and ante) is the sentence divided by this, rounded down to a readable step.")]
        [SerializeField] private int _stakeDivisor = 10;
        [Tooltip("The smallest betting unit, in years.")]
        [SerializeField] private int _minimumUnit = 10;
        [Tooltip("At most this share of the sentence may be on the table in one hand.")]
        [SerializeField, Range(1, 100)] private int _tableCapPercent = 30;

        [Header("Bet flow")]
        [Tooltip("Player cards that turn together at the deal, before the first decision.")]
        [SerializeField, Range(0, 4)] private int _openingCardsShown = 2;
        [SerializeField, Min(1)] private int _raiseUnitsBeforeDraw = 1;
        [SerializeField, Min(1)] private int _raiseUnitsAfterDraw = 2;
        [SerializeField, Min(1)] private int _houseReRaiseUnits = 1;

        // Discards, house cards shown, payouts and temper are each dealer's house rules: see DealerRoster.

        [Header("Randomness")]
        [Tooltip("Use a fixed seed for reproducible shuffles while debugging.")]
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed = 666;

        private TablePresenter _tablePresenter;
        private MainMenuPresenter _menuPresenter;

        private void Awake()
        {
            EnsureEventSystem();

            var table = new GameRules(_startingYears, _damnationYears, forcedRaiseYears: _forcedRaiseYears,
                stakes: new StakeScale(_stakeDivisor, _minimumUnit, _tableCapPercent), openingCardsShown: _openingCardsShown,
                raiseUnitsBeforeDraw: _raiseUnitsBeforeDraw, raiseUnitsAfterDraw: _raiseUnitsAfterDraw, houseReRaiseUnits: _houseReRaiseUnits);
            int? seed = _useFixedSeed ? _seed : (int?)null;

            TableView tableView = TableView.Create(transform, UiArt.Dealers);
            _tablePresenter = new TablePresenter(dealer => HellPokerGameFactory.Create(table, dealer, seed), tableView);

            MainMenuView menu = MainMenuView.Create(transform,
                string.Format(UiText.MenuTaglineFormat, table.StartingYears),
                string.Format(UiText.RulesFormat, table.StartingYears, table.DamnationYears, table.ForcedRaiseYears, table.Stakes.TableCapPercent));
            DealerSelectView dealerSelect = DealerSelectView.Create(transform, UiArt.Dealers);
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
