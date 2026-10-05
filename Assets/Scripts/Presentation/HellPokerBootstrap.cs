using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
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
            // Alt-tab must not freeze the game (or its music): it keeps running behind other windows.
            Application.runInBackground = true;
            EnsureEventSystem();

            // The soul line given here is only a placeholder: every dealer sets their own (Dealer.ApplyTo).
            var table = new GameRules(_startingYears, forcedRaiseYears: _forcedRaiseYears,
                stakes: new StakeScale(_stakeDivisor, _minimumUnit, _tableCapPercent), openingCardsShown: _openingCardsShown,
                raiseUnitsBeforeDraw: _raiseUnitsBeforeDraw, raiseUnitsAfterDraw: _raiseUnitsAfterDraw, houseReRaiseUnits: _houseReRaiseUnits,
                soulWorthYears: _soulWorthYears, soulLossPercent: _soulLossPercent);
            int? seed = _useFixedSeed ? _seed : (int?)null;

            ISettingsStore store = Store;
            // A first launch speaks the system's language (Turkish or English); batch runs (tests) always start in English.
            var settings = new GameSettings(store, FirstLanguage);
            AnimationClock.Speed = settings.SpeedMultiplier;   // before anything animates
            var archive = new RunArchive(store);
            // Sound: none in batch runs (tests); the click of every button goes through it.
            IAudio audio = Application.isBatchMode ? (IAudio)NullAudio.Instance : UnityAudio.Create(transform);
            UiFactory.ButtonClicked = () => audio.PlaySfx(SfxIds.Click);
            SettingsView settingsView = SettingsView.Create(transform);

            // Every hall's art is loaded up front: the first sight of a hall never stalls a frame.
            UiArt.Salons.Preload(DealerRoster.All.Select(d => d.Id).Append(DealerRoster.LuciferId));
            TableView tableView = TableView.Create(transform, UiArt.Dealers, UiArt.Salons);
            tableView.Audio = audio;
            // The run's events between hands: their own dice, derived from a master seed like the games' streams.
            var events = new EventSession(EventDeck.Standard,
                new Core.Randomness.SystemRandomSource(Core.Randomness.RandomSeeds.Derive(seed ?? Core.Randomness.RandomSeeds.Fresh(),
                    HellPokerGameFactory.EventStream)), EventChance(table), table.EventCooldownHands);
            _tablePresenter = new TablePresenter((dealer, sinner) => HellPokerGameFactory.Create(table, dealer, seed, sinner: sinner), tableView,
                settings, archive,
                DealerRoster.Lucifer, events, audio, RunLogs);
            ResumeSavedRun(archive);

            MainMenuView menu = MainMenuView.Create(transform,
                () => string.Format(UiText.MenuTaglineFormat, table.StartingYears),
                () => string.Format(UiText.RulesFormat, table.StartingYears, table.SoulThreshold, table.ForcedRaiseYears, table.Stakes.TableCapPercent,
                    table.LuciferGateYears, table.LuciferCastDownYears, DealerRoster.LuciferUnit, DealerRoster.LuciferCap),
                DealerRoster.Mammon.Payouts, UiText.CheatsPage, UiText.SinnersPage);
            _settingsPresenter = new SettingsPresenter(settings, settingsView, new UnityDisplayMode(), audio, menu);
            DealerSelectView dealerSelect = DealerSelectView.Create(transform, UiArt.Dealers, UiArt.Salons);
            SinnerSelectView sinnerSelect = SinnerSelectView.Create(transform, UiArt.Salons);
            EndScreenView endScreen = EndScreenView.Create(transform, UiArt.Dealers);
            RecordsView records = RecordsView.Create(transform);
            ScreenTransitionView transition = ScreenTransitionView.Create(transform);
            _menuPresenter = new MainMenuPresenter(menu, dealerSelect, settingsView, endScreen, records, tableView, _tablePresenter,
                new UnityApplicationQuitter(), transition, DealerRoster.All, DealerRoster.Lucifer, sinnerSelect, SinnerRoster.All, audio);

            gameObject.AddComponent<KeyboardInput>().Bind(_tablePresenter, _menuPresenter, _settingsPresenter);

            // Development builds (and the editor): F3 shows the frame rate. Any build: -fpstour walks every screen, measures, quits
            // (also the release build's smoke test: its log must stay clean through every screen change).
            if (Debug.isDebugBuild)
                FpsCounter.Create(transform);
            if (FpsTour.IsRequested)
                gameObject.AddComponent<FpsTour>().Run(_tablePresenter);
        }

        /// <summary>
        /// Settings, the saved run and the records live in PlayerPrefs. Batch runs (tests, screenshots) use one store in memory
        /// for the whole process instead: they never touch the player's own, and reloading the scene still finds the save.
        /// </summary>
        /// <summary>Batch runs (tests, screenshots, the FPS tour) play without random events: a test that presses its way through
        /// hands must not meet an offer at random. Their rules are covered by the EditMode tests.</summary>
        private static int EventChance(GameRules table) => Application.isBatchMode ? 0 : table.EventChancePercent;

        private static Language FirstLanguage => FirstLanguageFor(Application.isBatchMode, Application.systemLanguage);

        /// <summary>
        /// The language of a first launch (nothing saved yet): a Turkish system gets Turkish, every other English. Batch runs
        /// (tests, screenshots) always start in English, whatever machine they run on.
        /// </summary>
        internal static Language FirstLanguageFor(bool batchMode, SystemLanguage system) =>
            batchMode ? Language.English : GameSettings.LanguageForSystem(system == SystemLanguage.Turkish);

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

        /// <summary>Once the window exists: it comes to the front and takes the focus (a game opened behind another window
        /// would otherwise wait there unseen).</summary>
        private void Start()
        {
            if (!Application.isBatchMode) WindowFocus.BringToFront();
        }

        /// <summary>The playtest's run logs: text files next to the save (persistentDataPath/runs); none in batch runs (tests).</summary>
        private static IRunLogSink RunLogs => Application.isBatchMode ? null
            : new FileRunLogSink(System.IO.Path.Combine(Application.persistentDataPath, "runs"), Application.version);

        private void OnDestroy()
        {
            _tablePresenter?.CloseLog();   // a run still going is written as it stands
            UiFactory.ButtonClicked = null;   // the scene's sound goes with it
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
