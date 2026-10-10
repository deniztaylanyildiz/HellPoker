using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Phase 2 in the real scene: the menu's PHASE 2 TEST button, the class, the chapter's map and its first imp's table — and the
    /// demo's own table is never the one played. <see cref="ChapterScreenshots"/> walks the whole chapter for pictures.
    /// </summary>
    public class ChapterJourneyTests
    {
        internal static ChapterPresenter Chapters =>
            (ChapterPresenter)typeof(HellPokerBootstrap).GetField("_chapterPresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());

        internal static TableView ChapterTableView =>
            UnityEngine.Object.FindObjectsByType<TableView>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "ChapterTableCanvas");

        internal static IEnumerator WaitForChapterTable()
        {
            TableView table = ChapterTableView;
            float started = Time.time;
            yield return null;
            while (table != null && table.IsBusy && Time.time - started < 10f)
                yield return null;
        }

        internal static IEnumerator OpenChapters(int sinner = 0)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("ChaptersButton");
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.ChooseSinner(sinner);
            yield return new WaitForSeconds(0.6f);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ThePhaseTwoButtonOpensTheChapterAndItsFirstTable()
        {
            yield return OpenChapters();
            var map = UnityEngine.Object.FindFirstObjectByType<ChapterMapView>();
            var panel = UnityEngine.Object.FindFirstObjectByType<ChapterPanelView>();
            Assert.IsTrue(map.IsVisible, "the map");
            Assert.IsTrue(panel.IsOpen, "the chapter's opening words");
            HellPokerScreenshots.Press("PanelOption0");   // DESCEND
            yield return null;
            Assert.IsFalse(panel.IsOpen);
            Assert.AreEqual(6, map.State.Choices.Count, "six ways to start");

            Button node = HellPokerScreenshots.Find<Button>("Node0_2");
            node.onClick.Invoke();
            yield return WaitForChapterTable();
            Assert.IsTrue(Chapters.IsAtTable);
            Assert.IsFalse(map.IsVisible);
            TableView table = ChapterTableView;
            Assert.IsNotNull(table, "the chapter's own table");
            Assert.IsTrue(table.GetComponent<Canvas>().enabled);

            // A hand at the imp's table, for coins.
            Chapters.Table.PerformAction();
            yield return WaitForChapterTable();
            var demo = (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());
            Assert.IsNull(demo.Game, "the demo's run is untouched");
            Assert.AreEqual(1, ((TablePresenter)Chapters.Table).Floor.HandsPlayed);
        }
    }

    /// <summary>
    /// Screenshots of Phase 2's first chapter (70–79), walking it through to the end with the simplest answers (the way out on every
    /// panel, every decision passed): the opening words, the map, an imp's table, a match's end, each kind of node met, the gate,
    /// Mammon's table and the chapter's end. HELLPOKER_LANG=tr for Turkish; HELLPOKER_SHOTS for the folder.
    /// </summary>
    [Explicit, Category("Screenshots")]
    public class ChapterScreenshots
    {
        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureTheChapter()
        {
            yield return ChapterJourneyTests.OpenChapters(sinner: 2);
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("70_chapter_intro");
            HellPokerScreenshots.Press("PanelOption0");
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("71_chapter_map");

            var panel = UnityEngine.Object.FindFirstObjectByType<ChapterPanelView>();
            var seen = new HashSet<string>();
            int shot = 72;
            bool tableShot = false, bossShot = false;
            ChapterPresenter chapters = ChapterJourneyTests.Chapters;
            for (int guard = 0; guard < 4000; guard++)
            {
                if (panel.IsOpen)
                {
                    PanelCard card = panel.Card;
                    if (seen.Add(card.Title) && shot < 90)
                    {
                        yield return new WaitForSeconds(0.2f);
                        yield return HellPokerScreenshots.Shot($"{shot++}_panel_{Slug(card.Title)}");
                    }
                    if (card.Title == Presentation.Ui.UiText.ChapterDoneTitle || card.Title == Presentation.Ui.UiText.ChapterDamnedTitle
                        || card.Title == Presentation.Ui.UiText.ChapterFreeTitle)
                        break;
                    HellPokerScreenshots.Press("PanelOption" + (card.Options.Count - 1));
                    yield return null;
                    continue;
                }
                if (chapters.IsAtTable)
                {
                    var table = (TablePresenter)chapters.Table;
                    chapters.Table.PerformAction();
                    yield return ChapterJourneyTests.WaitForChapterTable();
                    if (!tableShot && table.Floor != null && table.Game.Phase == Core.Game.GamePhase.DrawReveal)
                    {
                        tableShot = true;
                        yield return new WaitForSeconds(0.5f);
                        yield return HellPokerScreenshots.Shot($"{shot++}_imp_table");
                    }
                    if (!bossShot && table.Floor == null && table.Game.Phase == Core.Game.GamePhase.DrawReveal)
                    {
                        bossShot = true;
                        yield return new WaitForSeconds(0.5f);
                        yield return HellPokerScreenshots.Shot($"{shot++}_mammon_table");
                    }
                    continue;
                }
                MapNode next = chapters.Run.Choices.OrderBy(n => n.Kind == NodeKind.Warden ? 0 : n.Kind == NodeKind.BlackMarket ? 1 : 2).First();
                HellPokerScreenshots.Find<Button>($"Node{next.Floor}_{next.Lane}").onClick.Invoke();
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot($"{shot}_chapter_end");
        }

        private static string Slug(string title) =>
            new string(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
    }
}
