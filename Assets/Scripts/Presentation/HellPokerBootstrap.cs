using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Game;
using HellPoker.Presentation.Settings;
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

        [Tooltip("At or below this many years left, passing is forbidden and every decision must raise or fold.")]
        [SerializeField] private int _forcedRaiseYears = 250;

        [Header("Soul")]
        [Tooltip("What the soul is worth in years once the sentence passes the dealer's soul line. Never shown to the player.")]
        [SerializeField, Min(10)] private int _soulWorthYears = 1000;
        [Tooltip("Losses while the soul is on the table cost this percent (on top of the dealer's own loss percent).")]
        [SerializeField, Min(100)] private int _soulLossPercent = 150;
        // Each dealer's soul line is a house rule: see DealerRoster.

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
        private SettingsPresenter _settingsPresenter;

        private void Awake()
        {
            EnsureEventSystem();

            // The soul line given here is only a placeholder: every dealer sets their own (Dealer.ApplyTo).
            var table = new GameRules(_startingYears, forcedRaiseYears: _forcedRaiseYears,
                stakes: new StakeScale(_stakeDivisor, _minimumUnit, _tableCapPercent), openingCardsShown: _openingCardsShown,
                raiseUnitsBeforeDraw: _raiseUnitsBeforeDraw, raiseUnitsAfterDraw: _raiseUnitsAfterDraw, houseReRaiseUnits: _houseReRaiseUnits,
                soulWorthYears: _soulWorthYears, soulLossPercent: _soulLossPercent);
            int? seed = _useFixedSeed ? _seed : (int?)null;

            ISettingsStore store = Store;
            var settings = new GameSettings(store);
            var archive = new RunArchive(store);
            SettingsView settingsView = SettingsView.Create(transform);
            _settingsPresenter = new SettingsPresenter(settings, settingsView, new UnityDisplayMode());

            TableView tableView = TableView.Create(transform, UiArt.Dealers, UiArt.Salons);
            _tablePresenter = new TablePresenter(dealer => HellPokerGameFactory.Create(table, dealer, seed), tableView, settings, archive,
                DealerRoster.Lucifer);
            ResumeSavedRun(archive);

            MainMenuView menu = MainMenuView.Create(transform,
                string.Format(UiText.MenuTaglineFormat, table.StartingYears),
                string.Format(UiText.RulesFormat, table.StartingYears, table.SoulThreshold, table.ForcedRaiseYears, table.Stakes.TableCapPercent,
                    table.LuciferGateYears, table.LuciferCastDownYears, DealerRoster.LuciferUnit, DealerRoster.LuciferCap),
                DealerRoster.Mammon.Payouts, UiText.CheatsPage());
            DealerSelectView dealerSelect = DealerSelectView.Create(transform, UiArt.Dealers, UiArt.Salons);
            EndScreenView endScreen = EndScreenView.Create(transform, UiArt.Dealers);
            RecordsView records = RecordsView.Create(transform);
            ScreenTransitionView transition = ScreenTransitionView.Create(transform);
            _menuPresenter = new MainMenuPresenter(menu, dealerSelect, settingsView, endScreen, records, tableView, _tablePresenter,
                new UnityApplicationQuitter(), transition, DealerRoster.All, DealerRoster.Lucifer);

            gameObject.AddComponent<KeyboardInput>().Bind(_tablePresenter, _menuPresenter, _settingsPresenter);
        }

        /// <summary>
        /// Settings, the saved run and the records live in PlayerPrefs. Batch runs (tests, screenshots) use one store in memory
        /// for the whole process instead: they never touch the player's own, and reloading the scene still finds the save.
        /// </summary>
        private static ISettingsStore Store => Application.isBatchMode ? BatchStore : (ISettingsStore)new PlayerPrefsStore();

        /// <summary>The in-memory store of batch runs (tests may clear it).</summary>
        public static readonly MemoryStore BatchStore = new MemoryStore();

        /// <summary>A run saved between hands is picked up where it was left; a save for an unknown demon is dropped.</summary>
        private void ResumeSavedRun(RunArchive archive)
        {
            RunSnapshot saved = archive.LoadRun();
            if (saved == null) return;

            Dealer dealer = DealerRoster.Find(saved.DealerId);
            Dealer origin = saved.OriginDealerId == null ? null : DealerRoster.Find(saved.OriginDealerId);
            // At Lucifer's table the save must know where a fall would land.
            bool lost = dealer == null || (dealer.IsFinalTable && (origin == null || origin.IsFinalTable));
            if (lost)
            {
                archive.ClearRun();
                return;
            }
            _tablePresenter.Resume(dealer, saved, origin);
        }

        private void OnDestroy()
        {
            _menuPresenter?.Dispose();
            _tablePresenter?.Dispose();
            _settingsPresenter?.Dispose();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }
}
