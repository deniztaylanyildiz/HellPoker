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
    /// buttons — the menu, the choice of demon, each demon's hall, then Lucifer's (summoned the way the game summons) —
    /// measures a few seconds on each, writes the results to the log ("Hell Poker FPS ...") and quits. Not part of normal play.
    /// </summary>
    public sealed class FpsTour : MonoBehaviour
    {
        public const string Argument = "-fpstour";
        private const float Settle = 1.5f;
        private const float Window = 4f;

        private TablePresenter _table;
        private ChapterPresenter _chapters;
        private MainMenuPresenter _menu;
        private readonly StringBuilder _report = new StringBuilder();

        public static bool IsRequested => System.Environment.GetCommandLineArgs().Contains(Argument);

        /// <param name="chapters">Phase 2's chapters: the tour also crosses from the first chapter into the second (a run saved at
        /// Mammon's spoils, continued from the menu). Null: the demo alone.</param>
        /// <param name="menu">The title menu: the tour switches it to the Boss Simulator (F9's way) for the demons' halls.</param>
        public void Run(TablePresenter table, ChapterPresenter chapters = null, MainMenuPresenter menu = null)
        {
            _table = table;
            _chapters = chapters;
            _menu = menu;
            StartCoroutine(Tour());
        }

        /// <summary>Phase 2: Mammon's spoils → the second chapter's opening → Belial's map → the first imp's table.</summary>
        private IEnumerator ChapterLeg()
        {
            _chapters.PrepareTour(Core.Sinners.SinnerRoster.Peasant);
            Press("ContinueButton");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("p2 spoils");
            int coins = _chapters.Panel?.Options.Count ?? 1;
            Press("PanelOption" + (coins - 1));   // the coins
            yield return new WaitForSecondsRealtime(Settle);
            Debug.Log($"Hell Poker Phase 2 tour: chapter {_chapters.Journey?.Chapter}, demon {_chapters.Run?.Rules.BossId}, " +
                      $"floors {_chapters.Run?.Map.FloorCount}, purse {_chapters.Run?.Purse.Coins}");
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

            // The game first: a saved run at Mammon's spoils goes on into Belial's chapter (the menu's CONTINUE).
            if (_chapters != null)
            {
                yield return ChapterLeg();
                Press("MenuButton");
                yield return new WaitForSecondsRealtime(0.8f);
            }

            // Then the Boss Simulator (the old demo, F9): every demon's hall and Lucifer's.
            if (_menu != null && !_menu.InSimulator) _menu.ToggleSimulator();
            yield return new WaitForSecondsRealtime(0.8f);
            Press("NewGameButton");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("choose a demon");

            for (int dealer = 0; dealer < DealerRoster.All.Count; dealer++)
            {
                if (dealer > 0)
                {
                    Press("MenuButton");
                    yield return new WaitForSecondsRealtime(0.6f);
                    Press("NewGameButton");
                    yield return new WaitForSecondsRealtime(0.6f);
                }
                Press("ChooseDealer" + dealer);
                yield return new WaitForSecondsRealtime(Settle);
                yield return Measure(DealerRoster.All[dealer].Id);
            }

            // Lucifer's hall: brought down to his gate, the game summons the player between hands.
            _table.Game.TakeOver(150, _table.Game.RoundNumber);
            _table.SwitchTable(DealerRoster.Lilith);
            // The summoning scene (the dark falling, his words) plays out before the hall is measured.
            yield return new WaitForSecondsRealtime(Settle * 4);
            yield return Measure(_table.IsAtFinalTable ? "lucifer" : "lucifer (not summoned)");

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

            // A new run asks who the player was: the tour plays the Peasant.
            if (name.StartsWith("ChooseDealer"))
            {
                Button sinner = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.name == "ChooseSinner0" && b.gameObject.activeInHierarchy);
                if (sinner != null) sinner.onClick.Invoke();
            }
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
