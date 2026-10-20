using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Game;
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
    /// Composition root: reads Inspector settings, builds the menu, the chapters (and their table) with their presenters, and connects input.
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

        private ChapterPresenter _chapterPresenter;
        private FeedbackPresenter _feedbackPresenter;
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
            // Demo 3.4.1: the Boss Simulator is gone; what it left in the store (its run, its records) goes, the game's own stays.
            SimulatorLeftovers.Forget(store);
            // A first launch speaks the system's language (Turkish or English); batch runs (tests) always start in English.
            var settings = new GameSettings(store, FirstLanguage);
            AnimationClock.Speed = settings.SpeedMultiplier;   // before anything animates
            // Sound: none in batch runs (tests); the click of every button goes through it.
            IAudio audio = Application.isBatchMode ? (IAudio)NullAudio.Instance : UnityAudio.Create(transform);
            UiFactory.ButtonClicked = () => audio.PlaySfx(SfxIds.Click);
            SettingsView settingsView = SettingsView.Create(transform);

            // Every demon's hall is loaded up front: the first sight of a hall never stalls a frame.
            UiArt.Salons.Preload(DealerRoster.All.Select(d => d.Id).Append(DealerRoster.LuciferId));

            MainMenuView menu = MainMenuView.Create(transform, DealerRoster.Mammon.Payouts);
            _settingsPresenter = new SettingsPresenter(settings, settingsView, new UnityDisplayMode(), audio);
            SinnerSelectView sinnerSelect = SinnerSelectView.Create(transform, UiArt.Salons);
            RecordsView records = RecordsView.Create(transform);
            ScreenTransitionView transition = ScreenTransitionView.Create(transform);

            // The chapters: a table of their own (its view, its presenter, built when a run first needs it), the map and its panel.
            ChapterMapView chapterMap = ChapterMapView.Create(transform);
            ChapterPanelView chapterPanel = ChapterPanelView.Create(transform, UiArt.Dealers);
            RelicBagView relicBag = RelicBagView.Create(transform);
            BossLootView loot = BossLootView.Create(transform);
            loot.Audio = audio;
            _chapterPresenter = new ChapterPresenter(chapterMap, chapterPanel, () =>
                {
                    UiArt.Salons.Preload(Core.Chapters.ChapterCast.All);
                    TableView chapterTableView = TableView.Create(transform, UiArt.Dealers, UiArt.Salons);
                    chapterTableView.name = "ChapterTableCanvas";
                    chapterTableView.Audio = audio;
                    chapterTableView.SetVisible(false);
                    var presenter = new TablePresenter((dealer, sinner) => HellPokerGameFactory.Create(table, dealer, seed, sinner: sinner), chapterTableView,
                        settings, audio: audio);
                    return (presenter, chapterTableView);
                }, transition, audio, table, () => seed ?? Core.Randomness.RandomSeeds.Fresh(), new ChapterArchive(store), RunLogs, relicBag, SaveButtonView.Create(transform), loot);

            _menuPresenter = new MainMenuPresenter(menu, settingsView, records, new UnityApplicationQuitter(), transition, sinnerSelect, _chapterPresenter,
                audio);

            // The tester's bug report / idea box, from the title menu (also the game's pause menu): mail and the clipboard.
            FeedbackView feedbackView = FeedbackView.Create(transform);
            _feedbackPresenter = new FeedbackPresenter(feedbackView, new UnityLinks(), () => _chapterPresenter.Feedback, Application.version);
            menu.FeedbackPressed += () => _feedbackPresenter.Open();

            gameObject.AddComponent<KeyboardInput>().Bind(_menuPresenter, _settingsPresenter, _chapterPresenter, _feedbackPresenter);

            // Development builds (and the editor): F3 shows the frame rate. Any build: -fpstour walks every screen, measures, quits
            // (also the release build's smoke test: its log must stay clean through every screen change).
            if (Debug.isDebugBuild)
                FpsCounter.Create(transform);
            if (FpsTour.IsRequested)
                gameObject.AddComponent<FpsTour>().Run(_chapterPresenter);

            DevJumpShortcut.Install(gameObject, _chapterPresenter, _menuPresenter);   // GEÇİCİ TEST KISAYOLU (F9): testler bitince silinecek
        }

        private static Language FirstLanguage => FirstLanguageFor(Application.isBatchMode, Application.systemLanguage);

        /// <summary>
        /// The language of a first launch (nothing saved yet): a Turkish system gets Turkish, every other English. Batch runs
        /// (tests, screenshots) always start in English, whatever machine they run on.
        /// </summary>
        internal static Language FirstLanguageFor(bool batchMode, SystemLanguage system) =>
            batchMode ? Language.English : GameSettings.LanguageForSystem(system == SystemLanguage.Turkish);

        /// <summary>
        /// Settings, the saved run and the records live in PlayerPrefs. Batch runs (tests, screenshots) use one store in memory
        /// for the whole process instead: they never touch the player's own, and reloading the scene still finds the save.
        /// </summary>
        private static ISettingsStore Store => Application.isBatchMode ? BatchStore : (ISettingsStore)new PlayerPrefsStore();

        /// <summary>The in-memory store of batch runs (tests may clear it).</summary>
        public static readonly MemoryStore BatchStore = new MemoryStore();

        /// <summary>Once the window exists: it comes to the front and takes the focus (a game opened behind another window
        /// would otherwise wait there unseen).</summary>
        private void Start()
        {
            if (!Application.isBatchMode) WindowFocus.BringToFront();
        }

        /// <summary>The playtest's run logs: text files next to the save (persistentDataPath/runs); none in batch runs (tests).</summary>
        private static IRunLogSink RunLogs => Application.isBatchMode ? null
            : new FileRunLogSink(System.IO.Path.Combine(Application.persistentDataPath, "runs"),
                HellPoker.Core.Game.ReleaseVersion.Display(Application.version));

        private void OnDestroy()
        {
            _chapterPresenter?.CloseLog();   // a run still going is written as it stands
            UiFactory.ButtonClicked = null;   // the scene's sound goes with it
            _menuPresenter?.Dispose();
            _feedbackPresenter?.Dispose();
            _chapterPresenter?.Dispose();   // and the chapter's table with it
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
