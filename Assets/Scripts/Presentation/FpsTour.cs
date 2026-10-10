using System.Collections;
using System.Linq;
using System.Text;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
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
        private readonly StringBuilder _report = new StringBuilder();

        public static bool IsRequested => System.Environment.GetCommandLineArgs().Contains(Argument);

        /// <param name="chapters">Phase 2's chapters: the tour also crosses from the first chapter into the second (a run saved at
        /// Mammon's spoils, continued from the menu). Null: the demo alone.</param>
        public void Run(TablePresenter table, ChapterPresenter chapters = null)
        {
            _table = table;
            _chapters = chapters;
            StartCoroutine(Tour());
        }

        /// <summary>Phase 2: Mammon's spoils → the second chapter's opening → Belial's map → the first imp's table.</summary>
        private IEnumerator ChapterLeg()
        {
            _chapters.PrepareTour(Core.Sinners.SinnerRoster.Peasant);
            Press("MenuButton");
            yield return new WaitForSecondsRealtime(0.8f);
            Press("ChaptersContinueButton");
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
            MapNode first = _chapters.Run?.Choices.OrderBy(n => n.Lane).FirstOrDefault();
            if (first != null) Press($"Node{first.Floor}_{first.Lane}");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("p2 belial imp");
        }

        private IEnumerator Tour()
        {
            _report.AppendLine($"Hell Poker FPS tour — {Screen.width}×{Screen.height}, vSync {QualitySettings.vSyncCount}, " +
                               $"target {Application.targetFrameRate}, {SystemInfo.graphicsDeviceName}");
            yield return new WaitForSecondsRealtime(Settle);
            yield return Measure("menu");

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

            if (_chapters != null) yield return ChapterLeg();

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
