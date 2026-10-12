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
        [TearDown]
        public void BackToTheUsualMenu() => HellPokerBootstrap.ForceSimulator = null;

        internal static MainMenuPresenter Menu =>
            (MainMenuPresenter)typeof(HellPokerBootstrap).GetField("_menuPresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());

        [UnityTest, Timeout(120000)]
        public IEnumerator F9_OpensTheBossSimulator_AndItsSaveOutlivesANewGame()
        {
            yield return OpenChapters();
            Assert.IsTrue(Chapters.Journey != null, "NEW GAME: a chapter run (class, then the map)");
            Assert.IsTrue(UnityEngine.Object.FindFirstObjectByType<ChapterMapView>().IsVisible);

            // F9 on the title menu: the Boss Simulator, the old demo.
            HellPokerScreenshots.Find<Button>("MapMenuButton").onClick.Invoke();
            yield return new WaitForSeconds(0.6f);
            Menu.ToggleSimulator();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(Menu.InSimulator);
            HellPokerScreenshots.Press("NewGameButton");   // a demon, then a class
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("ChooseDealer0");
            yield return new WaitForSeconds(0.6f);
            var demo = (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());
            Assert.IsNotNull(demo.Game, "the simulator's table");
            demo.PerformAction();   // a hand: the simulator's run is saved
            yield return new WaitForSeconds(0.3f);
            Assert.IsNotNull(new Presentation.Settings.RunArchive(HellPokerBootstrap.BatchStore).LoadRun(), "the simulator's own save");

            // Back to the game: a new run over the waiting one, confirmed — the simulator's save stays.
            // The simulator's own MENU (the chapter's table has a button of the same name).
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .First(b => b.name == "MenuButton" && b.GetComponentInParent<TableView>()?.name != "ChapterTableCanvas").onClick.Invoke();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(Menu.IsAtMenuRoot, "back on the title menu");
            Menu.ToggleSimulator();
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(Menu.InSimulator);
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.ChooseSinner(1);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual("warlock", Chapters.Journey.Sinner.Id, "the new chapter run");
            Assert.IsNotNull(new Presentation.Settings.RunArchive(HellPokerBootstrap.BatchStore).LoadRun(), "the simulator's save outlives it");
        }
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
            HellPokerBootstrap.ForceSimulator = false;   // the game, not the Boss Simulator
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("NewGameButton");
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

        [UnityTest, Timeout(120000)]
        public IEnumerator TheBugReportButton_OpensTheBox_AndTheReportGoesToTheClipboard()
        {
            yield return OpenChapters();
            HellPokerScreenshots.Find<Button>("MapMenuButton").onClick.Invoke();   // the game's pause menu: the title menu
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("MenuFeedbackButton");
            yield return null;
            var box = UnityEngine.Object.FindFirstObjectByType<FeedbackView>();
            Assert.IsTrue(box.IsOpen);
            Assert.IsTrue(box.IsBug);
            HellPokerScreenshots.Press("FeedbackIdeaButton");
            Assert.IsFalse(box.IsBug);
            box.Text = "More demons, please.";
            HellPokerScreenshots.Press("FeedbackSendButton");
            yield return null;
            StringAssert.Contains("caveengine@gmail.com", box.Status);
            var feedback = (FeedbackPresenter)typeof(HellPokerBootstrap).GetField("_feedbackPresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());
            Assert.AreEqual("[Hell Poker ÖNERİ] More demons, please.", feedback.LastSubject);
            StringAssert.Contains("Class: peasant", feedback.LastBody, "where the run stands");
            StringAssert.StartsWith("mailto:caveengine@gmail.com", feedback.LastLink);
            HellPokerScreenshots.Press("FeedbackBackButton");
            Assert.IsFalse(box.IsOpen);
            Assert.IsTrue(Menu.IsAtMenuRoot);
        }

        /// <summary>A Phase 2 run saved before the scene loads (the menu then offers CONTINUE).</summary>
        internal static IEnumerator LoadWithSave(ChapterSave save)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            HellPokerBootstrap.ForceSimulator = false;   // the game, not the Boss Simulator
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("ContinueButton");
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
            Assert.AreEqual(Math.Max(coins + ChapterRun.LootCoins, ChapterRules.StartingCoinsFor(Core.Sinners.Peasant.ClassId)), Chapters.Run.Purse.Coins,
                "the coins of the spoils go along, topped up to the Peasant's start");
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
        [TearDown]
        public void BackToTheUsualMenu() => HellPokerBootstrap.ForceSimulator = null;

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
            // The class choice of a chapter run: the classes' Phase 2 powers on their cards.
            HellPokerBootstrap.BatchStore.Clear();
            if (Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            HellPokerBootstrap.ForceSimulator = false;   // the game, not the Boss Simulator
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            // How to Play: the game's page first, then the powers and the demons' rules.
            yield return HellPokerScreenshots.Shot("80_menu");
            HellPokerScreenshots.Press("HowToPlayButton");
            yield return HellPokerScreenshots.Shot("81_rules_game");
            HellPokerScreenshots.Press("HandRanksPageButton");
            yield return HellPokerScreenshots.Shot("82_rules_powers");
            HellPokerScreenshots.Press("BackButton");
            yield return new WaitForSeconds(0.4f);
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.8f);
            yield return HellPokerScreenshots.Shot("83_chapter_classes");

            int shot = 84;
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                // The gate: the demon's bar, small, under the words.
                ChapterSave gate = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, chapter, "gate");
                gate.Coins = 60;
                yield return ChapterJourneyTests.LoadWithSave(gate);
                yield return new WaitForSeconds(0.3f);
                yield return HellPokerScreenshots.Shot($"{shot++}_chapter{chapter}_gate");

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

                // The bosses with the other classes' panels: the Warlock's prophecy at Belial's, the Peasant's satchel at Lilith's.
                ChapterSave boss = ChapterPresenter.PlacedSave(chapter == 1 ? Core.Sinners.SinnerRoster.King : chapter == 2 ? Core.Sinners.SinnerRoster.Warlock : Core.Sinners.SinnerRoster.Peasant, 11,
                    chapter, "");
                boss.Satchel = new System.Collections.Generic.List<Core.Cards.Card> { new Core.Cards.Card(Core.Cards.Rank.Ace, Core.Cards.Suit.Hearts),
                    new Core.Cards.Card(Core.Cards.Rank.Seven, Core.Cards.Suit.Clubs) };
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

        /// <summary>Demo 2.0's screens (110–118): the menu's BUG / IDEA box, the records with the circles and the locks, the class choice
        /// with the Summoner and a circle, a demon's table with three relics and upgrades, the Summoner's imps, the spoils, the fire in
        /// the fourth circle.</summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureDemoTwo()
        {
            var book = new ChapterRecords();
            book.RunStarted();
            book.RunEnded(JourneyEnd.Freed, 4, Core.Sinners.Peasant.ClassId, 140);
            book.WardenBeaten();
            HellPokerBootstrap.BatchStore.Clear();
            if (Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            HellPokerBootstrap.BatchStore.SetString(Presentation.Settings.ChapterArchive.RecordsKey, book.Encode());
            HellPokerBootstrap.ForceSimulator = false;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            yield return HellPokerScreenshots.Shot("110_menu");
            HellPokerScreenshots.Press("MenuFeedbackButton");
            UnityEngine.Object.FindFirstObjectByType<FeedbackView>().Text = "The gate took my last coin before Belial.";
            yield return null;
            yield return HellPokerScreenshots.Shot("111_feedback");
            HellPokerScreenshots.Press("FeedbackSendButton");
            yield return null;
            yield return HellPokerScreenshots.Shot("112_feedback_sent");
            HellPokerScreenshots.Press("FeedbackBackButton");
            HellPokerScreenshots.Press("RecordsButton");
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot("113_records");
            ChapterJourneyTests.Menu.GoBack();
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.8f);
            HellPokerScreenshots.Press("CircleUp0");
            yield return null;
            yield return HellPokerScreenshots.Shot("114_classes_circle");

            // A demon's table: three relics (their rarity frames) and two upgrades (the pips over the class badge).
            ChapterSave boss = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Peasant, 11, 1, "");
            boss.Relics = new List<string> { Core.Relics.RelicIds.FerrymansCoin, Core.Relics.ChapterRelicIds.IronVow, Core.Relics.ChapterRelicIds.BlackBook };
            boss.Upgrades = new List<string> { UpgradeIds.WideSatchel, UpgradeIds.Seed };
            boss.NodeDone = false;
            boss.Match = "boss";
            boss.BossBarStart = boss.BossBar = 1300;
            yield return ChapterJourneyTests.LoadWithSave(boss);
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.5f);
            yield return HellPokerScreenshots.Shot("115_boss_relics_upgrades");

            // The Summoner at an imp's table: the imps' panel.
            ChapterSave summoner = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Summoner, 11, 1, "chapter");
            summoner.Coins = 200;
            yield return ChapterJourneyTests.LoadWithSave(summoner);
            HellPokerScreenshots.Press("PanelOption0");
            yield return new WaitForSeconds(0.3f);
            MapNode first = ChapterJourneyTests.Chapters.Run.Choices.OrderBy(n => n.Lane).First();
            HellPokerScreenshots.Find<Button>($"Node{first.Floor}_{first.Lane}").onClick.Invoke();
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.5f);
            yield return HellPokerScreenshots.Shot("116_summoner_imps");

            yield return ChapterJourneyTests.LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 1, "loot"));
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("117_spoils");

            ChapterSave fire = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 1, "fire");
            fire.Circle = 4;
            yield return ChapterJourneyTests.LoadWithSave(fire);
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("118_fire_circle4");
        }

        private static string Slug(string title) =>
            new string(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
    }
}
