using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.4's screens (Screenshots/, or HELLPOKER_SHOTS; HELLPOKER_SHOT_SIZE, HELLPOKER_LANG=tr): the difficulty row (a locked level, an
    /// open one), SAVE on the map, SAVE dim at a table with its hover, an Ironman camp, Ironman's quit warning, an enraged demon.
    /// Not a real test (Explicit).
    /// </summary>
    [Explicit]
    public class Demo34Shots
    {
        [TearDown]
        public void Restore()
        {
            HellPokerBootstrap.ForceSimulator = null;
            ChapterUnlocks.AllClassesOpen = true;
        }

        private static string Tag => (System.Environment.GetEnvironmentVariable("HELLPOKER_SHOT_SIZE") ?? "1920x1080") + "_" +
                                     (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr" ? "tr" : "en");

        private static IEnumerator Load(ChapterSave save = null)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            if (save != null) new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            HellPokerBootstrap.ForceSimulator = false;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            if (save == null) yield break;
            HellPokerScreenshots.Press("ContinueButton");
            yield return new WaitForSeconds(0.8f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureDifficultyAndSave()
        {
            // The difficulty row: Hard locked (as a first player sees it), then Medium open, each with its words.
            ChapterUnlocks.AllClassesOpen = false;
            yield return Load();
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.8f);
            HellPokerScreenshots.Press("DifficultyUp");
            yield return null;
            var select = Object.FindFirstObjectByType<SinnerSelectView>();
            select.PointAtDifficulty();
            yield return null;
            yield return HellPokerScreenshots.Shot($"360_difficulty_locked_{Tag}");
            HellPokerScreenshots.Press("DifficultyDown");
            yield return null;
            select.PointAtDifficulty();
            yield return null;
            yield return HellPokerScreenshots.Shot($"361_difficulty_open_{Tag}");
            ChapterUnlocks.AllClassesOpen = true;

            // SAVE on the map (Medium), then dim at a floor's table with its hover.
            HellPokerScreenshots.ChooseSinner(0);
            yield return new WaitForSeconds(0.8f);
            HellPokerScreenshots.Press("PanelOption0");   // DESCEND
            yield return new WaitForSeconds(0.4f);
            HellPokerScreenshots.Press("SaveButton");
            yield return null;
            yield return HellPokerScreenshots.Shot($"362_map_save_{Tag}");
            MapNode table = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<Button>($"Node{table.Floor}_{table.Lane}").onClick.Invoke();
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.4f);
            Object.FindFirstObjectByType<SaveButtonView>().PointAt();
            yield return null;
            yield return HellPokerScreenshots.Shot($"363_table_save_dim_{Tag}");

            // An Ironman camp: SAVE only there.
            ChapterSave camp = AtCamp(Core.Sinners.SinnerRoster.King, Difficulty.Ironman);
            yield return Load(camp);
            yield return HellPokerScreenshots.Shot($"364_ironman_camp_{Tag}");

            // Ironman's warning: a step past the camp, then QUIT from the menu.
            HellPokerScreenshots.Press("PanelOption" + (ChapterJourneyTests.Chapters.Panel.Options.Count - 1));
            yield return new WaitForSeconds(0.3f);
            if (ChapterJourneyTests.Chapters.Panel != null)
                HellPokerScreenshots.Press("PanelOption" + (ChapterJourneyTests.Chapters.Panel.Options.Count - 1));
            yield return new WaitForSeconds(0.3f);
            MapNode next = ChapterJourneyTests.Chapters.Run.Choices.FirstOrDefault(n => n.Kind == NodeKind.Event || n.Kind == NodeKind.Treasure)
                           ?? ChapterJourneyTests.Chapters.Run.Choices.First();
            HellPokerScreenshots.Find<Button>($"Node{next.Floor}_{next.Lane}").onClick.Invoke();
            yield return new WaitForSeconds(0.6f);
            ChapterJourneyTests.Menu.GoBack();
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("QuitButton");
            yield return null;
            yield return HellPokerScreenshots.Shot($"365_ironman_quit_{Tag}");

            // An enraged demon: Mammon's table at Medium, his bar under half.
            ChapterSave boss = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 1, "");
            boss.Difficulty = Difficulty.Medium;
            boss.NodeDone = false;
            boss.Match = "boss";
            boss.BossBarStart = 1600;
            boss.BossBar = 700;
            yield return Load(boss);
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot($"366_boss_enraged_{Tag}");
        }

        /// <summary>A save standing at the first chapter's nearest camp, its panel waiting.</summary>
        internal static ChapterSave AtCamp(Core.Sinners.SinnerClass cls, Difficulty difficulty)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, 11, difficulty);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new List<MapNode> { n }).ToList();
            List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == NodeKind.PurgatoryFire);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = journey.Capture();
            save.Trail = found.Select(n => n.Lane).ToList();
            save.NodeDone = false;
            save.Pending = "fire";
            save.Coins = 300;
            return save;
        }
    }
}
