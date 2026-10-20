using System.Collections;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Phase 3's before / after pictures: the title menu, the map, a floor's table, a demon's table and How to Play, at the size in
    /// HELLPOKER_SHOT_SIZE (1920x1080, 2560x1440, 3840x2160, 1366x768), into HELLPOKER_SHOTS (Screenshots/phase3/... by hand).
    /// </summary>
    [Explicit, Category("Screenshots")]
    public class PhaseThreeShots
    {

        private static string Tag => System.Environment.GetEnvironmentVariable("HELLPOKER_SHOT_SIZE") ?? "1920x1080";

        [UnityTest, Timeout(300000)]
        public IEnumerator CaptureTheFiveScreens()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot($"p3_menu_{Tag}");

            HellPokerScreenshots.Press("HowToPlayButton");
            yield return new WaitForSeconds(0.5f);
            yield return HellPokerScreenshots.Shot($"p3_howto_{Tag}");
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.6f);

            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.ChooseSinner(0);
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("PanelOption0");   // DESCEND
            yield return new WaitForSeconds(0.4f);
            yield return HellPokerScreenshots.Shot($"p3_map_{Tag}");

            MapNode start = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<Button>($"Node0_{start.Lane}").onClick.Invoke();
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();   // the deal
            yield return new WaitForSeconds(1.2f);
            if (ChapterJourneyTests.ChapterTableView.IsBusy) ChapterJourneyTests.ChapterTableView.SkipAnimations();
            yield return null;
            yield return HellPokerScreenshots.Shot($"p3_floor_{Tag}");

            // A demon's table: Mammon's, the King with his relics, a hand dealt.
            HellPokerBootstrap.BatchStore.Clear();
            new ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(new ChapterSave
            {
                ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                BossBar = 1600, NodeDone = false, Relics = Core.Relics.ChapterRelics.ClassChain(Core.Sinners.King.ClassId).ToList(), Taxes = 20
            });
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("ContinueButton");
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return new WaitForSeconds(1.2f);
            if (ChapterJourneyTests.ChapterTableView.IsBusy) ChapterJourneyTests.ChapterTableView.SkipAnimations();
            yield return null;
            yield return HellPokerScreenshots.Shot($"p3_boss_{Tag}");
        }
    }
}
