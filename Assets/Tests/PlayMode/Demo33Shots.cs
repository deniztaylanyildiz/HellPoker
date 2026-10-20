using System.Collections;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.3's screens (Screenshots/, or HELLPOKER_SHOTS; HELLPOKER_SHOT_SIZE, HELLPOKER_LANG=tr): the title menu with no run and with a
    /// saved one, the settings (the game tab, the language tab where there is one), the records and How to Play over the menu's backdrop.
    /// Not a real test (Explicit).
    /// </summary>
    [Explicit]
    public class Demo33Shots
    {
        [TearDown]
        public void Restore() => HellPokerBootstrap.ForceSimulator = null;

        private static string Tag => (System.Environment.GetEnvironmentVariable("HELLPOKER_SHOT_SIZE") ?? "1920x1080") + "_" +
                                     (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr" ? "tr" : "en");

        private static IEnumerator Menu(ChapterSave save)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            if (save != null) new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            HellPokerBootstrap.ForceSimulator = false;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(2.5f);   // the logo's entrance plays out
        }

        private static bool Has(string button) => Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Any(b => b.name == button);

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureTheMenuAndSettings()
        {
            yield return Menu(null);
            yield return HellPokerScreenshots.Shot($"350_menu_new_{Tag}");

            ChapterSave save = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 2, "");
            save.NodeDone = true;
            save.Coins = 140;
            save.Difficulty = Difficulty.Hard;
            yield return Menu(save);
            yield return HellPokerScreenshots.Shot($"351_menu_saved_{Tag}");

            HellPokerScreenshots.Press("SettingsButton");
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot($"352_settings_game_{Tag}");
            if (Has("SettingsTabLanguage"))
            {
                HellPokerScreenshots.Press("SettingsTabLanguage");
                yield return null;
                yield return HellPokerScreenshots.Shot($"353_settings_language_{Tag}");
            }
            HellPokerScreenshots.Press("SettingsBackButton");
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("RecordsButton");
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot($"354_records_{Tag}");
            ChapterJourneyTests.Menu.GoBack();
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("HowToPlayButton");
            yield return new WaitForSeconds(0.4f);
            yield return HellPokerScreenshots.Shot($"355_howto_{Tag}");

            // The Boss Simulator's menu (F9): the same layout under its own label.
            HellPokerBootstrap.BatchStore.Clear();
            if (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            HellPokerBootstrap.ForceSimulator = true;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(1f);
            yield return HellPokerScreenshots.Shot($"356_menu_simulator_{Tag}");
        }
    }
}
