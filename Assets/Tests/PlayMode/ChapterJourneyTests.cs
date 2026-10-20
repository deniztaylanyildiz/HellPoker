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
    /// The game in the real scene: NEW GAME, the class, the chapter's map and its first imp's table; an old player's simulator leftovers. <see cref="ChapterScreenshots"/> walks the whole chapter for pictures.
    /// </summary>
    public class ChapterJourneyTests
    {
        internal static MainMenuPresenter Menu =>
            (MainMenuPresenter)typeof(HellPokerBootstrap).GetField("_menuPresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());

        /// <summary>The player's own run (a King in Belial's chapter, Hard) and records (a Medium freedom) in the store, before the scene loads.</summary>
        internal static void PlayersOwnRunAndRecords(out string save, out string book)
        {
            HellPokerBootstrap.BatchStore.Clear();
            var archive = new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore);
            ChapterSave mine = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 2, "");
            mine.NodeDone = true;
            mine.Difficulty = Difficulty.Hard;
            archive.SaveRun(mine);
            var records = new ChapterRecords();
            records.RunEnded(JourneyEnd.Freed, 4, Core.Sinners.King.ClassId, 100, Difficulty.Medium, 200);
            archive.SaveRecords(records);
            save = HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.ChapterArchive.RunKey, null);
            book = HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.ChapterArchive.RecordsKey, null);
        }

        /// <summary>Demo 3.4.1: a player who played the Boss Simulator launches the game — its run and records (garbled or not) go without a
        /// word, the game's own save and records stay and CONTINUE leads to the run.</summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TheSimulatorsOldSaveAndRecords_AreDropped_TheGamesOwnStay()
        {
            PlayersOwnRunAndRecords(out string save, out string book);
            HellPokerBootstrap.BatchStore.SetString(Presentation.Settings.RunArchive.RunKey, "v=4\ndealer=belial\nyears=???");
            HellPokerBootstrap.BatchStore.SetString(Presentation.Settings.RunArchive.RecordsKey, "runs=12\nabsolved=3");
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);

            Assert.IsNull(HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.RunArchive.RunKey, null), "the simulator's run is gone");
            Assert.IsNull(HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.RunArchive.RecordsKey, null), "and its records");
            Assert.AreEqual(save, HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.ChapterArchive.RunKey, null));
            Assert.AreEqual(book, HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.ChapterArchive.RecordsKey, null));
            Assert.AreEqual(1, Chapters.Records.FreedAt(Core.Sinners.King.ClassId, Difficulty.Medium), "the game's records read");
            Assert.IsTrue(Menu.IsAtMenuRoot);
            HellPokerScreenshots.Press("ContinueButton");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(Core.Sinners.King.ClassId, Chapters.Journey.Sinner.Id, "CONTINUE: the game's run");
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
            Assert.AreEqual(3, map.State.Choices.Count, "three ways to start (Demo 2.1's map)");

            MapNode start = map.State.Choices.First(n => n.Kind == NodeKind.Table);   // a table among the starts, always
            Button node = HellPokerScreenshots.Find<Button>($"Node0_{start.Lane}");
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
            HellPokerScreenshots.Press("DifficultyUp");
            yield return null;
            yield return HellPokerScreenshots.Shot("114_classes_difficulty");

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
            fire.Difficulty = Difficulty.Ironman;
            yield return ChapterJourneyTests.LoadWithSave(fire);
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("118_fire_ironman");
        }
        /// <summary>A save standing on a node of <paramref name="kind"/> of the first chapter's map (a path walked to it), its panel pending.</summary>
        private static ChapterSave AtNode(Core.Sinners.SinnerClass cls, int seed, NodeKind kind, string pending)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, seed);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new List<MapNode> { n }).ToList();
            List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == kind);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = ChapterPresenter.PlacedSave(cls, seed, 1, pending);
            save.Trail = found?.Select(n => n.Lane).ToList() ?? new List<int>();
            save.NodeDone = false;
            save.Coins = 300;
            return save;
        }

        /// <summary>Demo 2.1's screens (120–145): the sentence and the bar at a demon's table and a hand's two lines, two maps and an unknown
        /// node, a helper, a swindler (the Warlock reads him), an offer's result, the camp and the bag (five worn), the six vendors, five
        /// relics at a table.</summary>
        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureDemoTwoOne()
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            string[] five = { Core.Relics.RelicIds.FerrymansCoin, Core.Relics.ChapterRelicIds.IronVow, Core.Relics.ChapterRelicIds.BlackBook,
                Core.Relics.ChapterRelicIds.LeadSeal, Core.Relics.ChapterRelicIds.AshPurse };

            // How to Play's first page: the game's rules (the sentence and the bar, the unknown, the offers, the camp and the bag, the markets).
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("HowToPlayButton");
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot("119_how_to_play_game");

            // A demon's table: the sentence in the corner, the bar under the portrait, five relics; then a hand folded: its two lines.
            ChapterSave boss = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Peasant, 11, 1, "");
            boss.Relics = five.ToList();
            boss.NodeDone = false;
            boss.Match = "boss";
            boss.BossBarStart = boss.BossBar = 1300;
            yield return ChapterJourneyTests.LoadWithSave(boss);
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.5f);
            yield return HellPokerScreenshots.Shot("120_boss_sentence_bar_five_relics");
            ChapterJourneyTests.Chapters.Table.Bet(Core.Game.BetAction.Fold);
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.8f);
            yield return HellPokerScreenshots.Shot("121_boss_result_lines");

            // Two maps of other seeds; the hover of an unknown node.
            for (int k = 0; k < 2; k++)
            {
                ChapterSave map = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31 + k * 17, 1, "");
                map.NodeDone = true;
                yield return ChapterJourneyTests.LoadWithSave(map);
                yield return new WaitForSeconds(0.4f);
                yield return HellPokerScreenshots.Shot($"12{2 + k}_map_{k + 1}");
            }
            MapNode unknown = ChapterJourneyTests.Chapters.Run.Map.Floors.SelectMany(f => f).FirstOrDefault(n => n.Kind == NodeKind.Unknown);
            if (unknown != null)
            {
                var hover = HellPokerScreenshots.Find<Button>($"Node{unknown.Floor}_{unknown.Lane}").GetComponent<UnityEngine.EventSystems.EventTrigger>();
                hover.triggers[0].callback.Invoke(null);
                yield return null;
                yield return HellPokerScreenshots.Shot("124_unknown_node");
            }

            // A helper; a swindler as the Warlock reads him; an offer's result on its own panel.
            yield return ChapterJourneyTests.LoadWithSave(AtNode(Core.Sinners.SinnerRoster.Peasant, 11, NodeKind.Event, "event:" + ChoiceEventIds.ChainedSoul));
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("125_offer_help");
            yield return ChapterJourneyTests.LoadWithSave(AtNode(Core.Sinners.SinnerRoster.Find(Core.Sinners.Warlock.ClassId), 11, NodeKind.Event,
                "event:" + ChoiceEventIds.ThreeCups));
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("126_offer_swindle_prophecy");
            yield return ChapterJourneyTests.LoadWithSave(AtNode(Core.Sinners.SinnerRoster.Peasant, 11, NodeKind.Event, "event:" + ChoiceEventIds.HellLottery));
            yield return new WaitForSeconds(0.3f);
            HellPokerScreenshots.Press("PanelOption0");
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("127_offer_result");

            // The camp: its four actions, then the relics' bag (five worn, three in the bag, one forged).
            ChapterSave camp = AtNode(Core.Sinners.SinnerRoster.Peasant, 11, NodeKind.PurgatoryFire, "fire");
            camp.Relics = five.ToList();
            camp.Bag = new List<string> { Core.Relics.ChapterRelicIds.RustedChain, Core.Relics.ChapterRelicIds.LiarsTongue, Core.Relics.RelicIds.RustyCrown };
            camp.Forged = new List<string> { Core.Relics.ChapterRelicIds.BlackBook };
            yield return ChapterJourneyTests.LoadWithSave(camp);
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("128_camp");
            HellPokerScreenshots.Press("PanelOption4");
            yield return new WaitForSeconds(0.3f);
            HellPokerScreenshots.Press("BagPlace1");
            yield return null;
            yield return HellPokerScreenshots.Shot("129_camp_bag");
            HellPokerScreenshots.Press("BagDoneButton");
            yield return null;

            // The six vendors.
            int shot = 130;
            foreach (MarketVendor vendor in Enum.GetValues(typeof(MarketVendor)))
            {
                ChapterSave market = AtNode(Core.Sinners.SinnerRoster.Find(Core.Sinners.Jester.ClassId), 11, NodeKind.BlackMarket, "market");
                market.Vendor = (int)vendor;
                market.Relics = new List<string> { Core.Relics.ChapterRelicIds.AshPurse, Core.Relics.RelicIds.RustyCrown, Core.Relics.ChapterRelicIds.CandleStub };
                yield return ChapterJourneyTests.LoadWithSave(market);
                yield return new WaitForSeconds(0.3f);
                yield return HellPokerScreenshots.Shot($"{shot++}_market_{vendor.ToString().ToLowerInvariant()}");
            }
        }

        private static string Slug(string title) =>
            new string(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
    }
}
