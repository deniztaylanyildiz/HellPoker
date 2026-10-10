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

        /// <summary>A Phase 2 run saved before the scene loads (the menu then offers CONTINUE).</summary>
        internal static IEnumerator LoadWithSave(ChapterSave save)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("ChaptersContinueButton");
            yield return new WaitForSeconds(0.6f);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator MammonsSpoilsLeadIntoBelialsStage()
        {
            yield return LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Peasant, 7, 1, "loot"));
            var panel = UnityEngine.Object.FindFirstObjectByType<ChapterPanelView>();
            var map = UnityEngine.Object.FindFirstObjectByType<ChapterMapView>();
            Assert.IsTrue(panel.IsOpen);
            Assert.AreEqual(Presentation.Ui.UiText.LootTitle, panel.Card.Title, "the spoils of Mammon");
            int coins = Chapters.Run.Purse.Coins;
            HellPokerScreenshots.Press("PanelOption" + (panel.Card.Options.Count - 1));   // the coins
            yield return null;
            Assert.AreEqual(2, Chapters.Journey.Chapter, "the second chapter");
            Assert.AreEqual(Core.Dealers.DealerRoster.BelialId, Chapters.Run.Rules.BossId);
            Assert.AreEqual(coins + ChapterRun.LootCoins, Chapters.Run.Purse.Coins, "the coins of the spoils go along");
            Assert.AreEqual(10, Chapters.Run.Map.FloorCount);
            Assert.IsTrue(panel.IsOpen, "the chapter's opening words");
            HellPokerScreenshots.Press("PanelOption0");   // DESCEND
            yield return null;
            Assert.IsTrue(map.IsVisible);

            MapNode first = Chapters.Run.Choices.OrderBy(n => n.Lane).First();
            HellPokerScreenshots.Find<Button>($"Node{first.Floor}_{first.Lane}").onClick.Invoke();
            yield return WaitForChapterTable();
            Assert.IsTrue(Chapters.IsAtTable, "Belial's first imp");
            Chapters.Table.PerformAction();
            yield return WaitForChapterTable();
            Assert.AreEqual(1, ((TablePresenter)Chapters.Table).Floor.HandsPlayed);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator LucifersTableCountsInPercentOfHisBar()
        {
            yield return LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Warlock, 9, 3, "lucifer"));
            var panel = UnityEngine.Object.FindFirstObjectByType<ChapterPanelView>();
            Assert.IsTrue(panel.IsOpen);
            Assert.AreEqual(Presentation.Ui.UiText.LuciferCallsTitle, panel.Card.Title);
            HellPokerScreenshots.Press("PanelOption0");
            yield return WaitForChapterTable();
            Assert.IsTrue(Chapters.IsAtTable);
            Assert.AreEqual(JourneyStage.Lucifer, Chapters.Journey.Stage);
            var table = (TablePresenter)Chapters.Table;
            Assert.IsNull(table.Floor, "no coins at his table");
            Chapters.Table.PerformAction();   // the deal
            yield return WaitForChapterTable();
            Assert.AreNotEqual(Core.Game.GamePhase.Betting, table.Game.Phase, "a hand is under way");
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
            // A tour that only passes (and never draws) feeds an imp's purse for a long while: unless HELLPOKER_SHOT_BROKE=1, every imp
            // deals each hand with a single coin, so the tour reaches the gate and Mammon.
            bool rigged = Environment.GetEnvironmentVariable("HELLPOKER_SHOT_BROKE") != "1";
            if (rigged) chapters.Run.Purse.Add(200);
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
                    if (card.Title == Presentation.Ui.UiText.LootTitle || card.Title == Presentation.Ui.UiText.ChapterDamnedTitle
                        || card.Title == Presentation.Ui.UiText.PurseEmptyTitle)
                        break;
                    HellPokerScreenshots.Press("PanelOption" + (card.Options.Count - 1));
                    yield return null;
                    continue;
                }
                if (chapters.IsAtTable)
                {
                    var table = (TablePresenter)chapters.Table;
                    CoinPurse imp = table.Floor?.HousePurse;
                    if (rigged && imp != null && table.Game.Phase == Core.Game.GamePhase.Betting && imp.Coins > 1) imp.Add(1 - imp.Coins);
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

        /// <summary>The later chapters (84–93): Belial's and Lilith's opening words and maps, an imp's table in each, Belial's and
        /// Lilith's tables (the bar in percent), Lucifer's call and his table.</summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureTheLaterChapters()
        {
            int shot = 84;
            for (int chapter = 2; chapter <= 3; chapter++)
            {
                ChapterSave opening = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, chapter, "chapter");
                opening.Coins = 120;
                yield return ChapterJourneyTests.LoadWithSave(opening);
                yield return new WaitForSeconds(0.3f);
                yield return HellPokerScreenshots.Shot($"{shot++}_chapter{chapter}_intro");
                HellPokerScreenshots.Press("PanelOption0");
                yield return new WaitForSeconds(0.3f);
                yield return HellPokerScreenshots.Shot($"{shot++}_chapter{chapter}_map");
                MapNode first = ChapterJourneyTests.Chapters.Run.Choices.OrderBy(n => n.Lane).First();
                HellPokerScreenshots.Find<Button>($"Node{first.Floor}_{first.Lane}").onClick.Invoke();
                yield return ChapterJourneyTests.WaitForChapterTable();
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return ChapterJourneyTests.WaitForChapterTable();
                yield return new WaitForSeconds(0.5f);
                yield return HellPokerScreenshots.Shot($"{shot++}_chapter{chapter}_imp");

                ChapterSave boss = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, chapter, "");
                boss.NodeDone = false;
                boss.Match = "boss";
                boss.BossBarStart = boss.BossBar = 2000;
                yield return ChapterJourneyTests.LoadWithSave(boss);
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return ChapterJourneyTests.WaitForChapterTable();
                yield return new WaitForSeconds(0.5f);
                yield return HellPokerScreenshots.Shot($"{shot++}_chapter{chapter}_boss");
            }

            yield return ChapterJourneyTests.LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 3, "lucifer"));
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot($"{shot++}_lucifer_calls");
            HellPokerScreenshots.Press("PanelOption0");
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.5f);
            yield return HellPokerScreenshots.Shot($"{shot}_lucifer_table");
        }

        private static string Slug(string title) =>
            new string(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
    }
}
