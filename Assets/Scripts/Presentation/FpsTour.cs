using System;
using System.Collections;
using System.Linq;
using System.Text;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Measures the frame rate on every screen with moving backdrops, in the real player: started with the command line
    /// argument <c>-fpstour</c> (any build: it is also the release build's smoke test). It walks the game through its own
    /// buttons — the menu and its settings, a run crossing from Mammon's spoils into Belial's chapter, a floor's table, then each
    /// demon's table and Lucifer's (runs placed in the save and picked up with CONTINUE) — measures a few seconds on each, writes the
    /// results to the log ("Hell Poker FPS ...") and quits. Not part of normal play; it writes over the player's saved run.
    /// </summary>
    public sealed class FpsTour : MonoBehaviour
    {
        public const string Argument = "-fpstour";
        private const float Settle = 1.5f;
        private const float Window = 4f;

        private ChapterPresenter _chapters;
        private readonly StringBuilder _report = new StringBuilder();

        public static bool IsRequested => System.Environment.GetCommandLineArgs().Contains(Argument);

        /// <summary>Masa birliği: with <c>-tableshots</c> the tour also sits at a warden's table and saves a screenshot of it and of each
        /// demon's table and Lucifer's (persistentDataPath/tour/table_*.png).</summary>
        public const string TableShotsArgument = "-tableshots";
        private static bool TableShots => System.Environment.GetCommandLineArgs().Contains(TableShotsArgument);

        /// <summary>Demo 3.4: "-difficulty=medium" (easy, hard, ironman): the chapter leg is played at that level; Ironman also saves at a
        /// camp, leaves, comes back, then dies (its save gone). "-difficultyshots": the difficulty row, SAVE on the map and at a table.</summary>
        private static Difficulty TourDifficulty =>
            DifficultyRules.Parse(System.Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("-difficulty="))?.Substring("-difficulty=".Length));
        private static bool DifficultyShots => System.Environment.GetCommandLineArgs().Contains("-difficultyshots");

        /// <param name="chapters">The game's chapters: every leg of the tour is one of their screens.</param>
        public void Run(ChapterPresenter chapters)
        {
            _chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
            StartCoroutine(Tour());
        }

        /// <summary>Phase 2: Mammon's spoils → the second chapter's opening → Belial's map → the first imp's table.</summary>
        private IEnumerator ChapterLeg()
        {
            _chapters.PrepareTour(Core.Sinners.SinnerRoster.Peasant, TourDifficulty);
            Press("ContinueButton");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("p2 spoils");   // Demo 3.5: the spoils' scene (the cards come in, then the choice)
            var loot = FindFirstObjectByType<BossLootView>();
            if (loot != null && loot.IsAnimating) loot.Skip();
            int coins = loot != null && loot.State != null ? loot.State.Cards.Count : 1;
            Debug.Log($"Hell Poker spoils: scene {loot != null && loot.IsOpen}, cards {coins}, reroll {loot?.State?.CanReroll}");
            Press("LootCard" + (coins - 1));   // the coins
            yield return new WaitForSecondsRealtime(Settle);
            Debug.Log($"Hell Poker Phase 2 tour: chapter {_chapters.Journey?.Chapter}, demon {_chapters.Run?.Rules.BossId}, " +
                      $"floors {_chapters.Run?.Map.FloorCount}, purse {_chapters.Run?.Purse.Coins}, difficulty {_chapters.Journey?.Difficulty}, " +
                      $"ante every {_chapters.Run?.Rules.AnteStepHands} hands, tribute {_chapters.Run?.Tribute}");
            Press("PanelOption0");   // DESCEND
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("p2 belial map");
            // An imp's table (Demo 3.1.1: the map's leftmost node is not always one — a market or an offer left the panel unchecked).
            MapNode first = _chapters.Run?.Choices.Where(n => n.Kind == NodeKind.Table).OrderBy(n => n.Lane).FirstOrDefault()
                            ?? _chapters.Run?.Choices.OrderBy(n => n.Lane).FirstOrDefault();
            if (first != null) Press($"Node{first.Floor}_{first.Lane}");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("p2 belial imp");
            yield return CheckThePlayersPanel("p2_belial_imp");
            var save = FindFirstObjectByType<SaveButtonView>();
            Debug.Log($"Hell Poker save button at the table: {save?.Place}, enabled {save?.Enabled}");
        }

        /// <summary>Demo 3.4: an Ironman run at a camp — SAVE there, left (as if the game closed), CONTINUE back at the camp; then it dies
        /// and its save is gone for good.</summary>
        private IEnumerator IronmanLeg()
        {
            Press("MenuButton");
            yield return new WaitForSecondsRealtime(0.8f);
            _chapters.ForgetForTour(CampSave(Core.Sinners.SinnerRoster.Peasant, Difficulty.Ironman));
            Press("ContinueButton");
            yield return new WaitForSecondsRealtime(Settle);
            bool atCamp = _chapters.Run?.CurrentKind == NodeKind.PurgatoryFire;
            var save = FindFirstObjectByType<SaveButtonView>();
            string place = save?.Place.ToString();
            int before = _chapters.Journey?.SaveCount ?? -1;
            Press("SaveButton");
            yield return new WaitForSecondsRealtime(0.5f);
            int after = _chapters.Journey?.SaveCount ?? -1;
            yield return Shot("ironman_camp");
            string id = _chapters.Journey?.RunId;
            Press("MapMenuButton");   // the map's MENU (its click goes through over the camp's panel)
            yield return new WaitForSecondsRealtime(0.8f);
            _chapters.DropForTour();
            Press("ContinueButton");
            yield return new WaitForSecondsRealtime(Settle);
            bool back = _chapters.Journey?.RunId == id && _chapters.Run?.CurrentKind == NodeKind.PurgatoryFire;
            _chapters.DieForTour();
            yield return new WaitForSecondsRealtime(0.8f);
            Debug.Log($"Hell Poker ironman: at the camp {atCamp}, save button {place}, saved {before}->{after}, continued at the camp {back}, " +
                      $"after death continue {_chapters.CanContinue}, dead {_chapters.Records.IsDead(id)}");
            Press("PanelOption1");   // the end's MENU
            yield return new WaitForSecondsRealtime(0.8f);
        }

        /// <summary>A save standing at the first chapter's nearest camp, its panel waiting.</summary>
        private static ChapterSave CampSave(Core.Sinners.SinnerClass cls, Difficulty difficulty)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, 11, difficulty);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new System.Collections.Generic.List<MapNode> { n }).ToList();
            System.Collections.Generic.List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == NodeKind.PurgatoryFire);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new System.Collections.Generic.List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = journey.Capture();
            save.Trail = found?.Select(n => n.Lane).ToList() ?? new System.Collections.Generic.List<int>();
            save.NodeDone = false;
            save.Pending = "fire";
            save.Coins = 300;
            return save;
        }

        /// <summary>Demo 3.4: the difficulty row (its words open), then a Medium run's SAVE on the map and dim at a table.</summary>
        private IEnumerator DifficultyLeg()
        {
            Press("NewGameButton");
            yield return new WaitForSecondsRealtime(1f);
            var select = FindFirstObjectByType<SinnerSelectView>();
            select?.PointAtDifficulty();
            yield return null;
            yield return Shot("difficulty_row");
            Debug.Log($"Hell Poker difficulty row: '{select?.DifficultyLabel}' shown {select?.DifficultyShown}");
            Press("ChooseSinner0");
            yield return new WaitForSecondsRealtime(1f);
            Press("PanelOption0");   // DESCEND
            yield return new WaitForSecondsRealtime(0.6f);
            Press("SaveButton");
            yield return null;
            yield return Shot("map_save");
            MapNode table = _chapters.Run?.Choices.FirstOrDefault(n => n.Kind == NodeKind.Table);
            if (table != null) Press($"Node{table.Floor}_{table.Lane}");
            yield return new WaitForSecondsRealtime(Settle);
            _chapters.Table?.PerformAction();
            yield return new WaitForSecondsRealtime(Settle);
            var save = FindFirstObjectByType<SaveButtonView>();
            save?.PointAt();
            yield return null;
            Debug.Log($"Hell Poker save button: map then table {save?.Place}, enabled {save?.Enabled}, run {_chapters.Journey?.Difficulty}");
            yield return Shot("table_save_dim");
            Press("MenuButton");
            yield return new WaitForSecondsRealtime(0.8f);
            _chapters.DropForTour();
        }

        /// <summary>A save placed at a table, picked up from the menu's CONTINUE; one hand dealt; its hall measured (<paramref name="measure"/>);
        /// a screenshot with <c>-tableshots</c>.</summary>
        private IEnumerator SitAndShoot(ChapterSave save, string shot, bool luciferCall = false, string measure = null)
        {
            Press("MenuButton");
            yield return new WaitForSecondsRealtime(0.8f);
            _chapters.ForgetForTour(save);
            Press("ContinueButton");
            yield return new WaitForSecondsRealtime(Settle);
            if (luciferCall)
            {
                Press("PanelOption0");
                yield return new WaitForSecondsRealtime(Settle * 3);
            }
            _chapters.Table?.PerformAction();   // the deal
            yield return new WaitForSecondsRealtime(Settle * 2);
            Debug.Log($"Hell Poker table shot {shot}: at the table {_chapters.IsAtTable}, demon {_chapters.Run?.Rules.BossId}");
            if (measure != null) yield return Measure(measure);
            if (TableShots) yield return Shot("table_" + shot);
        }

        /// <summary>Every demon's hall at his own table (Mammon's, Belial's, Lilith's) and Lucifer's, each measured; a warden's table too
        /// with <c>-tableshots</c>. (Demo 3.4.1: the halls were the Boss Simulator's leg; now they are the game's tables.)</summary>
        private IEnumerator HallLeg()
        {
            if (TableShots) yield return SitAndShoot(WardenSave(), "warden");
            for (int chapter = 1; chapter <= 3; chapter++)
                yield return SitAndShoot(new ChapterSave
                {
                    ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = chapter, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                    BossBar = 1600, NodeDone = false
                }, "boss" + chapter, measure: ChapterRules.For(chapter).BossId);
            yield return SitAndShoot(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 3, "lucifer"), "lucifer", luciferCall: true,
                measure: DealerRoster.LuciferId);
            Press("MenuButton");
            yield return new WaitForSecondsRealtime(0.8f);
        }

        /// <summary>A save standing on the first chapter's nearest warden (a path walked to it).</summary>
        private static ChapterSave WardenSave()
        {
            ChapterJourney journey = ChapterJourney.Begin(Core.Sinners.SinnerRoster.Peasant, 11);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new System.Collections.Generic.List<MapNode> { n }).ToList();
            System.Collections.Generic.List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == NodeKind.Warden);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new System.Collections.Generic.List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Peasant, 11, 1, "");
            save.Trail = found?.Select(n => n.Lane).ToList() ?? new System.Collections.Generic.List<int>();
            save.NodeDone = false;
            save.Coins = 200;
            save.Match = "warden";
            save.MatchHouse = 50;
            return save;
        }

        /// <summary>
        /// Demo 3.1.1: at a chapter's table the player's panel stands under the counter with the class's animated portrait (a player missed
        /// it in a 3.0.2 build): one log line ("Hell Poker panel ...") and a screenshot in persistentDataPath/tour.
        /// </summary>
        private IEnumerator CheckThePlayersPanel(string shot)
        {
            TableView view = FindObjectsByType<TableView>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "ChapterTableCanvas");
            PlayerPanelView panel = view != null ? view.PlayerPanel : null;
            int frames = panel != null && panel.Sinner.Portrait.Clip != null ? panel.Sinner.Portrait.Clip.Frames.Length : 0;
            Debug.Log($"Hell Poker panel {Application.version}: shown {panel != null && panel.gameObject.activeInHierarchy}, class {panel?.Badge.ClassId}, " +
                      $"portrait frames {frames}, relics {panel?.Relics.Relics.Count ?? 0}, " +
                      $"badge on the demon {view != null && view.PortraitBadge.gameObject.activeInHierarchy}");
            string folder = System.IO.Path.Combine(Application.persistentDataPath, "tour");
            System.IO.Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, shot + ".png"));
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        private static IEnumerator Shot(string name)
        {
            string folder = System.IO.Path.Combine(Application.persistentDataPath, "tour");
            System.IO.Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, name + ".png"));
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        /// <summary>
        /// Demo 3.3: the title menu has no language button and a cursor; Settings ▸ LANGUAGE lists the languages and a choice rewrites the
        /// screen (the tour picks the other language, looks, and puts the player's back). One log line ("Hell Poker menu ...") and two
        /// screenshots in persistentDataPath/tour.
        /// </summary>
        private IEnumerator CheckTheMenu()
        {
            MainMenuView menu = FindFirstObjectByType<MainMenuView>();
            bool languageButton = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(b => b.name == "MenuLanguageButton");
            bool card = menu != null && menu.ContinueCardShown;
            string cursor = menu?.SelectedButton;
            yield return Shot("menu");
            Press("SettingsButton");
            yield return new WaitForSecondsRealtime(0.8f);
            Press("SettingsTabLanguage");
            yield return new WaitForSecondsRealtime(0.3f);
            int rows = FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b => b.name.StartsWith("SettingsLanguage", StringComparison.Ordinal));
            Language before = Lang.Current;
            int other = Lang.All.ToList().IndexOf(before) == 0 ? 1 : 0;
            Press("SettingsLanguage" + other);
            yield return new WaitForSecondsRealtime(0.3f);
            string tab = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "SettingsTabLanguage")?.GetComponentInChildren<Text>().text;
            yield return Shot("settings_language");
            Language chosen = Lang.Current;
            Press("SettingsLanguage" + Lang.All.ToList().IndexOf(before));
            yield return new WaitForSecondsRealtime(0.3f);
            Debug.Log($"Hell Poker menu {Application.version}: language button {languageButton}, cursor on {cursor}, " +
                      $"continue card {card}, languages {rows}, chose {chosen} (tab '{tab}'), back to {Lang.Current}");
            Press("SettingsBackButton");
            yield return new WaitForSecondsRealtime(0.8f);
        }

        private IEnumerator Tour()
        {
            _report.AppendLine($"Hell Poker FPS tour — {Screen.width}×{Screen.height}, vSync {QualitySettings.vSyncCount}, " +
                               $"target {Application.targetFrameRate}, {SystemInfo.graphicsDeviceName}");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("menu");
            yield return CheckTheMenu();
            if (DifficultyShots) yield return DifficultyLeg();

            // A saved run at Mammon's spoils goes on into Belial's chapter (the menu's CONTINUE), then every demon's table.
            yield return ChapterLeg();
            yield return HallLeg();
            if (TourDifficulty == Difficulty.Ironman) yield return IronmanLeg();

            Debug.Log(_report.ToString());
            Application.Quit();
        }

        private IEnumerator Measure(string screen)
        {
            var measurement = FpsCounter.Measure();
            float started = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - started < Window)
            {
                yield return null;
                measurement.Add(Time.unscaledDeltaTime);
            }
            string line = $"Hell Poker FPS {screen,-16} avg {measurement.Average,6:0.0}  min {measurement.Minimum,6:0.0}  " +
                          $"frames {measurement.Frames}  hitches {measurement.Hitches}";
            _report.AppendLine(line);
            Debug.Log(line);
        }

        private static void Press(string name)
        {
            Button button = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(b => b.name == name).OrderByDescending(b => b.gameObject.activeInHierarchy).FirstOrDefault();
            if (button == null)
            {
                Debug.LogWarning($"Hell Poker FPS tour: no button '{name}'.");
                return;
            }
            button.onClick.Invoke();

            // New Game over a run in progress asks first: the tour means it.
            if (name == "NewGameButton")
            {
                Button confirm = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.name == "MenuConfirmButton" && b.gameObject.activeInHierarchy);
                if (confirm != null) confirm.onClick.Invoke();
            }
        }
    }
}
